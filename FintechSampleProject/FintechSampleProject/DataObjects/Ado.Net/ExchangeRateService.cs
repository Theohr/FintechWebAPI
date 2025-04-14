namespace FintechSampleProject.DataObjects.Ado.Net
{
    public class ExchangeRateService : IExchangeRateService
    {
        public Task<decimal> GetExchangeRateAsync(string fromCurrency, string toCurrency)
        {
            // Random Exchange Rates
            //ToDo: Future work to update and fetch exchange rates from external API into the App
            if (fromCurrency == "EUR" && toCurrency == "USD") return Task.FromResult(1.1m);
            if (fromCurrency == "GBP" && toCurrency == "USD") return Task.FromResult(1.3m);
            return Task.FromResult(1m); // Same currency
        }
    }
}
