using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyPos.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SplitPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PaymentTypeName",
                table: "Payments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "SaleId",
                table: "Payments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_SaleId",
                table: "Payments",
                column: "SaleId");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Sales_SaleId",
                table: "Payments",
                column: "SaleId",
                principalTable: "Sales",
                principalColumn: "SaleId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Sales_SaleId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_SaleId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "PaymentTypeName",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "SaleId",
                table: "Payments");
        }
    }
}
