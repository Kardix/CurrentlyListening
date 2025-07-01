using Newtonsoft.Json;

namespace CurrentlyListening.Models;

public class Item
{
    [JsonProperty("album", NullValueHandling = NullValueHandling.Ignore)]
    public Album Album { get; set; }

    [JsonProperty("artists", NullValueHandling = NullValueHandling.Ignore)]
    public List<Artist> Artists { get; set; }

    [JsonProperty("available_markets", NullValueHandling = NullValueHandling.Ignore)]
    public List<string> AvailableMarkets { get; set; }

    [JsonProperty("disc_number", NullValueHandling = NullValueHandling.Ignore)]
    public long? DiscNumber { get; set; }

    [JsonProperty("duration_ms", NullValueHandling = NullValueHandling.Ignore)]
    public long? DurationMs { get; set; }

    [JsonProperty("explicit", NullValueHandling = NullValueHandling.Ignore)]
    public bool? Explicit { get; set; }

    [JsonProperty("external_ids", NullValueHandling = NullValueHandling.Ignore)]
    public ExternalIds ExternalIds { get; set; }

    [JsonProperty("external_urls", NullValueHandling = NullValueHandling.Ignore)]
    public ExternalUrls ExternalUrls { get; set; }

    [JsonProperty("href", NullValueHandling = NullValueHandling.Ignore)]
    public Uri Href { get; set; }

    [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
    public string Id { get; set; }

    [JsonProperty("is_local", NullValueHandling = NullValueHandling.Ignore)]
    public bool? IsLocal { get; set; }

    [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
    public string Name { get; set; }

    [JsonProperty("popularity", NullValueHandling = NullValueHandling.Ignore)]
    public long? Popularity { get; set; }

    [JsonProperty("preview_url")]
    public object PreviewUrl { get; set; }

    [JsonProperty("track_number", NullValueHandling = NullValueHandling.Ignore)]
    public long? TrackNumber { get; set; }

    [JsonProperty("type", NullValueHandling = NullValueHandling.Ignore)]
    public string Type { get; set; }

    [JsonProperty("uri", NullValueHandling = NullValueHandling.Ignore)]
    public string Uri { get; set; }
}