using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyPos.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SaleCodeTotalQuantitiy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SaleCode",
                table: "Sales",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TotalQuantity",
                table: "Sales",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SaleCode",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "TotalQuantity",
                table: "Sales");
        }
    }
}
