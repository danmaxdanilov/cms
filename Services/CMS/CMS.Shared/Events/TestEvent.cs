using System.Text.Json.Serialization;

namespace CMS.Shared.Events;

public class TestEvent
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("favorite_color")]
    public string FavoriteColor { get; set; }

    [JsonPropertyName("favorite_number")]
    public long FavoriteNumber { get; set; }
}