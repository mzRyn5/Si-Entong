using System.Collections.Generic;

namespace Store.Application.Services.AiChat;

public static class AiChatOptions
{
    public const int MaxAgentLoops = 5;
    public const int MaxHistoryMessages = 30;

    public static readonly IReadOnlyCollection<string> DraftActionNames = new[]
    {
        "create_sale",
        "create_purchase",
        "create_receivable_payment",
        "create_payable_payment",
        "create_stock_adjustment",
        "update_product",
        "create_product",
        "create_customer",
        "create_supplier"
    };
}
