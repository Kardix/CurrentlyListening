using Newtonsoft.Json;

namespace CurrentlyListening.Models;

public class Artist
{
    [JsonProperty("external_urls", NullValueHandling = NullValueHandling.Ignore)]
    public ExternalUrls ExternalUrls { get; set; }

    [JsonProperty("href", NullValueHandling = NullValueHandling.Ignore)]
    public Uri Href { get; set; }

    [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
    public string Id { get; set; }

    [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
    public string Name { get; set; }

    [JsonProperty("type", NullValueHandling = NullValueHandling.Ignore)]
    public string Type { get; set; }

    [JsonProperty("uri", NullValueHandling = NullValueHandling.Ignore)]
    public string Uri { get; set; }
}