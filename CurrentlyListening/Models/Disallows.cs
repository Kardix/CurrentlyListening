using Newtonsoft.Json;

namespace CurrentlyListening.Models;

public class Disallows
{
    [JsonProperty("resuming", NullValueHandling = NullValueHandling.Ignore)]
    public bool? Resuming { get; set; }

    [JsonProperty("skipping_prev", NullValueHandling = NullValueHandling.Ignore)]
    public bool? SkippingPrev { get; set; }
}