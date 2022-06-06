using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Kasa;

/// <summary>
/// <para>A TP-Link Kasa outlet or plug. This class is the main entry point of the Kasa library. The corresponding interface is <see cref="IKasaOutlet"/>.</para>
/// <para>You must call <see cref="Connect"/> on each instance before using it.</para>
/// <para>Remember to <c>Dispose</c> each instance when you're done using it in order to close the TCP connection with the device. Disposed instances may not be reused, even if you call <see cref="Connect"/> again.</para>
/// <para>To communicate with multiple Kasa devices, construct multiple <see cref="KasaOutlet"/> instances, one per device.</para>
/// <para>Example usage:</para>
/// <code>using IKasaOutlet outlet = new KasaOutlet("192.168.1.100");
/// await outlet.Connect();
/// bool isOutletOn = await outlet.System.IsOutletOn();
/// if(!isOutletOn){
///     await outlet.System.SetOutlet(true);
/// }</code>
/// </summary>
public class KasaOutlet: IKasaOutlet, IKasaOutlet.ISystemCommands, IKasaOutlet.ITimeCommands, IKasaOutlet.IEnergyMeterCommands {

    private readonly IKasaClient _client;

    /// <inheritdoc />
    public string Hostname => _client.Hostname;

    /// <inheritdoc />
    public ILoggerFactory? LoggerFactory {
        get => _client.LoggerFactory;
        set => _client.LoggerFactory = value;
    }

    /// <summary>
    /// <para>Construct a new instance of a <see cref="KasaClient"/> to talk to a Kasa device with the given hostname.</para>
    /// <para>After constructing an instance, remember to call <see cref="Connect"/> to establish a TCP connection to the device before you send commands.</para>
    /// <para>To communicate with multiple Kasa devices, construct multiple <see cref="KasaOutlet"/> instances, one per device.</para>
    /// <para>Remember to <see cref="Dispose()"/> each instance when you're done using it and want to disconnect from the TCP session. Disposed instances may not be reused, even if you call <see cref="Connect"/> again.</para>
    /// </summary>
    /// <param name="hostname">The fully-qualified domain name or IP address of the Kasa device to which this instance should connect.</param>
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

    /// <inheritdoc />
    public IKasaOutlet.IEnergyMeterCommands EnergyMeter => this;

    /// <summary>
    /// <para>Disconnects and disposes the TCP client.</para>
    /// <para>Subclasses should call this base method in their overriding <c>Dispose</c> implementations.</para>
    /// </summary>
    /// <param name="disposing"><c>true</c> to dispose the TCP client, <c>false</c> if you're running in a finalizer and it's already been disposed</param>
    protected virtual void Dispose(bool disposing) {
        if (disposing) {
            _client.Dispose();
        }
    }

    /// <summary>
    /// <para>Disconnect and dispose the TCP client.</para>
    /// <para>After calling this method, you can't use this instance again, even if you call <see cref="Connect"/> again. Instead, you should construct a new instance.</para>
    /// </summary>
    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    async Task<bool> IKasaOutlet.ISystemCommands.IsOutletOn() {
        SystemInfo systemInfo = await ((IKasaOutlet.ISystemCommands) this).GetInfo().ConfigureAwait(false);
        return systemInfo.IsOutletOn;
    }

    /// <inheritdoc />
    Task IKasaOutlet.ISystemCommands.SetOutletOn(bool turnOn) {
        return _client.Send<JObject>(CommandFamily.System, "set_relay_state", new { state = Convert.ToInt32(turnOn) });
    }

    /// <inheritdoc />
    Task<SystemInfo> IKasaOutlet.ISystemCommands.GetInfo() {
        return _client.Send<SystemInfo>(CommandFamily.System, "get_sysinfo");
    }

    /// <inheritdoc />
    async Task<bool> IKasaOutlet.ISystemCommands.IsIndicatorLightOn() {
        SystemInfo systemInfo = await ((IKasaOutlet.ISystemCommands) this).GetInfo().ConfigureAwait(false);
        return !systemInfo.IndicatorLightDisabled;
    }

    /// <inheritdoc />
    Task IKasaOutlet.ISystemCommands.SetIndicatorLightOn(bool turnOn) {
        return _client.Send<JObject>(CommandFamily.System, "set_led_off", new { off = Convert.ToInt32(!turnOn) });
    }

    /// <inheritdoc />
    Task IKasaOutlet.ISystemCommands.Reboot(TimeSpan afterDelay) {
        return _client.Send<JObject>(CommandFamily.System, "reboot", new { delay = (int) afterDelay.TotalSeconds });
    }

    /// <inheritdoc />
    Task IKasaOutlet.ISystemCommands.SetName(string name) {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 31) {
            throw new ArgumentOutOfRangeException(nameof(name), name, "name must be between 1 and 31 characters long (inclusive), and cannot be only whitespace");
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

    /// <inheritdoc />
    Task<PowerUsage> IKasaOutlet.IEnergyMeterCommands.GetInstantaneousPowerUsage() {
        return _client.Send<PowerUsage>(CommandFamily.EnergyMeter, "get_realtime");
    }

    /// <inheritdoc />
    async Task<IList<int>?> IKasaOutlet.IEnergyMeterCommands.GetDailyEnergyUsage(int year, int month) {
        JArray     response = (JArray) (await _client.Send<JObject>(CommandFamily.EnergyMeter, "get_daystat", new { year, month }).ConfigureAwait(false))["day_list"]!;
        List<int>? results  = null;

        if (response.Count > 0) {
            int daysInMonth = new DateTime(year, month, 1).AddMonths(1).AddDays(-1).Day;
            results = new List<int>(Enumerable.Repeat(0, daysInMonth));
            foreach (JToken dayEntry in response) {
                int day    = dayEntry["day"]!.Value<int>();
                int energy = dayEntry["energy_wh"]!.Value<int>();
                results[day - 1] = energy;
            }
        }

        return results;
    }

    /// <inheritdoc />
    async Task<IList<int>?> IKasaOutlet.IEnergyMeterCommands.GetMonthlyEnergyUsage(int year) {
        JArray     response = (JArray) (await _client.Send<JObject>(CommandFamily.EnergyMeter, "get_monthstat", new { year }).ConfigureAwait(false))["month_list"]!;
        List<int>? results  = null;

        if (response.Count > 0) {
            results = new List<int>(Enumerable.Repeat(0, 12));
            foreach (JToken monthEntry in response) {
                int month  = monthEntry["month"]!.Value<int>();
                int energy = monthEntry["energy_wh"]!.Value<int>();
                results[month - 1] = energy;
            }
        }

        return results;
    }

    /// <inheritdoc />
    Task IKasaOutlet.IEnergyMeterCommands.DeleteHistoricalUsage() {
        return _client.Send<JObject>(CommandFamily.EnergyMeter, "erase_emeter_stat");
    }

}