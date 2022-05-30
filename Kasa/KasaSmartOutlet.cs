using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Kasa;

public class KasaSmartOutlet: IKasaSmartOutlet, IKasaSmartOutlet.ISystemCommands, IKasaSmartOutlet.ITimeCommands {

    private readonly IKasaClient _client;

    public string Hostname => _client.Hostname;

    public KasaSmartOutlet(string hostname): this(new KasaClient(hostname)) { }

    internal KasaSmartOutlet(IKasaClient client) {
        _client = client;
    }

    /// <exception cref="SocketException"></exception>
    public Task Connect() {
        return _client.Connect();
    }

    public IKasaSmartOutlet.ISystemCommands System => this;
    public IKasaSmartOutlet.ITimeCommands Time => this;

    protected virtual void Dispose(bool disposing) {
        if (disposing) {
            _client.Dispose();
        }
    }

    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="SocketException"></exception>
    /// <exception cref="IOException"></exception>
    /// <exception cref="JsonReaderException"></exception>
    async Task<bool> IKasaSmartOutlet.ISystemCommands.IsOutletOn() {
        SystemInfo systemInfo = await ((IKasaSmartOutlet.ISystemCommands) this).GetSystemInfo().ConfigureAwait(false);
        return systemInfo.IsOutletOn;
    }

    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="SocketException"></exception>
    /// <exception cref="IOException"></exception>
    /// <exception cref="JsonReaderException"></exception>
    Task IKasaSmartOutlet.ISystemCommands.SetOutlet(bool turnOn) {
        return _client.Send<JObject>(CommandFamily.System, "set_relay_state", new { state = Convert.ToInt32(turnOn) });
    }

    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="SocketException"></exception>
    /// <exception cref="IOException"></exception>
    /// <exception cref="JsonReaderException"></exception>
    Task<SystemInfo> IKasaSmartOutlet.ISystemCommands.GetSystemInfo() {
        return _client.Send<SystemInfo>(CommandFamily.System, "get_sysinfo");
    }

    async Task<bool> IKasaSmartOutlet.ISystemCommands.IsIndicatorLightOn() {
        SystemInfo systemInfo = await ((IKasaSmartOutlet.ISystemCommands) this).GetSystemInfo().ConfigureAwait(false);
        return !systemInfo.IndicatorLightDisabled;
    }

    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="SocketException"></exception>
    /// <exception cref="IOException"></exception>
    /// <exception cref="JsonReaderException"></exception>
    Task IKasaSmartOutlet.ISystemCommands.SetIndicatorLight(bool turnOn) {
        return _client.Send<JObject>(CommandFamily.System, "set_led_off", new { off = Convert.ToInt32(!turnOn) });
    }

    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="SocketException"></exception>
    /// <exception cref="IOException"></exception>
    /// <exception cref="JsonReaderException"></exception>
    Task IKasaSmartOutlet.ISystemCommands.Reboot(TimeSpan afterDelay) {
        return _client.Send<JObject>(CommandFamily.System, "reboot", new { delay = (int) afterDelay.TotalSeconds });
    }

    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="SocketException"></exception>
    /// <exception cref="IOException"></exception>
    /// <exception cref="JsonReaderException"></exception>
    Task IKasaSmartOutlet.ISystemCommands.SetName(string name) {
        return _client.Send<JObject>(CommandFamily.System, "set_dev_alias", new { alias = name });
    }

    // /// <exception cref="SocketException"></exception>
    // /// <exception cref="IOException"></exception>
    // /// <exception cref="JsonReaderException"></exception>
    // Task IKasaSmartOutlet.ISystemCommands.SetLocation(double latitude, double longitude) {
    //     return _client.Send<JObject>(CommandFamily.System, "set_dev_location", new { latitude, longitude });
    // }

    /// <exception cref="SocketException"></exception>
    /// <exception cref="IOException"></exception>
    /// <exception cref="JsonReaderException"></exception>
    async Task<DateTime> IKasaSmartOutlet.ITimeCommands.GetTime() {
        JObject response = await _client.Send<JObject>(CommandFamily.Time, "get_time").ConfigureAwait(false);
        return new DateTime(
            response["year"]!.ToObject<int>(),
            response["month"]!.ToObject<int>(),
            response["mday"]!.ToObject<int>(),
            response["hour"]!.ToObject<int>(),
            response["min"]!.ToObject<int>(),
            response["sec"]!.ToObject<int>());
    }

}