namespace MyPos.Application.Dtos
{
    public class CompanyFinancialSummaryDto
    {
        public decimal TotalDebt { get; set; }
        public decimal TotalPayments { get; set; }
        public decimal RemainingDebt { get; set; }
    }
}