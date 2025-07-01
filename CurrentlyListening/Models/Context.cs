using Newtonsoft.Json;

namespace CurrentlyListening.Models;

public class Context
{
    [JsonProperty("external_urls", NullValueHandling = NullValueHandling.Ignore)]
    public ExternalUrls ExternalUrls { get; set; }

    [JsonProperty("href", NullValueHandling = NullValueHandling.Ignore)]
    public Uri Href { get; set; }

    [JsonProperty("type", NullValueHandling = NullValueHandling.Ignore)]
    public string Type { get; set; }

    [JsonProperty("uri", NullValueHandling = NullValueHandling.Ignore)]
    public string Uri { get; set; }
}