using Newtonsoft.Json;

namespace Kasa.Marshal;

internal class DaysOfWeekConverter: JsonConverter<ISet<DayOfWeek>> {

    public override void WriteJson(JsonWriter writer, ISet<DayOfWeek>? value, JsonSerializer serializer) {
        writer.WriteStartArray();
        foreach (DayOfWeek dayOfWeek in Enum.GetValues(typeof(DayOfWeek)).Cast<DayOfWeek>()) {
            writer.WriteValue(Convert.ToInt32(value?.Contains(dayOfWeek) ?? false));
        }

        writer.WriteEndArray();
    }

    public override ISet<DayOfWeek> ReadJson(JsonReader reader, Type objectType, ISet<DayOfWeek>? existingValue, bool hasExistingValue, JsonSerializer serializer) {
        ISet<DayOfWeek> deserialized = new HashSet<DayOfWeek>();

        foreach (DayOfWeek dayOfWeek in Enum.GetValues(typeof(DayOfWeek)).Cast<DayOfWeek>()) {
            if (reader.ReadAsInt32() == 1) { // automatically skips the opening array square bracket [
                deserialized.Add(dayOfWeek);
            }
        }

        reader.Read(); // skip closing array square bracket ]
        return deserialized;
    }

}