using Newtonsoft.Json.Linq;

namespace Kasa;

public partial class KasaOutlet {

    /// <inheritdoc />
    public IKasaOutlet.ITimerCommands Timer => this;

    /// <inheritdoc />
    async Task<Timer?> IKasaOutlet.ITimerCommands.Get() {
        JArray jToken = (JArray) (await _client.Send<JObject>(CommandFamily.Timer, "get_rules").ConfigureAwait(false))["rule_list"]!;
        return jToken.ToObject<IEnumerable<Timer>>(KasaClient.JsonSerializer)!.FirstOrDefault() is { IsEnabled: true } timer ? timer : null;
    }

    /// <inheritdoc />
    async Task<Timer> IKasaOutlet.ITimerCommands.Start(TimeSpan duration, bool setOutletOnWhenComplete) {
        IKasaOutlet.ITimerCommands @this = this;

        await @this.Clear().ConfigureAwait(false);
        Timer timer = new(duration, setOutletOnWhenComplete);
        await _client.Send<JObject>(CommandFamily.Timer, "add_rule", timer).ConfigureAwait(false);
        return (await @this.Get().ConfigureAwait(false))!.Value;
    }

    /// <inheritdoc />
    Task IKasaOutlet.ITimerCommands.Clear() {
        return _client.Send<JObject>(CommandFamily.Timer, "delete_all_rules");
    }

}