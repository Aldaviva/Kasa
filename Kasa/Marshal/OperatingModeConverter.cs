using Newtonsoft.Json;

namespace Kasa.Marshal;

internal class OperatingModeConverter: JsonConverter<OperatingMode> {

    public override void WriteJson(JsonWriter writer, OperatingMode value, JsonSerializer serializer) { }

    /// <exception cref="JsonSerializationException"></exception>
    public override OperatingMode ReadJson(JsonReader reader, Type objectType, OperatingMode existingValue, bool hasExistingValue, JsonSerializer serializer) {
        if (reader.TokenType == JsonToken.String) {
            return OperatingModes.FromJsonString((string) reader.Value!);
        } else {
            throw new JsonSerializationException($"Unexpected token or value when parsing OperatingMode. Token: {reader.TokenType}, Value: {reader.Value}");
        }
    }

}