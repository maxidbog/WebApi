using System.Text.Json.Serialization;

public class CharacteristicType
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("characteristics")]
    public List<Characteristic> Characteristics { get; set; }
}
