using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TimezoneGenerator.Data;

/*
 * These two JSON files come from the Kasa Smart Android app. To update them:
 *  1. Download APK from https://www.apkmirror.com/apk/tp-link-corporation-limited/kasa-for-mobile/
 *  2. Extract the APK
 *      - Using JADX (https://github.com/skylot/jadx/releases/latest): open the APK, and JSON files are in the Resources directory
 *      - Using Apktool (https://github.com/iBotPeaches/Apktool/releases/latest): `java -jar apktool.jar decode com.tplink.kasa_android_*.apk -o decompiled`, and JSON files are in the decompiled/assets directory
 *  3. Copy the two JSON files (timezone_*.json) into the KasaAppAssets directory inside this project, or wherever this project's EXE is saved
 *  4. Compile and run this program
 *  5. Copy the console output to Kasa/Data/TimeZones.cs
 */
JObject olsenDatabase = JObject.Load(new JsonTextReader(new StreamReader(File.OpenRead(@"KasaAppAssets\timezone_id.json"))));
JObject kasaDatabase  = JObject.Load(new JsonTextReader(new StreamReader(File.OpenRead(@"KasaAppAssets\timezone_fwindex.json"))));

IDictionary<string, int> results            = new Dictionary<string, int>();
IDictionary<string, int> kasaMap            = new Dictionary<string, int>();
ISet<string>             unusedWindowsZones = new HashSet<string>(TimeZoneInfo.GetSystemTimeZones().Select(zone => zone.Id));

foreach (JProperty kasaEntry in kasaDatabase.Properties().Where(property => property.Name != "version")) {
    string kasaKey   = kasaEntry.Name;
    int    kasaIndex = kasaEntry.Value.ToObject<KasaTimezone>()!.index;
    kasaMap.Add(kasaKey, kasaIndex);
}

foreach (JProperty olsenEntry in olsenDatabase.Properties().Where(property => property.Name != "version")) {
    string olsenZoneId = FixKasaIanaId(olsenEntry.Name);
    string kasaKey     = olsenEntry.Value.ToObject<OlsenTimezone>()!.index;
    int    kasaId      = kasaMap[kasaKey];

    if (TimeZoneInfo.TryConvertIanaIdToWindowsId(olsenZoneId, out string? windowsZoneId)) {
        results[windowsZoneId] = kasaId;
        unusedWindowsZones.Remove(windowsZoneId);
    }
}

Console.WriteLine("new Dictionary<string, int> {");
foreach (KeyValuePair<string, int> result in results) {
    Console.WriteLine($@"    {{ ""{result.Key}"", {result.Value} }},{(TimeZoneInfo.TryConvertWindowsIdToIanaId(result.Key, out string? olsenId) ? " // " + olsenId : string.Empty)}");
}

foreach (string unusedWindowsZone in unusedWindowsZones) {
    if (FixWindowsId(unusedWindowsZone) is var (kasaId, olsenId)) {
        Console.WriteLine($@"    {{ ""{unusedWindowsZone}"", {kasaId} }}, // {olsenId}");
    }
}

Console.WriteLine('}');

static string FixKasaIanaId(string kasaIanaId) => kasaIanaId switch {
    "Asia/Kashgar"      => "Asia/Dhaka",         // backward, outdated, and some people who live here actually use Asia/Shanghai instead
    "Asia/Urumqi"       => "Asia/Dhaka",         // backward, outdated, and some people who live here actually use Asia/Shanghai instead
    "Atlantic/Reykjavi" => "Atlantic/Reykjavik", // misspelled in Kasa app, did someone type that whole 632-line JSON file manually instead of just generating it automatically?
    "CET"               => "Europe/Paris",       // Windows doesn't have this abbreviated IANA IDs, but it does have a city that follows the same rules
    "MET"               => "Europe/Paris",       // Windows doesn't have this abbreviated IANA IDs, but it does have a city that follows the same rules
    "EET"               => "Europe/Sofia",       // Windows doesn't have this abbreviated IANA IDs, but it does have a city that follows the same rules
    "WET"               => "Europe/Lisbon",      // Windows doesn't have this abbreviated IANA IDs, but it does have a city that follows the same rules
    _                   => kasaIanaId
};

static (int kasaId, string olsenId)? FixWindowsId(string windowsId) => windowsId switch {
    "Magallanes Standard Time"   => null, // America/Punta_Arenas
    "Mid-Atlantic Standard Time" => null, // This is a fake zone that was made up by Microsoft, and nobody has any mappings for it, not even Olsen
    "South Sudan Standard Time"  => (63, "Africa/Juba"),
    "Astrakhan Standard Time"    => (67, "Europe/Samara"),
    "Saratov Standard Time"      => (67, "Europe/Samara"),
    "Altai Standard Time"        => (83, "Asia/Krasnoyarsk"),
    "Tomsk Standard Time"        => (83, "Asia/Krasnoyarsk"),
    "Kamchatka Standard Time"    => (103, "Asia/Kamchatka"),
    _                            => null
};