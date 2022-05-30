using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Kasa;

public class KasaOutlet: IKasaOutlet, IKasaOutlet.ISystemCommands, IKasaOutlet.ITimeCommands {

    private readonly IKasaClient _client;

    /// <inheritdoc />
    public string Hostname => _client.Hostname;

    public KasaOutlet(string hostname): this(new KasaClient(hostname)) { }

    internal KasaOutlet(IKasaClient client) {
        _client = client;
    }

    /// <inheritdoc />
    public Task Connect() {
        return _client.Connect();
    }

    /// <inheritdoc />
    public IKasaOutlet.ISystemCommands System => this;

    /// <inheritdoc />
    public IKasaOutlet.ITimeCommands Time => this;

    protected virtual void Dispose(bool disposing) {
        if (disposing) {
            _client.Dispose();
        }
    }

    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    async Task<bool> IKasaOutlet.ISystemCommands.IsOutletOn() {
        SystemInfo systemInfo = await ((IKasaOutlet.ISystemCommands) this).GetSystemInfo().ConfigureAwait(false);
        return systemInfo.IsOutletOn;
    }

    /// <inheritdoc />
    Task IKasaOutlet.ISystemCommands.SetOutlet(bool turnOn) {
        return _client.Send<JObject>(CommandFamily.System, "set_relay_state", new { state = Convert.ToInt32(turnOn) });
    }

    /// <inheritdoc />
    Task<SystemInfo> IKasaOutlet.ISystemCommands.GetSystemInfo() {
        return _client.Send<SystemInfo>(CommandFamily.System, "get_sysinfo");
    }

    /// <inheritdoc />
    async Task<bool> IKasaOutlet.ISystemCommands.IsIndicatorLightOn() {
        SystemInfo systemInfo = await ((IKasaOutlet.ISystemCommands) this).GetSystemInfo().ConfigureAwait(false);
        return !systemInfo.IndicatorLightDisabled;
    }

    /// <inheritdoc />
    Task IKasaOutlet.ISystemCommands.SetIndicatorLight(bool turnOn) {
        return _client.Send<JObject>(CommandFamily.System, "set_led_off", new { off = Convert.ToInt32(!turnOn) });
    }

    /// <inheritdoc />
    Task IKasaOutlet.ISystemCommands.Reboot(TimeSpan afterDelay) {
        return _client.Send<JObject>(CommandFamily.System, "reboot", new { delay = (int) afterDelay.TotalSeconds });
    }

    /// <inheritdoc />
    Task IKasaOutlet.ISystemCommands.SetName(string name) {
        if (string.IsNullOrEmpty(name) || name.Length > 31) {
            throw new ArgumentOutOfRangeException(nameof(name), name, "name must be between 1 and 31 characters long (inclusive)");
        }

        return _client.Send<JObject>(CommandFamily.System, "set_dev_alias", new { alias = name });
    }

    // /// <exception cref="SocketException"></exception>
    // /// <exception cref="IOException"></exception>
    // /// <exception cref="JsonReaderException"></exception>
    // Task IKasaSmartOutlet.ISystemCommands.SetLocation(double latitude, double longitude) {
    //     return _client.Send<JObject>(CommandFamily.System, "set_dev_location", new { latitude, longitude });
    // }

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

    async Task<TimeZoneInfo> IKasaOutlet.ITimeCommands.GetTimeZone() {
        JObject response         = await _client.Send<JObject>(CommandFamily.Time, "get_timezone").ConfigureAwait(false);
        int     deviceTimezoneId = response["index"]!.ToObject<int>();

        return TimeZoneInfo.FindSystemTimeZoneById(TimeZones.DeviceIndicesToWindowsZoneIds[deviceTimezoneId]);
    }

    Task IKasaOutlet.ITimeCommands.SetTimezone(TimeZoneInfo timeZone) {
        int deviceTimezoneId = TimeZones.WindowsZoneIdsToDeviceIndices[timeZone.Id];
        return _client.Send<JObject>(CommandFamily.Time, "set_timezone", new { index = deviceTimezoneId });
    }

}