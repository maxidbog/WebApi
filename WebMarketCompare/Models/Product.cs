using WebMarketCompare.Models;

namespace WebMarketCompare.Models
{
    public class Product
    {
        public string Sku { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public decimal OriginalPrice { get; set; }
        public double? Rating { get; set; }
        public int ReviewsCount { get; set; }
        public string Url { get; set; }
        public List<Characteristic> Characteristics { get; set; } = new();
        public List<string> ImageUrls { get; set; } = new();
        public string Brand { get; set; }
    }

}
