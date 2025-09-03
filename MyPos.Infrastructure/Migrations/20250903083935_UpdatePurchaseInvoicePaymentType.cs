using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyPos.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePurchaseInvoicePaymentType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseInvoices_PaymentTypes_PaymentTypeId",
                table: "PurchaseInvoices");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseInvoices_PaymentTypes_PaymentTypeId",
                table: "PurchaseInvoices",
                column: "PaymentTypeId",
                principalTable: "PaymentTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseInvoices_PaymentTypes_PaymentTypeId",
                table: "PurchaseInvoices");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseInvoices_PaymentTypes_PaymentTypeId",
                table: "PurchaseInvoices",
                column: "PaymentTypeId",
                principalTable: "PaymentTypes",
                principalColumn: "Id");
        }
    }
}
