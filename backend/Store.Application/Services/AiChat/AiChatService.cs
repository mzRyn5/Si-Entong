using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Store.Application.Abstractions.Repositories;
using Store.Application.Abstractions.Services;
using Store.Application.Services.Inventory;
using Store.Application.Services.Payables;
using Store.Application.Services.Receivables;
using Store.Application.Services.Purchases;
using Store.Application.Services.Sales;
using Store.Application.Services.MasterData;
using Store.Application.Services.Reports;
using Store.Contracts.AiChat;
using Store.Contracts.Requests.Inventory;
using Store.Contracts.Requests.Payables;
using Store.Contracts.Requests.Purchases;
using Store.Contracts.Requests.Receivables;
using Store.Contracts.Requests.Sales;
using Store.Contracts.Requests.Products;
using Store.Contracts.Requests.Customers;
using Store.Contracts.Requests.Suppliers;
using Store.Domain.Entities;
using Store.Domain.Enums;
using Store.Domain.Exceptions;

namespace Store.Application.Services.AiChat;

public class AiChatService : IAiChatService
{
    private readonly IGeminiClient _geminiClient;
    private readonly IAiToolExecutor _toolExecutor;
    private readonly IAiChatRepository _chatRepo;
    private readonly IProductRepository _productRepository;
    private readonly IReportService _reportService;
    private readonly IStoreProfileRepository _storeProfileRepository;
    
    private readonly AiInputGuard _inputGuard;
    private readonly AiAgentLoop _agentLoop;
    private readonly AiResponseComposer _responseComposer;
    private readonly IAiDraftActionService _draftActionService;

    public AiChatService(
        IGeminiClient geminiClient,
        IAiToolExecutor toolExecutor,
        IAiChatRepository chatRepo,
        IProductRepository productRepository,
        IReportService reportService,
        IStoreProfileRepository storeProfileRepository,
        AiInputGuard inputGuard,
        AiAgentLoop agentLoop,
        AiResponseComposer responseComposer,
        IAiDraftActionService draftActionService)
    {
        _geminiClient = geminiClient;
        _toolExecutor = toolExecutor;
        _chatRepo = chatRepo;
        _productRepository = productRepository;
        _reportService = reportService;
        _storeProfileRepository = storeProfileRepository;
        _inputGuard = inputGuard;
        _agentLoop = agentLoop;
        _responseComposer = responseComposer;
        _draftActionService = draftActionService;
    }

    public async Task<AiChatResponse> HandleMessageAsync(AiChatRequest request, Guid userId, Guid storeId)
    {
        // 1. Sanitasi input
        var cleanedMessage = _inputGuard.CleanOrThrow(request.Message);

        if (string.IsNullOrWhiteSpace(cleanedMessage))
        {
            return new AiChatResponse
            {
                Reply = "Pesan tidak boleh kosong.",
                ResponseType = "error"
            };
        }

        // 2. Load or Create Session
        if (string.IsNullOrEmpty(request.SessionId))
        {
            request.SessionId = Guid.NewGuid().ToString();
        }

        if (!Guid.TryParse(request.SessionId, out var sessionId))
        {
            return new AiChatResponse
            {
                Reply = "Session ID tidak valid.",
                ResponseType = "error"
            };
        }

        var session = await _chatRepo.GetOrCreateSessionAsync(sessionId, userId, storeId);

        // 3. Save User Message
        var userIntent = DetectIntent(cleanedMessage);
        await _chatRepo.SaveMessageAsync(session.Id, "user", cleanedMessage, intent: userIntent);

        // 4. Load recent messages (up to 30 last messages)
        var recentMessages = await _chatRepo.GetRecentMessagesAsync(session.Id, AiChatOptions.MaxHistoryMessages);
        var history = recentMessages.Select(m => new AiChatMessageDto
        {
            Role = m.Role,
            Message = m.Role == "tool" ? m.ToolResults ?? "" : (m.Role == "assistant" && !string.IsNullOrEmpty(m.ToolCalls) ? m.ToolCalls : m.Message),
            Intent = m.Intent,
            FunctionName = m.Role == "tool" ? m.FunctionName : (m.Role == "assistant" && !string.IsNullOrEmpty(m.ToolCalls) ? m.FunctionName : null),
            CreatedAt = m.CreatedAt
        }).ToList();

        if (recentMessages.Count >= AiChatOptions.MaxHistoryMessages)
        {
            // Prepend summary pesan lama sebagai context
            var olderMessages = await _chatRepo.GetRecentMessagesAsync(session.Id, 50);
            var summary = BuildConversationSummary(olderMessages.Take(20));
            
            history.Insert(0, new AiChatMessageDto
            {
                Role = "user",
                Message = $"[Ringkasan percakapan sebelumnya: {summary}]",
                CreatedAt = DateTime.UtcNow
            });
        }

        // Build store context snapshot once
        var storeContext = await BuildStoreContextAsync(session.Id, storeId);

        try
        {
            // 5. Agentic Loop
            var loopResult = await _agentLoop.RunAsync(session.Id, history, request, userId, storeId, storeContext);

            // 6. Build Final Response
            var response = await _responseComposer.ComposeAsync(loopResult, session.Id, userId, storeId);

            // 7. Save Assistant Final Text Message
            await _chatRepo.SaveMessageAsync(session.Id, "assistant", response.Reply, response.Intent);

            return response;
        }
        catch (Exception ex)
        {
            if (ex is SecurityException)
            {
                throw;
            }

            return new AiChatResponse
            {
                Reply = "Maaf, koneksi AI belum tersedia. Silakan coba lagi atau gunakan menu manual.",
                ResponseType = "error"
            };
        }
    }

    public async Task<AiActionResponse> ExecuteActionAsync(AiActionRequest request, Guid userId, Guid storeId)
    {
        return await _draftActionService.ExecuteAsync(request, userId, storeId);
    }

    public async Task<List<AiChatMessageDto>> GetSessionHistoryAsync(Guid sessionId, Guid userId)
    {
        var session = await _chatRepo.GetSessionByIdAsync(sessionId);
        if (session == null || session.UserId != userId)
        {
            throw new ForbiddenException("Sesi AI tidak ditemukan atau bukan milik user ini.");
        }

        var messages = await _chatRepo.GetRecentMessagesAsync(sessionId, 50);
        return messages.Select(m => new AiChatMessageDto
        {
            Role = m.Role,
            Message = m.Message,
            Intent = m.Intent,
            FunctionName = m.Role == "tool" ? m.FunctionName : null,
            CreatedAt = m.CreatedAt
        }).ToList();
    }

    public async Task CloseSessionAsync(Guid sessionId, Guid userId)
    {
        await _chatRepo.CloseSessionAsync(sessionId, userId);
    }



    private async Task<StoreContextSnapshot> BuildStoreContextAsync(Guid sessionId, Guid storeId)
    {
        var today = DateTime.UtcNow.Date;
        var dashboard = await _reportService.GetDashboardSummaryAsync(today);
        var storeProfile = await _storeProfileRepository.GetAsync();
        var activeSaleDraft = await _chatRepo.GetActiveDraftAsync(sessionId, "create_sale");

        string? activeSaleDraftJson = null;
        if (activeSaleDraft != null && !string.IsNullOrEmpty(activeSaleDraft.DraftPayload))
        {
            try
            {
                var payload = JsonSerializer.Deserialize<SaleDraftPayload>(activeSaleDraft.DraftPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (payload != null && payload.Items != null && payload.Items.Count > 0)
                {
                    var productIds = payload.Items.Select(i => i.ProductId).Distinct().ToList();
                    var productsDict = (await _productRepository.GetByIdsAsync(productIds))
                        .ToDictionary(p => p.Id);

                    var enrichedItems = new List<object>();
                    foreach (var item in payload.Items)
                    {
                        productsDict.TryGetValue(item.ProductId, out var product);
                        enrichedItems.Add(new
                        {
                            productId = item.ProductId,
                            productName = product?.Name ?? "Produk",
                            quantity = item.Quantity,
                            unitPrice = item.UnitPrice,
                            subtotal = item.Quantity * item.UnitPrice
                        });
                    }

                    activeSaleDraftJson = JsonSerializer.Serialize(new
                    {
                        items = enrichedItems,
                        totalAmount = payload.TotalAmount,
                        paidAmount = payload.PaidAmount
                    });
                }
            }
            catch { }
        }

        return new StoreContextSnapshot
        {
            StoreName = storeProfile?.Name ?? "Toko",
            TodayDate = today.ToString("dddd, dd MMMM yyyy", new System.Globalization.CultureInfo("id-ID")),
            TodaySalesAmount = dashboard.TotalSalesAmount,
            TodaySalesCount = dashboard.TotalSalesTransactions,
            TodayPurchaseAmount = dashboard.TotalPurchaseAmount,
            TodayExpenseAmount = dashboard.TotalExpenseAmount,
            TodayGrossProfit = dashboard.GrossProfitAmount,
            LowStockCount = dashboard.LowStockProductCount,
            OutOfStockCount = dashboard.OutOfStockProductCount,
            ActiveSaleDraftJson = activeSaleDraftJson
        };
    }

    private string? DetectIntent(string message)
    {
        if (message.Contains("jual", StringComparison.OrdinalIgnoreCase) || message.Contains("kasir", StringComparison.OrdinalIgnoreCase)) return "sales";
        if (message.Contains("beli", StringComparison.OrdinalIgnoreCase) || message.Contains("supplier", StringComparison.OrdinalIgnoreCase)) return "purchases";
        if (message.Contains("stok", StringComparison.OrdinalIgnoreCase)) return "inventory";
        if (message.Contains("untung", StringComparison.OrdinalIgnoreCase) || 
            message.Contains("laba", StringComparison.OrdinalIgnoreCase) || 
            message.Contains("profit", StringComparison.OrdinalIgnoreCase)) return "profit";
        if (message.Contains("penjualan", StringComparison.OrdinalIgnoreCase) || 
            message.Contains("omzet", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("pendapatan", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("revenue", StringComparison.OrdinalIgnoreCase)) return "revenue";
        return null;
    }

    private string BuildConversationSummary(IEnumerable<AiChatMessage> messages)
    {
        var summaries = new List<string>();
        foreach (var msg in messages)
        {
            if (msg.Role == "user")
            {
                summaries.Add($"User: {msg.Message}");
            }
            else if (msg.Role == "assistant" && !string.IsNullOrEmpty(msg.Message))
            {
                summaries.Add($"AI: {msg.Message}");
            }
        }
        var combined = string.Join("; ", summaries);
        return combined.Length > 200 ? combined.Substring(0, 200) + "..." : combined;
    }
}

// Helper Payload classes for deserialization
internal class SaleDraftPayload
{
    public List<SaleDraftItem> Items { get; set; } = new();
    public decimal PaidAmount { get; set; }
    public decimal TotalAmount { get; set; }
}

internal class SaleDraftItem
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

internal class PurchaseDraftPayload
{
    public Guid SupplierId { get; set; }
    public List<PurchaseDraftItem> Items { get; set; } = new();
    public decimal TotalAmount { get; set; }
}

internal class PurchaseDraftItem
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitCost { get; set; }
}

internal class PaymentDraftPayload
{
    public Guid CustomerId { get; set; }
    public Guid SupplierId { get; set; }
    public decimal Amount { get; set; }
}

internal class StockAdjustmentDraftPayload
{
    public Guid ProductId { get; set; }
    public int CurrentStock { get; set; }
    public int NewStock { get; set; }
    public int Difference { get; set; }
    public string Reason { get; set; } = string.Empty;
}

internal class ProductUpdateDraftPayload
{
    public Guid ProductId { get; set; }
    public decimal? NewSellingPrice { get; set; }
    public string Reason { get; set; } = string.Empty;
}

internal class ProductDraftPayload
{
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal PurchasePrice { get; set; }
    public int InitialStock { get; set; }
    public Guid CategoryId { get; set; }
    public Guid UnitId { get; set; }
}

internal class CustomerDraftPayload
{
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
}

internal class SupplierDraftPayload
{
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
}
