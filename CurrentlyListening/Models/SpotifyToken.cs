namespace CurrentlyListening.Models;

public class SpotifyToken
{
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
    public DateTime ExpiryTime { get; set; }
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
}