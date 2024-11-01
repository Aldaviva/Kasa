using System.Net.NetworkInformation;
using Newtonsoft.Json;

namespace Kasa.Marshal;

internal class MacAddressConverter: JsonConverter<PhysicalAddress> {

    public override void WriteJson(JsonWriter writer, PhysicalAddress? value, JsonSerializer serializer) { }

    /// <exception cref="JsonSerializationException"></exception>
    public override PhysicalAddress? ReadJson(JsonReader reader, Type objectType, PhysicalAddress? existingValue, bool hasExistingValue, JsonSerializer serializer) {
        if (reader.TokenType == JsonToken.String) {
            string macString = (string) reader.Value!;
            try {
                return PhysicalAddress.Parse(macString.Replace(":", "-").ToUpperInvariant());
            } catch (FormatException e) {
                throw new JsonSerializationException($"Failed to parse PhysicalAddress {macString}", e);
                // throw JsonSerializationException.Create(reader, string.Format(CultureInfo.InvariantCulture, "Error parsing version string: {0}", reader.Value), e);
            }
        } else {
            throw new JsonSerializationException($"Unexpected token or value when parsing Feature. Token: {reader.TokenType}, Value: {reader.Value}");
        }
    }

}