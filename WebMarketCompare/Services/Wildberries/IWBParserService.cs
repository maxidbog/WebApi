using WebMarketCompare.Models;

namespace WebMarketCompare.Services.Wildberries
{
    public interface IWBParserService
    {
        Task<Product> ParseProductAsync(string productUrl);
        Task<Product> ParseProductBySkuAsync(string sku);
    }
}
