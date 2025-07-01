using Newtonsoft.Json;

namespace CurrentlyListening.Models;

public class Actions
{
    [JsonProperty("disallows", NullValueHandling = NullValueHandling.Ignore)]
    public Disallows Disallows { get; set; }
}