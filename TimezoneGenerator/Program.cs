using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

JsonSerializer jsonSerializer = JsonSerializer.CreateDefault();

IDictionary<string, int> results = new Dictionary<string, int>();

JObject olsenDatabase  = JObject.Load(new JsonTextReader(new StreamReader(File.OpenRead("timezone_id.json"))));
JObject deviceDatabase = JObject.Load(new JsonTextReader(new StreamReader(File.OpenRead("timezone_fwindex.json"))));

IDictionary<string, int> deviceMap = new Dictionary<string, int>();

foreach (JProperty deviceEntry in deviceDatabase.Properties().Where(property => property.Name != "version")) {
    string deviceKey   = deviceEntry.Name;
    int    deviceIndex = deviceEntry.Value.ToObject<DeviceTimezone>()!.index;
    deviceMap.Add(deviceKey, deviceIndex);
}

foreach (JProperty olsenEntry in olsenDatabase.Properties().Where(property => property.Name != "version")) {
    string olsenZoneId = olsenEntry.Name;
    string deviceKey   = olsenEntry.Value.ToObject<OlsenTimezone>()!.index;
    int    deviceId    = deviceMap[deviceKey];
    TimeZoneInfo.TryConvertIanaIdToWindowsId(olsenZoneId, out string? windowsZoneId);
    // Console.WriteLine($"{olsenZoneId} → {windowsZoneId}");
    if (windowsZoneId != null) {
        results[windowsZoneId] = deviceId;
    } else {
        Console.WriteLine($"Windows does not have a timezone for {olsenZoneId}");
    }
}

Console.WriteLine("new Dictionary<string, int> {");
foreach (KeyValuePair<string, int> result in results) {
    Console.WriteLine($@"    {{ ""{result.Key}"", {result.Value} }},");
}

Console.WriteLine('}');

// ReadOnlyCollection<TimeZoneInfo> zones = TimeZoneInfo.GetSystemTimeZones();
// Console.WriteLine("The local system has the following {0} time zones", zones.Count);
// foreach (TimeZoneInfo zone in zones) {
//     TimeZoneInfo.TryConvertWindowsIdToIanaId(zone.Id, out string? ianaId);
//     if (ianaId != null) {
//         Console.WriteLine(ianaId);
//     }
//     // Console.WriteLine($"{zone.Id} - {zone.StandardName}");
// }

public class OlsenTimezone {

    public string name { get; set; }
    public string offset { get; set; }
    public bool dst { get; set; }
    public string index { get; set; }

}

public class DeviceTimezone {

    public bool dst { get; set; }
    public string? description { get; set; }
    public string? description_dst { get; set; }
    public string offset { get; set; }
    public string offset_dst { get; set; }
    public int index { get; set; }

}