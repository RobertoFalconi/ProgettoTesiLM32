using System.ComponentModel.DataAnnotations;

namespace ProgettoTesi.Models
{
    public class StrumentoFinanziarioViewModel
    {
        public long? Id { get; set; }
        public string? UserId { get; set; }
        public string? Nome { get; set; }
        [Required(ErrorMessage = "Inserire un titolo per proseguire la ricerca")]
        public string? Simbolo { get; set; }
        public string? Descrizione { get; set; }
        public List<string>? ListSimboli { get; set; }

        public string? TotalRevenue { get; set; }
        public string? GrossProfits { get; set; }
        public string? ProfitMargins { get; set; }
        public string? EbitdaMargins { get; set; }
        public string? CurrentPrice { get; set; }
        public string? TargetHighPrice { get; set; }
        public string? TargetMedianPrice { get; set; }
        public string? TargetLowPrice { get; set; }
        public string? EarningsGrowth { get; set; }
        public string? RevenueGrowth { get; set; }
        public string? OperatingCashflow { get; set; }
        public string? FinancialCurrency { get; set; }

    }
}
