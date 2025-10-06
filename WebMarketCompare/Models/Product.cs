using Swashbuckle.AspNetCore.Annotations;
using WebMarketCompare.Models;

namespace WebMarketCompare.Models
{
    public class Product
    {
        public string Article { get; set; }
        public string CategoryId { get; set; }
        public string CategoryName { get; set; }
        public string ProductUrl { get; set; }
        public string ProductName { get; set; }
        public decimal CardPrice { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal OriginalPrice { get; set; }
        public double? AverageRating { get; set; }
        public int ReviewsCount { get; set; }
        public int StockQuantity { get; set; }
        public string DeliveryTime { get; set; }
        public string SellerName { get; set; }
        public string Brand { get; set; }
        public double? SellerRating { get; set; }
        public string ImageUrl { get; set; }
        public string ReturnDeadline { get; set; }
        public string ReturnConditions { get; set; }
        public List<CharacteristicType> Characteristics { get; set; } = new();

        public bool IsAvailable { get; set; }
    }

}
