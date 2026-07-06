using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Store.Application.Abstractions.Repositories;
using Store.Domain.Entities;
using Store.Domain.Exceptions;

namespace Store.Application.Services.AiChat.Tools;

public sealed class PartnerDebtToolHandler : IAiToolHandler
{
    private readonly ISupplierRepository _supplierRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IReceivableRepository _receivableRepository;
    private readonly IPayableRepository _payableRepository;
    private readonly IAiChatRepository _aiChatRepository;

    public PartnerDebtToolHandler(
        ISupplierRepository supplierRepository,
        ICustomerRepository customerRepository,
        IReceivableRepository receivableRepository,
        IPayableRepository payableRepository,
        IAiChatRepository aiChatRepository)
    {
        _supplierRepository = supplierRepository;
        _customerRepository = customerRepository;
        _receivableRepository = receivableRepository;
        _payableRepository = payableRepository;
        _aiChatRepository = aiChatRepository;
    }

    public IReadOnlyCollection<string> FunctionNames => new[]
    {
        "search_customer",
        "search_supplier",
        "get_receivables",
        "create_receivable_payment_draft",
        "create_payable_payment_draft"
    };

    public object GetDeclaration(string functionName)
    {
        return functionName switch
        {
            "search_customer" => new
            {
                name = "search_customer",
                description = "Cari pelanggan berdasarkan nama di database",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        keyword = new { type = "string", description = "Nama pelanggan" }
                    },
                    required = new[] { "keyword" }
                }
            },
            "search_supplier" => new
            {
                name = "search_supplier",
                description = "Cari supplier berdasarkan nama di database",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        keyword = new { type = "string", description = "Nama supplier" }
                    },
                    required = new[] { "keyword" }
                }
            },
            "get_receivables" => new
            {
                name = "get_receivables",
                description = "Dapatkan sisa piutang pelanggan",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        customerId = new { type = "string", description = "GUID pelanggan" }
                    },
                    required = new[] { "customerId" }
                }
            },
            "create_receivable_payment_draft" => new
            {
                name = "create_receivable_payment_draft",
                description = "Buat draft pembayaran piutang oleh pelanggan",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        customerId = new { type = "string", description = "GUID pelanggan" },
                        amount = new { type = "number", description = "Nominal pembayaran piutang" }
                    },
                    required = new[] { "customerId", "amount" }
                }
            },
            "create_payable_payment_draft" => new
            {
                name = "create_payable_payment_draft",
                description = "Buat draft pembayaran hutang ke supplier",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        supplierId = new { type = "string", description = "GUID supplier" },
                        amount = new { type = "number", description = "Nominal pembayaran hutang" }
                    },
                    required = new[] { "supplierId", "amount" }
                }
            },
            _ => throw new ArgumentException($"Unknown function: {functionName}")
        };
    }

    public async Task<object> ExecuteAsync(AiToolExecutionContext context, CancellationToken cancellationToken = default)
    {
        var root = context.Arguments;
        switch (context.FunctionName)
        {
            case "search_customer":
                {
                    var keyword = root.GetProperty("keyword").GetString() ?? string.Empty;
                    var customers = await _customerRepository.GetAllAsync(keyword, true, 1, 10, cancellationToken);
                    foreach (var c in customers)
                    {
                        if (c.StoreId != context.StoreId)
                        {
                            throw new ForbiddenException("Akses ditolak: Pelanggan bukan milik toko ini.");
                        }
                    }

                    return customers.Select(c => new
                    {
                        customerId = c.Id,
                        name = c.Name,
                        phone = c.Phone
                    }).ToList();
                }

            case "search_supplier":
                {
                    var keyword = root.GetProperty("keyword").GetString() ?? string.Empty;
                    var suppliers = await _supplierRepository.GetAllAsync(keyword, true, 1, 10, cancellationToken);
                    foreach (var s in suppliers)
                    {
                        if (s.StoreId != context.StoreId)
                        {
                            throw new ForbiddenException("Akses ditolak: Supplier bukan milik toko ini.");
                        }
                    }

                    return suppliers.Select(s => new
                    {
                        supplierId = s.Id,
                        name = s.Name,
                        phone = s.Phone,
                        address = s.Address
                    }).ToList();
                }

            case "get_receivables":
                {
                    var customerIdStr = root.GetProperty("customerId").GetString();
                    if (string.IsNullOrEmpty(customerIdStr) || !Guid.TryParse(customerIdStr, out var customerId))
                    {
                        return new { error = "CustomerId tidak valid." };
                    }

                    var customer = await _customerRepository.GetByIdAsync(customerId, cancellationToken);
                    if (customer == null)
                    {
                        return new { error = "Pelanggan tidak ditemukan." };
                    }
                    if (customer.StoreId != context.StoreId)
                    {
                        throw new ForbiddenException("Akses ditolak: Pelanggan bukan milik toko ini.");
                    }

                    var receivables = await _receivableRepository.GetAllAsync(customerId, "unpaid", 1, 100, cancellationToken);
                    var receivablesList = receivables.ToList();
                    
                    var partialReceivables = await _receivableRepository.GetAllAsync(customerId, "partial", 1, 100, cancellationToken);
                    receivablesList.AddRange(partialReceivables);

                    decimal totalDebt = receivablesList.Sum(r => r.RemainingAmount);

                    return new
                    {
                        customerId = customerId,
                        totalDebt = totalDebt,
                        receivables = receivablesList.Select(r => new
                        {
                            id = r.Id,
                            receivableNumber = r.ReceivableNumber,
                            remainingAmount = r.RemainingAmount,
                            dueDate = r.DueDate
                        }).ToList()
                    };
                }

            case "create_receivable_payment_draft":
                {
                    var customerIdStr = root.GetProperty("customerId").GetString();
                    if (string.IsNullOrEmpty(customerIdStr) || !Guid.TryParse(customerIdStr, out var customerId))
                    {
                        return new { error = "CustomerId tidak valid." };
                    }

                    var customer = await _customerRepository.GetByIdAsync(customerId, cancellationToken);
                    if (customer == null)
                    {
                        return new { error = "Pelanggan tidak ditemukan." };
                    }
                    if (customer.StoreId != context.StoreId)
                    {
                        throw new ForbiddenException("Akses ditolak: Pelanggan bukan milik toko ini.");
                    }

                    var amount = root.GetProperty("amount").GetDecimal();
                    if (amount <= 0)
                    {
                        return new { error = "Nominal pembayaran harus lebih besar dari 0." };
                    }

                    var receivables = await _receivableRepository.GetAllAsync(customerId, "unpaid", 1, 100, cancellationToken);
                    var receivablesList = receivables.ToList();
                    var partialReceivables = await _receivableRepository.GetAllAsync(customerId, "partial", 1, 100, cancellationToken);
                    receivablesList.AddRange(partialReceivables);

                    decimal totalDebt = receivablesList.Sum(r => r.RemainingAmount);
                    if (amount > totalDebt)
                    {
                        return new { error = $"Nominal pembayaran (Rp{amount:N0}) melebihi total piutang pelanggan (Rp{totalDebt:N0})." };
                    }

                    var draft = new AiActionDraft
                    {
                        SessionId = context.SessionId,
                        UserId = context.UserId,
                        ActionName = "create_receivable_payment",
                        EntityType = "customer",
                        DraftPayload = JsonSerializer.Serialize(new
                        {
                            customerId = customerId,
                            amount = amount
                        }),
                        IdempotencyKey = Guid.NewGuid().ToString(),
                        Status = "pending",
                        ExpiredAt = DateTime.UtcNow.AddMinutes(10)
                    };

                    await _aiChatRepository.SaveDraftAsync(draft, cancellationToken);

                    return new
                    {
                        draftId = draft.Id,
                        customerName = customer.Name,
                        amount = amount,
                        remainingDebtAfterPayment = totalDebt - amount,
                        status = "pending",
                        expiredAt = draft.ExpiredAt
                    };
                }

            case "create_payable_payment_draft":
                {
                    var supplierIdStr = root.GetProperty("supplierId").GetString();
                    if (string.IsNullOrEmpty(supplierIdStr) || !Guid.TryParse(supplierIdStr, out var supplierId))
                    {
                        return new { error = "SupplierId tidak valid." };
                    }

                    var supplier = await _supplierRepository.GetByIdAsync(supplierId, cancellationToken);
                    if (supplier == null)
                    {
                        return new { error = "Supplier tidak ditemukan." };
                    }
                    if (supplier.StoreId != context.StoreId)
                    {
                        throw new ForbiddenException("Akses ditolak: Supplier bukan milik toko ini.");
                    }

                    var amount = root.GetProperty("amount").GetDecimal();
                    if (amount <= 0)
                    {
                        return new { error = "Nominal pembayaran harus lebih besar dari 0." };
                    }

                    var payables = await _payableRepository.GetAllAsync(supplierId, "unpaid", 1, 100, cancellationToken);
                    var payablesList = payables.ToList();
                    var partialPayables = await _payableRepository.GetAllAsync(supplierId, "partial", 1, 100, cancellationToken);
                    payablesList.AddRange(partialPayables);

                    decimal totalDebt = payablesList.Sum(p => p.RemainingAmount);
                    if (amount > totalDebt)
                    {
                        return new { error = $"Nominal pembayaran (Rp{amount:N0}) melebihi total hutang ke supplier (Rp{totalDebt:N0})." };
                    }

                    var draft = new AiActionDraft
                    {
                        SessionId = context.SessionId,
                        UserId = context.UserId,
                        ActionName = "create_payable_payment",
                        EntityType = "supplier",
                        DraftPayload = JsonSerializer.Serialize(new
                        {
                            supplierId = supplierId,
                            amount = amount
                        }),
                        IdempotencyKey = Guid.NewGuid().ToString(),
                        Status = "pending",
                        ExpiredAt = DateTime.UtcNow.AddMinutes(10)
                    };

                    await _aiChatRepository.SaveDraftAsync(draft, cancellationToken);

                    return new
                    {
                        draftId = draft.Id,
                        supplierName = supplier.Name,
                        amount = amount,
                        remainingDebtAfterPayment = totalDebt - amount,
                        status = "pending",
                        expiredAt = draft.ExpiredAt
                    };
                }

            default:
                throw new ArgumentException($"Unsupported function: {context.FunctionName}");
        }
    }
}
