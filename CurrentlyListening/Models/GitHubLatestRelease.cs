using Newtonsoft.Json;

namespace CurrentlyListening.Models;


public class GitHubLatestRelease
{
    [JsonProperty("tag_name")]
    public string TagName { get; set; }

    [JsonProperty("html_url")]
    public string HtmlUrl { get; set; }
}