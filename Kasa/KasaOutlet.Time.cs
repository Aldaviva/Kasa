using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Kasa;

public partial class KasaOutlet {

    /// <inheritdoc />
    public IKasaOutlet.ITimeCommands Time => this;

    /// <inheritdoc />
    async Task<DateTime> IKasaOutlet.ITimeCommands.GetTime() {
        JObject response = await _client.Send<JObject>(CommandFamily.Time, "get_time").ConfigureAwait(false);
        return new DateTime(
            response["year"]!.ToObject<int>(),
            response["month"]!.ToObject<int>(),
            response["mday"]!.ToObject<int>(),
            response["hour"]!.ToObject<int>(),
            response["min"]!.ToObject<int>(),
            response["sec"]!.ToObject<int>());
    }

    /// <inheritdoc />
    async Task<DateTimeOffset> IKasaOutlet.ITimeCommands.GetTimeWithZoneOffset() {
        IKasaOutlet.ITimeCommands @this = this;

        DateTime                  localDate             = await @this.GetTime().ConfigureAwait(false);
        IEnumerable<TimeZoneInfo> timeZonePossibilities = await @this.GetTimeZones().ConfigureAwait(false);

        return new DateTimeOffset(localDate, timeZonePossibilities.First().GetUtcOffset(localDate));
    }

    /// <inheritdoc />
    // ExceptionAdjustment: M:System.TimeZoneInfo.FindSystemTimeZoneById(System.String) -T:System.Security.SecurityException
    async Task<IEnumerable<TimeZoneInfo>> IKasaOutlet.ITimeCommands.GetTimeZones() {
        JObject response         = await _client.Send<JObject>(CommandFamily.Time, "get_timezone").ConfigureAwait(false);
        int     deviceTimezoneId = response["index"]!.ToObject<int>();

        IEnumerable<string> windowsZoneIds = TimeZones.KasaIndicesToWindowsZoneIds[deviceTimezoneId];
        return windowsZoneIds.SelectMany(windowsZoneId => {
            try {
                return new[] { TimeZoneInfo.FindSystemTimeZoneById(windowsZoneId) };
            } catch (Exception e) when (e is TimeZoneNotFoundException or InvalidTimeZoneException) {
                return Enumerable.Empty<TimeZoneInfo>();
            }
        });
    }

    /// <inheritdoc />
    Task IKasaOutlet.ITimeCommands.SetTimeZone(TimeZoneInfo timeZone) {
        try {
            int deviceTimezoneId = TimeZones.WindowsZoneIdsToKasaIndices[timeZone.Id];
            return _client.Send<JObject>(CommandFamily.Time, "set_timezone", new { index = deviceTimezoneId });
        } catch (KeyNotFoundException e) {
            throw new TimeZoneNotFoundException($"Kasa devices don't have a built-in time zone that matches {timeZone.Id}." +
                $"Consult Kasa.{nameof(TimeZones)}.{nameof(TimeZones.WindowsZoneIdsToKasaIndices)} for supported time zones.", e);
        }
    }

}