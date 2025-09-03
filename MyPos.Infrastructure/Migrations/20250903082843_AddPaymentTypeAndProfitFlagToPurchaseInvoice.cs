using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyPos.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentTypeAndProfitFlagToPurchaseInvoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DoesNotAffectProfit",
                table: "PurchaseInvoices",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "PaymentTypeId",
                table: "PurchaseInvoices",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseInvoices_PaymentTypeId",
                table: "PurchaseInvoices",
                column: "PaymentTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseInvoices_PaymentTypes_PaymentTypeId",
                table: "PurchaseInvoices",
                column: "PaymentTypeId",
                principalTable: "PaymentTypes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseInvoices_PaymentTypes_PaymentTypeId",
                table: "PurchaseInvoices");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseInvoices_PaymentTypeId",
                table: "PurchaseInvoices");

            migrationBuilder.DropColumn(
                name: "DoesNotAffectProfit",
                table: "PurchaseInvoices");

            migrationBuilder.DropColumn(
                name: "PaymentTypeId",
                table: "PurchaseInvoices");
        }
    }
}
