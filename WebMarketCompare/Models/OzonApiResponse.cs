using Newtonsoft.Json;

namespace WebMarketCompare.Models
{
    public class OzonApiResponse
    {
        public WidgetStates WidgetStates { get; set; }
    }

    public class WidgetStates
    {
        [Newtonsoft.Json.JsonExtensionData]
        public Dictionary<string, object> States { get; set; } = new();
    }

    // Models для парсинга конкретных виджетов
    public class ProductHeadingWidget
    {
        [JsonProperty("title")]
        public string Title { get; set; }
    }

    public class PriceWidget
    {
        [JsonProperty("price")]
        public string Price { get; set; }

        [JsonProperty("originalPrice")]
        public string OriginalPrice { get; set; }

        [JsonProperty("isAvailable")]
        public bool IsAvailable { get; set; }
    }

    public class RatingWidget
    {
        [JsonProperty("totalScore")]
        public double? TotalScore { get; set; }

        [JsonProperty("reviewsCount")]
        public int ReviewsCount { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }
    }

    public class GalleryWidget
    {
        [JsonProperty("images")]
        public List<ImageInfo> Images { get; set; } = new();
    }

    public class ImageInfo
    {
        [JsonProperty("src")]
        public string Source { get; set; }
    }

    public class CharacteristicsWidget
    {
        [JsonProperty("characteristics")]
        public List<CharacteristicItem> Characteristics { get; set; } = new();
    }

    public class CharacteristicItem
    {
        [JsonProperty("title")]
        public TitleInfo Title { get; set; }

        [JsonProperty("values")]
        public List<ValueInfo> Values { get; set; } = new();
    }

    public class TitleInfo
    {
        [JsonProperty("textRs")]
        public List<TextItem> TextRs { get; set; } = new();
    }

    public class ValueInfo
    {
        [JsonProperty("text")]
        public string Text { get; set; }
    }

    public class TextItem
    {
        [JsonProperty("content")]
        public string Content { get; set; }
    }

    public class BrandWidget
    {
        [JsonProperty("content")]
        public ContentInfo Content { get; set; }
    }

    public class ContentInfo
    {
        [JsonProperty("title")]
        public TitleContent Title { get; set; }
    }

    public class TitleContent
    {
        [JsonProperty("text")]
        public List<TextContent> Text { get; set; } = new();
    }

    public class TextContent
    {
        [JsonProperty("content")]
        public string Content { get; set; }
    }
}
