using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;

namespace Store.Application.Services.AiChat.Tools;

public sealed class UiToolHandler : IAiToolHandler
{
    public IReadOnlyCollection<string> FunctionNames => new[] { "navigate_to_page", "fill_current_form" };

    public object GetDeclaration(string functionName)
    {
        return functionName switch
        {
            "navigate_to_page" => new
            {
                name = "navigate_to_page",
                description = "Navigasi halaman dashboard website",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        pageKey = new { type = "string", description = "Halaman tujuan. Pilihan: dashboard, products, categories, units, suppliers, customers, inventory, purchases, sales, receivables, payables, expenses, reports, settings, users, audit-logs" }
                    },
                    required = new[] { "pageKey" }
                }
            },
            "fill_current_form" => new
            {
                name = "fill_current_form",
                description = "Bantu isikan form yang saat ini aktif",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        formKey = new { type = "string", description = "Key form" },
                        fields = new { type = "object", description = "Key-value data field form" }
                    },
                    required = new[] { "formKey", "fields" }
                }
            },
            _ => throw new ArgumentException($"Unknown function: {functionName}")
        };
    }

    public Task<object> ExecuteAsync(AiToolExecutionContext context, CancellationToken cancellationToken = default)
    {
        var root = context.Arguments;
        switch (context.FunctionName)
        {
            case "navigate_to_page":
                {
                    var pageKey = root.GetProperty("pageKey").GetString() ?? string.Empty;
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
                        return Task.FromResult<object>(new
                        {
                            uiAction = new
                            {
                                type = "navigate",
                                route = route
                            },
                            reply = $"Mengalihkan halaman ke {pageKey}..."
                        });
                    }

                    return Task.FromResult<object>(new { error = $"Halaman '{pageKey}' tidak ditemukan atau tidak diperbolehkan." });
                }

            case "fill_current_form":
                {
                    var formKey = root.GetProperty("formKey").GetString();
                    var fields = root.GetProperty("fields");

                    var fieldsDict = new Dictionary<string, object>();
                    foreach (var field in fields.EnumerateObject())
                    {
                        object val = field.Value.ValueKind switch
                        {
                            JsonValueKind.String => field.Value.GetString() ?? "",
                            JsonValueKind.Number => field.Value.GetDecimal(),
                            JsonValueKind.True => true,
                            JsonValueKind.False => false,
                            _ => field.Value.ToString()
                        };
                        fieldsDict.Add(field.Name, val);
                    }

                    return Task.FromResult<object>(new
                    {
                        uiAction = new
                        {
                            type = "fill_form",
                            formKey = formKey,
                            fields = fieldsDict
                        },
                        reply = "Mengisi form otomatis..."
                    });
                }

            default:
                throw new ArgumentException($"Unsupported function: {context.FunctionName}");
        }
    }
}
