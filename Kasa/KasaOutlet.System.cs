using Newtonsoft.Json.Linq;

namespace Kasa;

public partial class KasaOutlet {

    /// <inheritdoc />
    public IKasaOutletBase.ISystemCommands.SingleOutlet System => this;

    /// <inheritdoc />
    async Task<bool> IKasaOutletBase.ISystemCommands.SingleOutlet.IsOutletOn() {
        SystemInfo systemInfo = await ((IKasaOutletBase.ISystemCommands) this).GetInfo().ConfigureAwait(false);
        return systemInfo.IsOutletOn;
    }

    /// <inheritdoc />
    Task IKasaOutletBase.ISystemCommands.SingleOutlet.SetOutletOn(bool turnOn) => SetOutletOn(turnOn, null);

    internal Task SetOutletOn(bool turnOn, ChildContext? context) {
        return Client.Send<JObject>(CommandFamily.System, "set_relay_state", new { state = Convert.ToInt32(turnOn) }, context);
    }

    /// <inheritdoc />
    Task<SystemInfo> IKasaOutletBase.ISystemCommands.GetInfo() => GetInfo();

    protected virtual Task<SystemInfo> GetInfo() {
        return Client.Send<SystemInfo>(CommandFamily.System, "get_sysinfo");
    }

    /// <inheritdoc />
    async Task<bool> IKasaOutletBase.ISystemCommands.IsIndicatorLightOn() {
        SystemInfo systemInfo = await ((IKasaOutletBase.ISystemCommands) this).GetInfo().ConfigureAwait(false);
        return !systemInfo.IndicatorLightDisabled;
    }

    /// <inheritdoc />
    Task IKasaOutletBase.ISystemCommands.SetIndicatorLightOn(bool turnOn) {
        return Client.Send<JObject>(CommandFamily.System, "set_led_off", new { off = Convert.ToInt32(!turnOn) });
    }

    /// <inheritdoc />
    Task IKasaOutletBase.ISystemCommands.Reboot(TimeSpan afterDelay) {
        return Client.Send<JObject>(CommandFamily.System, "reboot", new { delay = (int) afterDelay.TotalSeconds });
    }

    /// <inheritdoc />
    async Task<string> IKasaOutletBase.ISystemCommands.GetName() {
        SystemInfo systemInfo = await ((IKasaOutletBase.ISystemCommands) this).GetInfo().ConfigureAwait(false);
        return systemInfo.Name;
    }

    /// <inheritdoc />
    Task IKasaOutletBase.ISystemCommands.SetName(string name) => SetName(name, null);

    /// <exception cref="ArgumentOutOfRangeException">if the new name is empty or longer than 31 characters</exception>
    internal Task SetName(string name, ChildContext? context) {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 31) {
            throw new ArgumentOutOfRangeException(nameof(name), name, "name must be between 1 and 31 characters long (inclusive), and cannot be only whitespace");
        }

        return Client.Send<JObject>(CommandFamily.System, "set_dev_alias", new { alias = name }, context);
    }

    Task<int> IKasaOutletBase.ISystemCommands.CountOutlets() {
        return Task.FromResult(1);
    }

}