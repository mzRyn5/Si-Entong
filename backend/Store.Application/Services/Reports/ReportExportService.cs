using System.Globalization;
using System.Net;
using System.Text;
using Store.Contracts.Responses.Reports;

namespace Store.Application.Services.Reports;

public interface IReportExportService
{
    byte[] ExportDailySales(IEnumerable<DailySalesReportResponse> rows);
    byte[] ExportPurchases(IEnumerable<PurchaseReportResponse> rows);
    byte[] ExportStockValuation(IEnumerable<StockValuationReportResponse> rows);
    byte[] ExportBasicProfit(BasicProfitReportResponse row);
}

public class ReportExportService : IReportExportService
{
    public byte[] ExportDailySales(IEnumerable<DailySalesReportResponse> rows)
    {
        var tableRows = rows.Select(row => new object?[]
        {
            row.Date,
            row.TransactionCount,
            row.TotalSalesAmount,
            row.TotalDiscountAmount,
            row.NetSalesAmount
        });

        return BuildWorkbook("Daily Sales", ["Date", "Transaction Count", "Total Sales", "Discount", "Net Sales"], tableRows);
    }

    public byte[] ExportPurchases(IEnumerable<PurchaseReportResponse> rows)
    {
        var tableRows = rows.Select(row => new object?[]
        {
            row.PurchaseDate,
            row.PurchaseNumber,
            row.SupplierName,
            row.GrossPurchaseAmount,
            row.ReturnAmount,
            row.NetPurchaseAmount,
            row.PaymentMethod
        });

        return BuildWorkbook("Purchases", ["Date", "Purchase Number", "Supplier", "Gross Purchase", "Return", "Net Purchase", "Payment Method"], tableRows);
    }

    public byte[] ExportStockValuation(IEnumerable<StockValuationReportResponse> rows)
    {
        var tableRows = rows.Select(row => new object?[]
        {
            row.ProductName,
            row.CurrentStock,
            row.PurchasePrice,
            row.StockValue
        });

        return BuildWorkbook("Stock Valuation", ["Product", "Current Stock", "Purchase Price", "Stock Value"], tableRows);
    }

    public byte[] ExportBasicProfit(BasicProfitReportResponse row)
    {
        var tableRows = new[]
        {
            new object?[] { "From Date", row.FromDate },
            new object?[] { "To Date", row.ToDate },
            new object?[] { "Net Sales", row.NetSalesAmount },
            new object?[] { "Estimated COGS", row.EstimatedCostOfGoodsSold },
            new object?[] { "Estimated Gross Profit", row.EstimatedGrossProfit },
            new object?[] { "Expense", row.ExpenseAmount },
            new object?[] { "Estimated Net Profit", row.EstimatedNetProfit }
        };

        return BuildWorkbook("Basic Profit", ["Metric", "Value"], tableRows);
    }

    private static byte[] BuildWorkbook(
        string worksheetName,
        string[] headers,
        IEnumerable<object?[]> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine("""<?xml version="1.0"?>""");
        builder.AppendLine("""<?mso-application progid="Excel.Sheet"?>""");
        builder.AppendLine("""<Workbook xmlns="urn:schemas-microsoft-com:office:spreadsheet" xmlns:ss="urn:schemas-microsoft-com:office:spreadsheet">""");
        builder.AppendLine($"""<Worksheet ss:Name="{Xml(worksheetName)}"><Table>""");
        builder.AppendLine("<Row>");
        foreach (var header in headers)
        {
            builder.AppendLine($"""<Cell><Data ss:Type="String">{Xml(header)}</Data></Cell>""");
        }
        builder.AppendLine("</Row>");

        foreach (var row in rows)
        {
            builder.AppendLine("<Row>");
            foreach (var cell in row)
            {
                builder.AppendLine(ToCell(cell));
            }
            builder.AppendLine("</Row>");
        }

        builder.AppendLine("</Table></Worksheet></Workbook>");
        return Encoding.UTF8.GetBytes(builder.ToString());
    }

    private static string ToCell(object? value)
    {
        if (value == null)
        {
            return """<Cell><Data ss:Type="String"></Data></Cell>""";
        }

        return value switch
        {
            DateTime date => $"""<Cell><Data ss:Type="DateTime">{date:yyyy-MM-ddTHH:mm:ss}</Data></Cell>""",
            int number => $"""<Cell><Data ss:Type="Number">{number.ToString(CultureInfo.InvariantCulture)}</Data></Cell>""",
            decimal number => $"""<Cell><Data ss:Type="Number">{number.ToString(CultureInfo.InvariantCulture)}</Data></Cell>""",
            double number => $"""<Cell><Data ss:Type="Number">{number.ToString(CultureInfo.InvariantCulture)}</Data></Cell>""",
            _ => $"""<Cell><Data ss:Type="String">{Xml(value.ToString() ?? string.Empty)}</Data></Cell>"""
        };
    }

    private static string Xml(string value)
    {
        return WebUtility.HtmlEncode(value);
    }
}
