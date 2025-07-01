using Newtonsoft.Json;

namespace CurrentlyListening.Models;

public class ExternalIds
{
    [JsonProperty("isrc", NullValueHandling = NullValueHandling.Ignore)]
    public string Isrc { get; set; }
}