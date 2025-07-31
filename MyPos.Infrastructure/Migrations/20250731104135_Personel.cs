using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyPos.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Personel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Personnel",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ViewCustomer = table.Column<bool>(type: "bit", nullable: false),
                    AddOrUpdateProduct = table.Column<bool>(type: "bit", nullable: false),
                    DeleteProduct = table.Column<bool>(type: "bit", nullable: false),
                    ManageCompany = table.Column<bool>(type: "bit", nullable: false),
                    ViewIncomeExpense = table.Column<bool>(type: "bit", nullable: false),
                    PurchaseInvoiceFullAccess = table.Column<bool>(type: "bit", nullable: false),
                    PurchaseInvoiceCreate = table.Column<bool>(type: "bit", nullable: false),
                    StockCount = table.Column<bool>(type: "bit", nullable: false),
                    SalesDiscount = table.Column<bool>(type: "bit", nullable: false),
                    DailyReport = table.Column<bool>(type: "bit", nullable: false),
                    HistoricalReport = table.Column<bool>(type: "bit", nullable: false),
                    ViewTurnoverInReports = table.Column<bool>(type: "bit", nullable: false),
                    MaxDiscount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DiscountCurrency = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IdentityNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EmergencyContact = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BloodType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartingSalary = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CurrentSalary = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IBAN = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImagePath = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Personnel", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Personnel");
        }
    }
}
