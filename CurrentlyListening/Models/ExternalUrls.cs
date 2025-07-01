using Newtonsoft.Json;

namespace CurrentlyListening.Models;

public class ExternalUrls
{
    [JsonProperty("spotify", NullValueHandling = NullValueHandling.Ignore)]
    public Uri Spotify { get; set; }
}