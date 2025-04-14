namespace FintechSampleProject.Models
{
    public class Preferences
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public string DefaultCurrency { get; set; }
        public int DefaultTradeSize { get; set; }
    }
}
