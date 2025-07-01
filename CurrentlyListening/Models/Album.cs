using Newtonsoft.Json;

namespace CurrentlyListening.Models;

public class Album
{
    [JsonProperty("album_type", NullValueHandling = NullValueHandling.Ignore)]
    public string AlbumType { get; set; }

    [JsonProperty("artists", NullValueHandling = NullValueHandling.Ignore)]
    public List<Artist> Artists { get; set; }

    [JsonProperty("available_markets", NullValueHandling = NullValueHandling.Ignore)]
    public List<string> AvailableMarkets { get; set; }

    [JsonProperty("external_urls", NullValueHandling = NullValueHandling.Ignore)]
    public ExternalUrls ExternalUrls { get; set; }

    [JsonProperty("href", NullValueHandling = NullValueHandling.Ignore)]
    public Uri Href { get; set; }

    [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
    public string Id { get; set; }

    [JsonProperty("images", NullValueHandling = NullValueHandling.Ignore)]
    public List<Image> Images { get; set; }

    [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
    public string Name { get; set; }

    [JsonProperty("release_date", NullValueHandling = NullValueHandling.Ignore)]
    [JsonConverter(typeof(SpotifyDateConverter))]
    public DateTimeOffset? ReleaseDate { get; set; }

    [JsonProperty("release_date_precision", NullValueHandling = NullValueHandling.Ignore)]
    public string ReleaseDatePrecision { get; set; }

    [JsonProperty("total_tracks", NullValueHandling = NullValueHandling.Ignore)]
    public long? TotalTracks { get; set; }

    [JsonProperty("type", NullValueHandling = NullValueHandling.Ignore)]
    public string Type { get; set; }

    [JsonProperty("uri", NullValueHandling = NullValueHandling.Ignore)]
    public string Uri { get; set; }
}