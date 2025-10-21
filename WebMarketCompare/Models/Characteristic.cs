using System.Text.Json.Serialization;

public class Characteristic
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("value")]
    public string Value { get; set; }

    [JsonPropertyName("isBest")]
    public bool? IsBest { get; set; } = null;
}

