using Newtonsoft.Json;

namespace Kasa.Marshal;

internal class TimeSpanConverter: JsonConverter<TimeSpan> {

    /// <exception cref="JsonSerializationException"></exception>
    public override void WriteJson(JsonWriter writer, TimeSpan value, JsonSerializer serializer) {
        writer.WriteValue((long) value.TotalSeconds);
    }

    /// <exception cref="JsonSerializationException"></exception>
    public override TimeSpan ReadJson(JsonReader reader, Type objectType, TimeSpan existingValue, bool hasExistingValue, JsonSerializer serializer) {
        double jsonValue = reader.TokenType switch {
            JsonToken.Integer => (long) reader.Value!,
            JsonToken.Float   => (double) reader.Value!,
            _                 => throw new JsonSerializationException($"Unexpected token parsing date. Expected String, got {reader.TokenType}.")
        };

        return TimeSpan.FromSeconds(jsonValue);
    }

}