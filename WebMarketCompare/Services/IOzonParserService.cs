using WebMarketCompare.Models;

namespace WebMarketCompare.Services
{
    public interface IOzonParserService
    {
        Task<Product> ParseProductAsync(string productUrl);
        Task<Product> ParseProductBySkuAsync(string sku);
    }
}
