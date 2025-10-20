using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyPos.Application.Dtos.Reports
{
    public class SummaryBoxDto
    {
        public decimal Total { get; set; }
        public decimal Cash { get; set; }
        public decimal Pos { get; set; }
    }

    /// <summary>
    /// Görseldeki 4 Kutu Özeti için Ana DTO
    /// </summary>
    public class CashFlowDetailDto
    {
        public decimal Total { get; set; }
        public decimal Cash { get; set; }
        public decimal Pos { get; set; }
    }

    /// <summary>
    /// Günlük Nakit Akışı Raporu için Ana DTO
    /// </summary>
    public class DailyCashFlowReportDto
    {
        // Girişler (Tahsilatlar)
        public CashFlowDetailDto CustomerPayments { get; set; } = new CashFlowDetailDto(); // Müşterinin bize yaptığı ödemeler (Tahsilatlar)
        public CashFlowDetailDto Incomes { get; set; } = new CashFlowDetailDto();          // Diğer gelirler

        // Çıkışlar (Ödemeler)
        public CashFlowDetailDto CompanyPayments { get; set; } = new CashFlowDetailDto();   // Firma ödemeleri
        public CashFlowDetailDto Expenses { get; set; } = new CashFlowDetailDto();          // İşletme giderleri

        // Genel Akış Özeti (Satışlar da dahil edilerek hesaplanacak)
        public decimal TotalCashInflow { get; set; }
        public decimal TotalPosInflow { get; set; }

        public decimal TotalCashOutflow { get; set; }
        public decimal TotalPosOutflow { get; set; }

        public decimal NetCashChange { get; set; }
        public decimal NetPosChange { get; set; }
    }

    // (DailySummaryDto eski haliyle burada kalır)
    public class DailySummaryDto
    {
        public decimal SingleCashSales { get; set; }
        public decimal SplitCashSales { get; set; }
        public decimal SinglePosSales { get; set; }
        public decimal SplitPosSales { get; set; }
        public decimal SingleOpenAccountSales { get; set; }

        public decimal TotalCash { get; set; }
        public decimal TotalPos { get; set; }
        public decimal TotalOpenAccount { get; set; }

        public decimal TotalSales { get; set; }
        public decimal DirectDebtAdditions { get; set; }
    }

    // KONTROL: Buradaki RevenueReportDto'yu RevenueSummaryDto olarak değiştiriyoruz.
    public class ComprehensiveDailySummaryDto
    {
        public DailyCashRegisterReportDto CashRegisterReport { get; set; } = new DailyCashRegisterReportDto();
        public ProfitReportDto ProfitReport { get; set; } = new ProfitReportDto();
        public RevenueSummaryDto RevenueSummary { get; set; } = new RevenueSummaryDto(); // İSİM GÜNCELLENDİ
        public ProductCostReportDto ProductCostReport { get; set; } = new ProductCostReportDto();
    }

    public class DailyCashRegisterReportDto
    {
        public decimal CashSales { get; set; }
        public decimal CashIncomes { get; set; }
        public decimal CashCustomerPayments { get; set; }
        public decimal CashCompanyPayments { get; set; }
        public decimal CashExpenses { get; set; }
        public decimal CashPurchaseInvoicePayments { get; set; }
        public decimal NetCashBalance { get; set; }
    }

    public class ProfitReportDto
    {
        public decimal GrossProfit { get; set; }
    }

    // YENİ İSİM: RevenueSummaryDto
    public class RevenueSummaryDto
    {
        public decimal TotalSalesRevenue { get; set; }
        public decimal CustomerPayments { get; set; }
        public decimal Incomes { get; set; }
        public decimal Expenses { get; set; }
        public decimal NetRevenue { get; set; }
    }

    public class ProductCostReportDto
    {
        public decimal TotalCostOfGoodsSold { get; set; }
    }
    public class DailyCustomerPaymentDetailDto
    {
        public int Id { get; set; }
        public string CustomerName { get; set; }
        public decimal Amount { get; set; }
        public string PaymentType { get; set; }
        public string Note { get; set; }
        public string Time { get; set; }
    }
    public class DailyCompanyTransactionDto
    {
        public int CompanyId { get; set; }
        public string CompanyName { get; set; }
        public decimal TotalDebt { get; set; }   // Borç tutarı
        public decimal TotalPayment { get; set; } // Ödeme tutarı
    }
}
