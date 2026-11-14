using System.Globalization;
using Newtonsoft.Json;

namespace CurrentlyListening;

// Spotify sends release_date as "yyyy", "yyyy-MM" or "yyyy-MM-dd" (see release_date_precision),
// and "0000" when the date is unknown. Anything that isn't a real date becomes null instead of
// failing the whole currently-playing response.
public class SpotifyDateConverter : JsonConverter<DateTimeOffset?>
{
    private static readonly string[] Formats = { "yyyy-MM-dd", "yyyy-MM", "yyyy" };

    public override DateTimeOffset? ReadJson(JsonReader reader, Type objectType, DateTimeOffset? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        switch (reader.Value)
        {
            // Json.NET may already have parsed an ISO-looking string into a date.
            case DateTimeOffset dateTimeOffset:
                return dateTimeOffset;
            case DateTime dateTime:
                return new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc));
            case string dateString when DateTimeOffset.TryParseExact(dateString, Formats,
                CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var date):
                return date;
            default:
                return null;
        }
    }

    public override void WriteJson(JsonWriter writer, DateTimeOffset? value, JsonSerializer serializer)
    {
        if (value.HasValue)
            writer.WriteValue(value.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        else
            writer.WriteNull();
    }
}
