namespace FintechSampleProject.Models
{
    public class DailyTradeSummary
    {
        public DateTime Date { get; set; } 
        public int TotalTrades { get; set; } 
        public decimal TotalAmount { get; set; } 
        public int BuyTrades { get; set; } 
        public int SellTrades { get; set; } 
    }
}
