using Newtonsoft.Json.Linq;

namespace Kasa;

public partial class KasaOutlet {

    /// <inheritdoc />
    public IKasaOutletBase.ITimeCommands Time => this;

    /// <inheritdoc />
    async Task<DateTime> IKasaOutletBase.ITimeCommands.GetTime() {
        JObject response = await _client.Send<JObject>(CommandFamily.Time, "get_time").ConfigureAwait(false);
        return new DateTime(
            response["year"]!.ToObject<int>(KasaClient.JsonSerializer),
            response["month"]!.ToObject<int>(KasaClient.JsonSerializer),
            response["mday"]!.ToObject<int>(KasaClient.JsonSerializer),
            response["hour"]!.ToObject<int>(KasaClient.JsonSerializer),
            response["min"]!.ToObject<int>(KasaClient.JsonSerializer),
            response["sec"]!.ToObject<int>(KasaClient.JsonSerializer));
    }

    /// <inheritdoc />
    async Task<DateTimeOffset> IKasaOutletBase.ITimeCommands.GetTimeWithZoneOffset() {
        DateTime                  localDate             = await ((IKasaOutletBase.ITimeCommands) this).GetTime().ConfigureAwait(false);
        IEnumerable<TimeZoneInfo> timeZonePossibilities = await ((IKasaOutletBase.ITimeCommands) this).GetTimeZones().ConfigureAwait(false);
        return new DateTimeOffset(localDate, timeZonePossibilities.First().GetUtcOffset(localDate));
    }

    /// <inheritdoc />
    // ExceptionAdjustment: M:System.TimeZoneInfo.FindSystemTimeZoneById(System.String) -T:System.Security.SecurityException
    async Task<IEnumerable<TimeZoneInfo>> IKasaOutletBase.ITimeCommands.GetTimeZones() {
        JObject response         = await _client.Send<JObject>(CommandFamily.Time, "get_timezone").ConfigureAwait(false);
        int     deviceTimezoneId = response["index"]!.ToObject<int>(KasaClient.JsonSerializer);

        IEnumerable<string> windowsZoneIds = TimeZones.KasaIndicesToWindowsZoneIds[deviceTimezoneId];
        return windowsZoneIds.SelectMany(windowsZoneId => {
            try {
                return [TimeZoneInfo.FindSystemTimeZoneById(windowsZoneId)];
            } catch (Exception e) when (e is TimeZoneNotFoundException or InvalidTimeZoneException) {
                return Enumerable.Empty<TimeZoneInfo>();
            }
        });
    }

    /// <inheritdoc />
    Task IKasaOutletBase.ITimeCommands.SetTimeZone(TimeZoneInfo timeZone) {
        try {
            int deviceTimezoneId = TimeZones.WindowsZoneIdsToKasaIndices[timeZone.Id];
            return _client.Send<JObject>(CommandFamily.Time, "set_timezone", new { index = deviceTimezoneId });
        } catch (KeyNotFoundException e) {
            throw new TimeZoneNotFoundException($"Kasa devices don't have a built-in time zone that matches {timeZone.Id}." +
                $"Consult Kasa.{nameof(TimeZones)}.{nameof(TimeZones.WindowsZoneIdsToKasaIndices)} for supported time zones.", e);
        }
    }

}