using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Kasa;

public partial class KasaOutlet {

    /// <inheritdoc />
    public IKasaOutlet.ISystemCommands System => this;

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
    Task IKasaOutlet.ISystemCommands.SetAllChildOutletOn(bool turnOn)
    {
        var systemInfo = System.GetInfo().Result;

        if (systemInfo.Children == null)
        {
            // No children are found, return.
            return Task.FromResult(false);
        }
			 
        // In order to support multiple outlets, a 'context' node must be added:
        // {"system":{"set_relay_state":{"state":0}},"context":{"child_ids":["etc00", "etc01]}}
        // https://community.home-assistant.io/t/tp-link-kasa-kp400-2-outlet-support/113135/9
       
        string[] children = systemInfo.Children.Select(child => child.id).ToArray();
        var commandParameters = new { state = Convert.ToInt32(turnOn) };

        //var serializedObject = Newtonsoft.Json.JsonConvert.SerializeObject(commandParameters); // Debug
        // Include array of children IDs to Send<JObject>().
        return _client.Send<JObject>(CommandFamily.System, "set_relay_state", commandParameters, children);
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
    async Task<string> IKasaOutlet.ISystemCommands.GetName() {
        SystemInfo systemInfo = await ((IKasaOutlet.ISystemCommands) this).GetInfo().ConfigureAwait(false);
        return systemInfo.Name;
    }

    /// <inheritdoc />
    Task IKasaOutlet.ISystemCommands.SetName(string name) {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 31) {
            throw new ArgumentOutOfRangeException(nameof(name), name, "name must be between 1 and 31 characters long (inclusive), and cannot be only whitespace");
        }

        return _client.Send<JObject>(CommandFamily.System, "set_dev_alias", new { alias = name });
    }

}