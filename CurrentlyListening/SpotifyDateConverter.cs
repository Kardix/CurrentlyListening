using System.Globalization;
using Newtonsoft.Json;

namespace CurrentlyListening;

public class SpotifyDateConverter : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset ReadJson(JsonReader reader, Type objectType, DateTimeOffset existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var dateString = (string)reader.Value;

        if (dateString.Length == 4) // "yyyy"
            return new DateTimeOffset(int.Parse(dateString), 1, 1, 0, 0, 0, TimeSpan.Zero);

        if (dateString.Length == 7) // "yyyy-MM"
            return DateTimeOffset.ParseExact(dateString, "yyyy-MM", CultureInfo.InvariantCulture);

        return DateTimeOffset.Parse(dateString, CultureInfo.InvariantCulture);
    }

    public override void WriteJson(JsonWriter writer, DateTimeOffset value, JsonSerializer serializer)
    {
        writer.WriteValue(value.ToString("yyyy-MM-dd")); // Adjust if needed
    }
}