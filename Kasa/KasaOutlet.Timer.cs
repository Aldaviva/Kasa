using Newtonsoft.Json.Linq;

namespace Kasa;

public partial class KasaOutlet {

    /// <inheritdoc />
    public IKasaOutletBase.ITimerCommandsSingleOutlet Timer => this;

    /// <inheritdoc />
    async Task<Timer?> IKasaOutletBase.ITimerCommandsSingleOutlet.Get() {
        return await GetTimer(null).ConfigureAwait(false);
    }

    internal async Task<Timer?> GetTimer(ChildContext? context) {
        JArray jToken = (JArray) (await Client.Send<JObject>(CommandFamily.Timer, "get_rules", null, context).ConfigureAwait(false))["rule_list"]!;
        return jToken.ToObject<IEnumerable<Timer>>(KasaClient.JsonSerializer)!.FirstOrDefault() is { IsEnabled: true } timer ? timer : null;
    }

    /// <inheritdoc />
    Task<Timer> IKasaOutletBase.ITimerCommandsSingleOutlet.Start(TimeSpan duration, bool setOutletOnWhenComplete) {
        return StartTimer(duration, setOutletOnWhenComplete, null);
    }

    internal async Task<Timer> StartTimer(TimeSpan duration, bool setOutletOnWhenComplete, ChildContext? context) {
        await ClearTimers(context).ConfigureAwait(false);
        Timer timer = new(duration, setOutletOnWhenComplete);
        await Client.Send<JObject>(CommandFamily.Timer, "add_rule", timer, context).ConfigureAwait(false);
        return (await GetTimer(context).ConfigureAwait(false))!.Value;
    }

    /// <inheritdoc />
    Task IKasaOutletBase.ITimerCommandsSingleOutlet.Clear() {
        return ClearTimers(null);
    }

    internal Task ClearTimers(ChildContext? context) {
        return Client.Send<JObject>(CommandFamily.Timer, "delete_all_rules", context);
    }

}