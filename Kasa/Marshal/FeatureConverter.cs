using Newtonsoft.Json;

namespace Kasa.Marshal;

internal class FeatureConverter: JsonConverter<ISet<Feature>> {

    public override void WriteJson(JsonWriter writer, ISet<Feature>? value, JsonSerializer serializer) { }

    /// <exception cref="JsonSerializationException"></exception>
    public override ISet<Feature> ReadJson(JsonReader reader, Type objectType, ISet<Feature>? existingValue, bool hasExistingValue, JsonSerializer serializer) {
        if (reader.TokenType == JsonToken.String) {
            return new HashSet<Feature>(((string) reader.Value!).Split(':').Select(Features.FromJsonString));
        } else {
            throw new JsonSerializationException($"Unexpected token or value when parsing Feature. Token: {reader.TokenType}, Value: {reader.Value}");
        }
    }

}