using WebMarketCompare.Models;

namespace WebMarketCompare.Services.YandexMarket
{
    public interface IYaMParserService
    {
        Task<Product> ParseProductAsync(string productUrl);
    }
}
