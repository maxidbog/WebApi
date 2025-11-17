using System.Text.Json.Serialization;

public class Product
{
    [JsonPropertyName("article")]
    public string Article { get; set; }

    [JsonPropertyName("categoryId")]
    public string? CategoryId { get; set; }

    [JsonPropertyName("categoryName")]
    public string CategoryName { get; set; }

    [JsonPropertyName("productUrl")]
    public string ProductUrl { get; set; }

    [JsonPropertyName("productName")]
    public string ProductName { get; set; }

    [JsonPropertyName("cardPrice")]
    public decimal CardPrice { get; set; }

    [JsonPropertyName("currentPrice")]
    public decimal CurrentPrice { get; set; }

    [JsonPropertyName("originalPrice")]
    public decimal OriginalPrice { get; set; }

    [JsonPropertyName("averageRating")]
    public double? AverageRating { get; set; }

    [JsonPropertyName("reviewsCount")]
    public int ReviewsCount { get; set; }

    [JsonPropertyName("stockQuantity")]
    public int StockQuantity { get; set; }

    [JsonPropertyName("deliveryTime")]
    public string? DeliveryTime { get; set; }

    [JsonPropertyName("sellerName")]
    public string? SellerName { get; set; }

    [JsonPropertyName("brand")]
    public string? Brand { get; set; }

    [JsonPropertyName("sellerRating")]
    public double? SellerRating { get; set; }

    [JsonPropertyName("imageUrl")]
    public string ImageUrl { get; set; }

    [JsonPropertyName("returnDeadline")]
    public string? ReturnDeadline { get; set; }

    [JsonPropertyName("returnConditions")]
    public string? ReturnConditions { get; set; }

    [JsonPropertyName("characteristics")]
    public Dictionary<string, Characteristic> Characteristics { get; set; } = new();

    [JsonPropertyName("isAvailable")]
    public bool IsAvailable { get; set; }
}
