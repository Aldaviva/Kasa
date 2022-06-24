using System;
using Newtonsoft.Json;

namespace Kasa.Marshal;

internal class TimeSpanConverter: JsonConverter {

    public override bool CanConvert(Type objectType) => objectType == typeof(TimeSpan);

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer) {
        // if (value is TimeSpan timeSpan) {
        //kasa throws "invalid argument" error (-3) on floating-point delays, they must be integral
        writer.WriteValue((long) ((TimeSpan) value!).TotalSeconds);
        // } else if (value is null) {
        // writer.WriteNull();
        // } else {
        // throw new JsonSerializationException(string.Format(CultureInfo.InvariantCulture, "Unexpected value when converting TimeSpan. Expected TimeSpan, got {0}.", value?.GetType()));
        // }
    }

    /// <exception cref="JsonSerializationException"></exception>
    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer) {
        return reader.TokenType switch {
            // JsonToken.Null when !(objectType.IsGenericType && objectType.GetGenericTypeDefinition() == typeof(Nullable<>)) =>
            //     throw new JsonSerializationException(string.Format(CultureInfo.InvariantCulture, "Cannot convert null value to {0}.", objectType)),
            // JsonToken.Null    => null,
            JsonToken.Integer => TimeSpan.FromSeconds((long) reader.Value!),
            JsonToken.Float   => TimeSpan.FromSeconds((double) reader.Value!),
            _                 => throw new JsonSerializationException($"Unexpected token parsing date. Expected String, got {reader.TokenType}.")
        };

    }

}