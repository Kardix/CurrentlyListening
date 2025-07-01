using Newtonsoft.Json;

namespace CurrentlyListening.Models;

public class SpotifyResponse
{
    [JsonProperty("timestamp", NullValueHandling = NullValueHandling.Ignore)]
    public long? Timestamp { get; set; }

    [JsonProperty("context", NullValueHandling = NullValueHandling.Ignore)]
    public Context Context { get; set; }

    [JsonProperty("progress_ms", NullValueHandling = NullValueHandling.Ignore)]
    public long? ProgressMs { get; set; }

    [JsonProperty("item", NullValueHandling = NullValueHandling.Ignore)]
    public Item Item { get; set; }

    [JsonProperty("currently_playing_type", NullValueHandling = NullValueHandling.Ignore)]
    public string CurrentlyPlayingType { get; set; }

    [JsonProperty("actions", NullValueHandling = NullValueHandling.Ignore)]
    public Actions Actions { get; set; }

    [JsonProperty("is_playing", NullValueHandling = NullValueHandling.Ignore)]
    public bool? IsPlaying { get; set; }
}