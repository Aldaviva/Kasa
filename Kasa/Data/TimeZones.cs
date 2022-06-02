using System.Collections.Generic;
using System.Linq;

namespace Kasa;

/// <summary>
/// <para>To get a Windows <c>TimeZoneInfo</c> from a string ID like "Pacific Standard Time", call <c>TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time")</c></para>
/// <para>.NET ≥ 6 only: To get a Windows <c>TimeZoneInfo</c> from an IANA string ID like "America/Los_Angeles", call <c>TimeZoneInfo.TryConvertIanaIdToWindowsId("America/Los_Angeles", out string? windowsId) ? TimeZoneInfo.FindSystemTimeZoneById(windowsId) : null;</c></para>
/// <para>For other .NET runtimes, you'll probably want to use NodaTime for time zone conversions.</para>
/// </summary>
public readonly struct TimeZones {

    private static IReadOnlyDictionary<string, int>? _windowsZoneIdsToKasaIndices;

    /// <summary>
    /// <para>All the time zones that are supported by both .NET and Kasa.</para>
    /// <para>The key is the Windows TimeZoneInfo ID, and the value is the Kasa "index", which is what its TCP API uses.</para>
    /// </summary>
    public static IReadOnlyDictionary<string, int> WindowsZoneIdsToKasaIndices => _windowsZoneIdsToKasaIndices ??= new Dictionary<string, int> {
        { "Greenwich Standard Time", 38 },         // Atlantic/Reykjavik
        { "E. Africa Standard Time", 63 },         // Africa/Nairobi
        { "W. Central Africa Standard Time", 43 }, // Africa/Lagos
        { "South Africa Standard Time", 52 },      // Africa/Johannesburg
        { "Egypt Standard Time", 50 },             // Africa/Cairo
        { "Morocco Standard Time", 37 },           // Africa/Casablanca
        { "Romance Standard Time", 43 },           // Europe/Paris
        { "Sudan Standard Time", 63 },             // Africa/Khartoum
        { "Sao Tome Standard Time", 38 },          // Africa/Sao_Tome
        { "Libya Standard Time", 58 },             // Africa/Tripoli
        { "Namibia Standard Time", 46 },           // Africa/Windhoek
        { "Alaskan Standard Time", 3 },            // America/Anchorage
        { "SA Western Standard Time", 22 },        // America/La_Paz
        { "Tocantins Standard Time", 33 },         // America/Araguaina
        { "Argentina Standard Time", 29 },         // America/Buenos_Aires
        { "Paraguay Standard Time", 21 },          // America/Asuncion
        { "SA Pacific Standard Time", 17 },        // America/Bogota
        { "Aleutian Standard Time", 2 },           // America/Adak
        { "Bahia Standard Time", 33 },             // America/Bahia
        { "Central Standard Time (Mexico)", 14 },  // America/Mexico_City
        { "SA Eastern Standard Time", 33 },        // America/Cayenne
        { "Central America Standard Time", 12 },   // America/Guatemala
        { "Mountain Standard Time", 10 },          // America/Denver
        { "Central Brazilian Standard Time", 24 }, // America/Cuiaba
        { "Eastern Standard Time (Mexico)", 18 },  // America/Cancun
        { "Venezuela Standard Time", 20 },         // America/Caracas
        { "Central Standard Time", 13 },           // America/Chicago
        { "Mountain Standard Time (Mexico)", 8 },  // America/Chihuahua
        { "US Mountain Standard Time", 7 },        // America/Phoenix
        { "Yukon Standard Time", 6 },              // America/Whitehorse
        { "Eastern Standard Time", 18 },           // America/New_York
        { "Pacific Standard Time (Mexico)", 4 },   // America/Tijuana
        { "US Eastern Standard Time", 19 },        // America/Indianapolis
        { "Atlantic Standard Time", 23 },          // America/Halifax
        { "Greenland Standard Time", 31 },         // America/Godthab
        { "Turks And Caicos Standard Time", 18 },  // America/Grand_Turk
        { "Cuba Standard Time", 16 },              // America/Havana
        { "Pacific Standard Time", 6 },            // America/Los_Angeles
        { "Saint Pierre Standard Time", 31 },      // America/Miquelon
        { "Montevideo Standard Time", 32 },        // America/Montevideo
        { "UTC-02", 34 },                          // Etc/GMT+2
        { "Haiti Standard Time", 18 },             // America/Port-au-Prince
        { "Canada Central Standard Time", 15 },    // America/Regina
        { "Pacific SA Standard Time", 33 },        // America/Santiago
        { "E. South America Standard Time", 28 },  // America/Sao_Paulo
        { "Azores Standard Time", 35 },            // Atlantic/Azores
        { "Newfoundland Standard Time", 27 },      // America/St_Johns
        { "Central Pacific Standard Time", 102 },  // Pacific/Guadalcanal
        { "SE Asia Standard Time", 82 },           // Asia/Bangkok
        { "West Pacific Standard Time", 97 },      // Pacific/Port_Moresby
        { "Tasmania Standard Time", 96 },          // Australia/Hobart
        { "West Asia Standard Time", 74 },         // Asia/Tashkent
        { "New Zealand Standard Time", 104 },      // Pacific/Auckland
        { "Central Asia Standard Time", 79 },      // Asia/Almaty
        { "W. Europe Standard Time", 43 },         // Europe/Berlin
        { "Arab Standard Time", 60 },              // Asia/Riyadh
        { "Jordan Standard Time", 47 },            // Asia/Amman
        { "Russia Time Zone 11", 103 },            // Asia/Kamchatka
        { "Arabic Standard Time", 59 },            // Asia/Baghdad
        { "Azerbaijan Standard Time", 66 },        // Asia/Baku
        { "Middle East Standard Time", 49 },       // Asia/Beirut
        { "Singapore Standard Time", 86 },         // Asia/Singapore
        { "India Standard Time", 75 },             // Asia/Calcutta
        { "Transbaikal Standard Time", 92 },       // Asia/Chita
        { "Ulaanbaatar Standard Time", 89 },       // Asia/Ulaanbaatar
        { "China Standard Time", 84 },             // Asia/Shanghai
        { "Sri Lanka Standard Time", 76 },         // Asia/Colombo
        { "Bangladesh Standard Time", 79 },        // Asia/Dhaka
        { "Syria Standard Time", 51 },             // Asia/Damascus
        { "Tokyo Standard Time", 90 },             // Asia/Tokyo
        { "Arabian Standard Time", 65 },           // Asia/Dubai
        { "West Bank Standard Time", 56 },         // Asia/Hebron
        { "W. Mongolia Standard Time", 82 },       // Asia/Hovd
        { "North Asia East Standard Time", 85 },   // Asia/Irkutsk
        { "Turkey Standard Time", 55 },            // Europe/Istanbul
        { "Israel Standard Time", 56 },            // Asia/Jerusalem
        { "Afghanistan Standard Time", 71 },       // Asia/Kabul
        { "Pakistan Standard Time", 74 },          // Asia/Karachi
        { "Nepal Standard Time", 77 },             // Asia/Katmandu
        { "Yakutsk Standard Time", 92 },           // Asia/Yakutsk
        { "North Asia Standard Time", 83 },        // Asia/Krasnoyarsk
        { "Magadan Standard Time", 99 },           // Asia/Magadan
        { "GTB Standard Time", 55 },               // Europe/Bucharest
        { "N. Central Asia Standard Time", 80 },   // Asia/Novosibirsk
        { "Omsk Standard Time", 80 },              // Asia/Omsk
        { "North Korea Standard Time", 91 },       // Asia/Pyongyang
        { "Qyzylorda Standard Time", 78 },         // Asia/Qyzylorda
        { "Myanmar Standard Time", 79 },           // Asia/Rangoon
        { "Sakhalin Standard Time", 99 },          // Asia/Sakhalin
        { "Korea Standard Time", 91 },             // Asia/Seoul
        { "Russia Time Zone 10", 101 },            // Asia/Srednekolymsk
        { "Taipei Standard Time", 88 },            // Asia/Taipei
        { "Georgian Standard Time", 69 },          // Asia/Tbilisi
        { "Iran Standard Time", 64 },              // Asia/Tehran
        { "Vladivostok Standard Time", 100 },      // Asia/Vladivostok
        { "Ekaterinburg Standard Time", 73 },      // Asia/Yekaterinburg
        { "Caucasus Standard Time", 70 },          // Asia/Yerevan
        { "GMT Standard Time", 37 },               // Europe/London
        { "Cape Verde Standard Time", 36 },        // Atlantic/Cape_Verde
        { "AUS Eastern Standard Time", 96 },       // Australia/Sydney
        { "Cen. Australia Standard Time", 93 },    // Australia/Adelaide
        { "E. Australia Standard Time", 95 },      // Australia/Brisbane
        { "AUS Central Standard Time", 94 },       // Australia/Darwin
        { "Aus Central W. Standard Time", 87 },    // Australia/Eucla
        { "Lord Howe Standard Time", 96 },         // Australia/Lord_Howe
        { "W. Australia Standard Time", 87 },      // Australia/Perth
        { "Easter Island Standard Time", 18 },     // Pacific/Easter
        { "FLE Standard Time", 52 },               // Europe/Kiev
        { "UTC", 38 },                             // Etc/UTC
        { "UTC+12", 105 },                         // Etc/GMT-12
        { "UTC+13", 107 },                         // Etc/GMT-13
        { "Line Islands Standard Time", 109 },     // Pacific/Kiritimati
        { "Hawaiian Standard Time", 2 },           // Pacific/Honolulu
        { "UTC-11", 1 },                           // Etc/GMT+11
        { "Dateline Standard Time", 0 },           // Etc/GMT+12
        { "UTC-08", 5 },                           // Etc/GMT+8
        { "UTC-09", 3 },                           // Etc/GMT+9
        { "Central Europe Standard Time", 44 },    // Europe/Budapest
        { "E. Europe Standard Time", 52 },         // Europe/Chisinau
        { "Kaliningrad Standard Time", 57 },       // Europe/Kaliningrad
        { "Belarus Standard Time", 61 },           // Europe/Minsk
        { "Russian Standard Time", 62 },           // Europe/Moscow
        { "Russia Time Zone 3", 67 },              // Europe/Samara
        { "Central European Standard Time", 44 },  // Europe/Warsaw
        { "Volgograd Standard Time", 62 },         // Europe/Volgograd
        { "Mauritius Standard Time", 70 },         // Indian/Mauritius
        { "Chatham Islands Standard Time", 104 },  // Pacific/Chatham
        { "Samoa Standard Time", 108 },            // Pacific/Apia
        { "Bougainville Standard Time", 102 },     // Pacific/Bougainville
        { "Fiji Standard Time", 106 },             // Pacific/Fiji
        { "Marquesas Standard Time", 3 },          // Pacific/Marquesas
        { "Norfolk Standard Time", 102 },          // Pacific/Norfolk
        { "Tonga Standard Time", 107 },            // Pacific/Tongatapu
        { "South Sudan Standard Time", 63 },       // Africa/Juba
        { "Astrakhan Standard Time", 67 },         // Europe/Samara
        { "Saratov Standard Time", 67 },           // Europe/Samara
        { "Altai Standard Time", 83 },             // Asia/Krasnoyarsk
        { "Tomsk Standard Time", 83 },             // Asia/Krasnoyarsk
        { "Kamchatka Standard Time", 103 },        // Asia/Kamchatka
    };

    private static Dictionary<int, IEnumerable<string>>? _kasaIndicesToWindowsZoneIds;

    internal static IReadOnlyDictionary<int, IEnumerable<string>> KasaIndicesToWindowsZoneIds {
        get {
            if (_kasaIndicesToWindowsZoneIds == null) {
                _kasaIndicesToWindowsZoneIds = new Dictionary<int, IEnumerable<string>>();
                foreach (KeyValuePair<string, int> zoneIdsToDeviceIndex in WindowsZoneIdsToKasaIndices) {
                    int    kasaIndex     = zoneIdsToDeviceIndex.Value;
                    string windowsZoneId = zoneIdsToDeviceIndex.Key;
                    _kasaIndicesToWindowsZoneIds.TryGetValue(kasaIndex, out IEnumerable<string>? enumerable);
                    _kasaIndicesToWindowsZoneIds[kasaIndex] = (enumerable ?? new List<string>()).Concat(new[] { windowsZoneId });
                }
            }

            return _kasaIndicesToWindowsZoneIds;
        }
    }

}