using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Store.Application.Abstractions.Repositories;
using Store.Application.Abstractions.Services;
using Store.Contracts.AiChat;

namespace Store.Application.Services.AiChat;

public sealed class AiResponseComposer
{
    private readonly IAiChatRepository _chatRepo;

    public AiResponseComposer(IAiChatRepository chatRepo)
    {
        _chatRepo = chatRepo;
    }

    public async Task<AiChatResponse> ComposeAsync(GeminiResult result, Guid sessionId, Guid userId, Guid storeId, CancellationToken cancellationToken = default)
    {
        var response = new AiChatResponse
        {
            Reply = result.Text,
            ResponseType = "text",
            UsedFallback = result.UsedFallback,
            FallbackReason = result.FallbackReason
        };

        if (result.UsedFallback && !string.IsNullOrWhiteSpace(response.Reply))
        {
            response.Reply += "\n\nCatatan: koneksi Gemini asli sedang tidak tersedia, jadi jawaban ini memakai mode fallback lokal.";
        }

        try
        {
            if (result.HasFunctionCall && !string.IsNullOrEmpty(result.Arguments))
            {
                using var doc = JsonDocument.Parse(result.Arguments);
                var root = doc.RootElement;

                if (result.FunctionName == "navigate_to_page" && root.TryGetProperty("pageKey", out var pageEl))
                {
                    var pageKey = pageEl.GetString() ?? string.Empty;
                    var allowedRoutes = new Dictionary<string, string>
                    {
                        { "dashboard", "/dashboard" },
                        { "products", "/products" },
                        { "categories", "/categories" },
                        { "units", "/units" },
                        { "suppliers", "/suppliers" },
                        { "customers", "/customers" },
                        { "inventory", "/inventory" },
                        { "purchases", "/purchases" },
                        { "sales", "/sales" },
                        { "receivables", "/receivables" },
                        { "payables", "/payables" },
                        { "expenses", "/expenses" },
                        { "reports", "/reports" },
                        { "settings", "/settings" },
                        { "users", "/users" },
                        { "audit-logs", "/audit-logs" }
                    };

                    if (allowedRoutes.TryGetValue(pageKey.ToLower(), out var route))
                    {
                        response.ResponseType = "navigation";
                        response.UiAction = new AiUiAction
                        {
                            Type = "navigate",
                            Route = route
                        };
                    }
                }
                else if (result.FunctionName == "fill_current_form" && root.TryGetProperty("formKey", out var formEl) && root.TryGetProperty("fields", out var fieldsEl))
                {
                    response.ResponseType = "fill_form";
                    var fieldsDict = new Dictionary<string, object>();
                    foreach (var prop in fieldsEl.EnumerateObject())
                    {
                        object val = prop.Value.ValueKind switch
                        {
                            JsonValueKind.String => prop.Value.GetString() ?? "",
                            JsonValueKind.Number => prop.Value.GetDecimal(),
                            JsonValueKind.True => true,
                            JsonValueKind.False => false,
                            _ => prop.Value.ToString()
                        };
                        fieldsDict.Add(prop.Name, val);
                    }
                    response.UiAction = new AiUiAction
                    {
                        Type = "fill_form",
                        FormKey = formEl.GetString(),
                        Fields = fieldsDict
                    };
                }
            }

            var latestDraft = await _chatRepo.GetLatestActiveDraftAsync(
                sessionId,
                userId,
                storeId,
                AiChatOptions.DraftActionNames,
                cancellationToken);

            if (latestDraft != null)
            {
                response.ResponseType = "draft_preview";
                response.Intent = latestDraft.ActionName;
                response.DraftId = latestDraft.Id.ToString();
                response.RequiresConfirmation = true;

                string confirmAction = latestDraft.ActionName switch
                {
                    "create_sale" => "confirm_sale",
                    "create_purchase" => "confirm_purchase",
                    "create_receivable_payment" => "confirm_receivable_payment",
                    "create_payable_payment" => "confirm_payable_payment",
                    "create_stock_adjustment" => "confirm_stock_adjustment",
                    "update_product" => "confirm_product_update",
                    "create_product" => "confirm_product_creation",
                    "create_customer" => "confirm_customer_creation",
                    "create_supplier" => "confirm_supplier_creation",
                    _ => "confirm"
                };

                string confirmLabel = latestDraft.ActionName switch
                {
                    "create_sale" => "Konfirmasi Simpan Penjualan",
                    "create_purchase" => "Konfirmasi Simpan Pembelian",
                    "create_receivable_payment" => "Konfirmasi Simpan Pembayaran Piutang",
                    "create_payable_payment" => "Konfirmasi Simpan Pembayaran Hutang",
                    "create_stock_adjustment" => "Konfirmasi Simpan Koreksi Stok",
                    "update_product" => "Konfirmasi Ubah Harga Jual",
                    "create_product" => "Konfirmasi Simpan Produk Baru",
                    "create_customer" => "Konfirmasi Simpan Pelanggan Baru",
                    "create_supplier" => "Konfirmasi Simpan Supplier Baru",
                    _ => "Konfirmasi"
                };

                response.QuickActions = new List<AiQuickAction>
                {
                    new AiQuickAction
                    {
                        Label = confirmLabel,
                        Action = confirmAction,
                        Payload = new { draftId = latestDraft.Id.ToString() }
                    },
                    new AiQuickAction
                    {
                        Label = "Batal",
                        Action = "cancel_draft",
                        Payload = new { draftId = latestDraft.Id.ToString() }
                    }
                };
            }
        }
        catch (Exception)
        {
            response.ResponseType = "text";
        }

        return response;
    }
}
