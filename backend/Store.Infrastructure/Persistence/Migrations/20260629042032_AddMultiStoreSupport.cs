using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Store.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiStoreSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_units_name",
                table: "units");

            migrationBuilder.DropIndex(
                name: "IX_products_sku",
                table: "products");

            migrationBuilder.DropIndex(
                name: "IX_categories_name",
                table: "categories");

            migrationBuilder.AddColumn<Guid>(
                name: "store_id",
                table: "users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "store_id",
                table: "units",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "store_id",
                table: "suppliers",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "store_id",
                table: "store_settings",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "store_profiles",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<Guid>(
                name: "store_id",
                table: "StockOpnames",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "store_id",
                table: "stock_movements",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "store_id",
                table: "stock_adjustments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "store_id",
                table: "SalesReturns",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "store_id",
                table: "sales",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "store_id",
                table: "receivables",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "store_id",
                table: "purchases",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "store_id",
                table: "PurchaseReturns",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "store_id",
                table: "products",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "store_id",
                table: "payables",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "store_id",
                table: "expenses",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "store_id",
                table: "expense_categories",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "store_id",
                table: "customers",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "store_id",
                table: "categories",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "store_id",
                table: "CashMovements",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "store_id",
                table: "cash_sessions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "store_id",
                table: "audit_logs",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_store_id",
                table: "users",
                column: "store_id");

            migrationBuilder.CreateIndex(
                name: "IX_units_store_id",
                table: "units",
                column: "store_id");

            migrationBuilder.CreateIndex(
                name: "IX_units_store_id_name",
                table: "units",
                columns: new[] { "store_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_suppliers_store_id",
                table: "suppliers",
                column: "store_id");

            migrationBuilder.CreateIndex(
                name: "IX_store_settings_store_id",
                table: "store_settings",
                column: "store_id");

            migrationBuilder.CreateIndex(
                name: "IX_StockOpnames_store_id",
                table: "StockOpnames",
                column: "store_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_store_id",
                table: "stock_movements",
                column: "store_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_adjustments_store_id",
                table: "stock_adjustments",
                column: "store_id");

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturns_store_id",
                table: "SalesReturns",
                column: "store_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_store_id",
                table: "sales",
                column: "store_id");

            migrationBuilder.CreateIndex(
                name: "IX_receivables_store_id",
                table: "receivables",
                column: "store_id");

            migrationBuilder.CreateIndex(
                name: "IX_purchases_store_id",
                table: "purchases",
                column: "store_id");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReturns_store_id",
                table: "PurchaseReturns",
                column: "store_id");

            migrationBuilder.CreateIndex(
                name: "IX_products_store_id",
                table: "products",
                column: "store_id");

            migrationBuilder.CreateIndex(
                name: "IX_products_store_id_sku",
                table: "products",
                columns: new[] { "store_id", "sku" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payables_store_id",
                table: "payables",
                column: "store_id");

            migrationBuilder.CreateIndex(
                name: "IX_expenses_store_id",
                table: "expenses",
                column: "store_id");

            migrationBuilder.CreateIndex(
                name: "IX_expense_categories_store_id",
                table: "expense_categories",
                column: "store_id");

            migrationBuilder.CreateIndex(
                name: "IX_customers_store_id",
                table: "customers",
                column: "store_id");

            migrationBuilder.CreateIndex(
                name: "IX_categories_store_id",
                table: "categories",
                column: "store_id");

            migrationBuilder.CreateIndex(
                name: "IX_categories_store_id_name",
                table: "categories",
                columns: new[] { "store_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CashMovements_store_id",
                table: "CashMovements",
                column: "store_id");

            migrationBuilder.CreateIndex(
                name: "IX_cash_sessions_store_id",
                table: "cash_sessions",
                column: "store_id");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_store_id",
                table: "audit_logs",
                column: "store_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_users_store_id",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_units_store_id",
                table: "units");

            migrationBuilder.DropIndex(
                name: "IX_units_store_id_name",
                table: "units");

            migrationBuilder.DropIndex(
                name: "IX_suppliers_store_id",
                table: "suppliers");

            migrationBuilder.DropIndex(
                name: "IX_store_settings_store_id",
                table: "store_settings");

            migrationBuilder.DropIndex(
                name: "IX_StockOpnames_store_id",
                table: "StockOpnames");

            migrationBuilder.DropIndex(
                name: "IX_stock_movements_store_id",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "IX_stock_adjustments_store_id",
                table: "stock_adjustments");

            migrationBuilder.DropIndex(
                name: "IX_SalesReturns_store_id",
                table: "SalesReturns");

            migrationBuilder.DropIndex(
                name: "IX_sales_store_id",
                table: "sales");

            migrationBuilder.DropIndex(
                name: "IX_receivables_store_id",
                table: "receivables");

            migrationBuilder.DropIndex(
                name: "IX_purchases_store_id",
                table: "purchases");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseReturns_store_id",
                table: "PurchaseReturns");

            migrationBuilder.DropIndex(
                name: "IX_products_store_id",
                table: "products");

            migrationBuilder.DropIndex(
                name: "IX_products_store_id_sku",
                table: "products");

            migrationBuilder.DropIndex(
                name: "IX_payables_store_id",
                table: "payables");

            migrationBuilder.DropIndex(
                name: "IX_expenses_store_id",
                table: "expenses");

            migrationBuilder.DropIndex(
                name: "IX_expense_categories_store_id",
                table: "expense_categories");

            migrationBuilder.DropIndex(
                name: "IX_customers_store_id",
                table: "customers");

            migrationBuilder.DropIndex(
                name: "IX_categories_store_id",
                table: "categories");

            migrationBuilder.DropIndex(
                name: "IX_categories_store_id_name",
                table: "categories");

            migrationBuilder.DropIndex(
                name: "IX_CashMovements_store_id",
                table: "CashMovements");

            migrationBuilder.DropIndex(
                name: "IX_cash_sessions_store_id",
                table: "cash_sessions");

            migrationBuilder.DropIndex(
                name: "IX_audit_logs_store_id",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "store_id",
                table: "users");

            migrationBuilder.DropColumn(
                name: "store_id",
                table: "units");

            migrationBuilder.DropColumn(
                name: "store_id",
                table: "suppliers");

            migrationBuilder.DropColumn(
                name: "store_id",
                table: "store_settings");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "store_profiles");

            migrationBuilder.DropColumn(
                name: "store_id",
                table: "StockOpnames");

            migrationBuilder.DropColumn(
                name: "store_id",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "store_id",
                table: "stock_adjustments");

            migrationBuilder.DropColumn(
                name: "store_id",
                table: "SalesReturns");

            migrationBuilder.DropColumn(
                name: "store_id",
                table: "sales");

            migrationBuilder.DropColumn(
                name: "store_id",
                table: "receivables");

            migrationBuilder.DropColumn(
                name: "store_id",
                table: "purchases");

            migrationBuilder.DropColumn(
                name: "store_id",
                table: "PurchaseReturns");

            migrationBuilder.DropColumn(
                name: "store_id",
                table: "products");

            migrationBuilder.DropColumn(
                name: "store_id",
                table: "payables");

            migrationBuilder.DropColumn(
                name: "store_id",
                table: "expenses");

            migrationBuilder.DropColumn(
                name: "store_id",
                table: "expense_categories");

            migrationBuilder.DropColumn(
                name: "store_id",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "store_id",
                table: "categories");

            migrationBuilder.DropColumn(
                name: "store_id",
                table: "CashMovements");

            migrationBuilder.DropColumn(
                name: "store_id",
                table: "cash_sessions");

            migrationBuilder.DropColumn(
                name: "store_id",
                table: "audit_logs");

            migrationBuilder.CreateIndex(
                name: "IX_units_name",
                table: "units",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_products_sku",
                table: "products",
                column: "sku",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_categories_name",
                table: "categories",
                column: "name",
                unique: true);
        }
    }
}
