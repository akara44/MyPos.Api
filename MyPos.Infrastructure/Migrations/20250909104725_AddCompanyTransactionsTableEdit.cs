using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyPos.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyTransactionsTableEdit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CompanyTransactions_PurchaseInvoices_PurchaseInvoiceId",
                table: "CompanyTransactions");

            migrationBuilder.DropIndex(
                name: "IX_CompanyTransactions_PurchaseInvoiceId",
                table: "CompanyTransactions");

            migrationBuilder.DropColumn(
                name: "PurchaseInvoiceId",
                table: "CompanyTransactions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PurchaseInvoiceId",
                table: "CompanyTransactions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompanyTransactions_PurchaseInvoiceId",
                table: "CompanyTransactions",
                column: "PurchaseInvoiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_CompanyTransactions_PurchaseInvoices_PurchaseInvoiceId",
                table: "CompanyTransactions",
                column: "PurchaseInvoiceId",
                principalTable: "PurchaseInvoices",
                principalColumn: "Id");
        }
    }
}
