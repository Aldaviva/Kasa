using Newtonsoft.Json.Linq;
using System.Diagnostics.CodeAnalysis;

namespace Kasa;

public partial class KasaOutlet {

    /// <inheritdoc />
    public IKasaOutletBase.ISystemCommands.ISingleSocket System => this;

    /// <inheritdoc />
    async Task<bool> IKasaOutletBase.ISystemCommands.ISingleSocket.IsSocketOn() {
        SystemInfo systemInfo = await ((IKasaOutletBase.ISystemCommands) this).GetInfo().ConfigureAwait(false);
        return systemInfo.IsSocketOn;
    }

    /// <inheritdoc />
    Task IKasaOutletBase.ISystemCommands.ISingleSocket.SetSocketOn(bool turnOn) => SetSocketOn(turnOn, null);

    /// <inheritdoc cref="IKasaOutletBase.ISystemCommands.ISingleSocket.SetSocketOn" />
    internal Task SetSocketOn(bool turnOn, SocketContext? context) {
        return _client.Send<JObject>(CommandFamily.System, "set_relay_state", new { state = Convert.ToInt32(turnOn) }, context);
    }

    /// <inheritdoc />
    Task<SystemInfo> IKasaOutletBase.ISystemCommands.GetInfo() => GetInfo();

    /// <inheritdoc cref="IKasaOutletBase.ISystemCommands.GetInfo" />
    protected virtual Task<SystemInfo> GetInfo() {
        return _client.Send<SystemInfo>(CommandFamily.System, "get_sysinfo");
    }

    /// <inheritdoc />
    async Task<bool> IKasaOutletBase.ISystemCommands.IsIndicatorLightOn() {
        SystemInfo systemInfo = await ((IKasaOutletBase.ISystemCommands) this).GetInfo().ConfigureAwait(false);
        return !systemInfo.IndicatorLightDisabled;
    }

    /// <inheritdoc />
    Task IKasaOutletBase.ISystemCommands.SetIndicatorLightOn(bool turnOn) {
        return _client.Send<JObject>(CommandFamily.System, "set_led_off", new { off = Convert.ToInt32(!turnOn) });
    }

    /// <inheritdoc />
    Task IKasaOutletBase.ISystemCommands.Reboot(TimeSpan afterDelay) {
        return _client.Send<JObject>(CommandFamily.System, "reboot", new { delay = (int) afterDelay.TotalSeconds });
    }

    /// <inheritdoc />
    async Task<string> IKasaOutletBase.ISystemCommands.GetName() {
        SystemInfo systemInfo = await ((IKasaOutletBase.ISystemCommands) this).GetInfo().ConfigureAwait(false);
        return systemInfo.Name;
    }

    /// <inheritdoc />
    Task IKasaOutletBase.ISystemCommands.SetName(string name) => SetName(name, null);

    /// <inheritdoc cref="IKasaOutletBase.ISystemCommands.SetName" />
    internal Task SetName(string name, SocketContext? context) {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 31) {
            throw new ArgumentOutOfRangeException(nameof(name), name, "name must be between 1 and 31 characters long (inclusive), and cannot be only whitespace");
        }

        return _client.Send<JObject>(CommandFamily.System, "set_dev_alias", new { alias = name }, context);
    }

    /// <inheritdoc />
    async Task<int> IKasaOutletBase.ISystemCommands.CountSockets() {
        return (await GetInfo().ConfigureAwait(false)).SocketCount ?? 1;
    }

    /// <inheritdoc />
    [Obsolete($"This method was poorly named, and has been renamed to {nameof(System.IsSocketOn)}", false)]
    [ExcludeFromCodeCoverage]
    Task<bool> IKasaOutletBase.ISystemCommands.ISingleSocket.IsOutletOn() => System.IsSocketOn();

    /// <inheritdoc />
    [Obsolete($"This method was poorly named, and has been renamed to {nameof(System.SetSocketOn)}", false)]
    [ExcludeFromCodeCoverage]
    Task IKasaOutletBase.ISystemCommands.ISingleSocket.SetOutletOn(bool turnOn) => System.SetSocketOn(turnOn);

}