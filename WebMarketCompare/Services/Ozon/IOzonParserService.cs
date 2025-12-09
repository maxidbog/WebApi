using WebMarketCompare.Models;

namespace WebMarketCompare.Services.Ozon
{
    public interface IOzonParserService
    {
        Task<Product> ParseProductAsync(string productUrl);
        Task<Product> ParseProductBySkuAsync(string sku);
    }
}
