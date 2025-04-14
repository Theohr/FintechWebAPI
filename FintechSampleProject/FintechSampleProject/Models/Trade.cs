namespace FintechSampleProject.Models
{ 
    public class Trade
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string TradeType { get; set; }
        public string Status { get; set; }
        public DateTime CreationDate { get; set; }
        public int AccountId { get; set; }
    }
}
