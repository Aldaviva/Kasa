using Newtonsoft.Json.Linq;

namespace Kasa;

public partial class KasaOutlet {

    /// <inheritdoc />
    public IKasaOutletBase.ITimerCommandsSingleSocket Timer => this;

    /// <inheritdoc />
    async Task<Timer?> IKasaOutletBase.ITimerCommandsSingleSocket.Get() {
        return await GetTimer(null).ConfigureAwait(false);
    }

    /// <exception cref="NetworkException"></exception>
    /// <exception cref="ResponseParsingException"></exception>
    internal async Task<Timer?> GetTimer(SocketContext? context) {
        JArray jToken = (JArray) (await _client.Send<JObject>(CommandFamily.Timer, "get_rules", null, context).ConfigureAwait(false))["rule_list"]!;
        return jToken.ToObject<IEnumerable<Timer>>(KasaClient.JsonSerializer)!.FirstOrDefault() is { IsEnabled: true } timer ? timer : null;
    }

    /// <inheritdoc />
    Task<Timer> IKasaOutletBase.ITimerCommandsSingleSocket.Start(TimeSpan duration, bool setSocketOnWhenComplete) {
        return StartTimer(duration, setSocketOnWhenComplete, null);
    }

    /// <exception cref="NetworkException"></exception>
    /// <exception cref="ResponseParsingException"></exception>
    internal async Task<Timer> StartTimer(TimeSpan duration, bool setOutletOnWhenComplete, SocketContext? context) {
        await ClearTimers(context).ConfigureAwait(false);
        Timer timer = new(duration, setOutletOnWhenComplete);
        await _client.Send<JObject>(CommandFamily.Timer, "add_rule", timer, context).ConfigureAwait(false);
        return (await GetTimer(context).ConfigureAwait(false))!.Value;
    }

    /// <inheritdoc />
    Task IKasaOutletBase.ITimerCommandsSingleSocket.Clear() {
        return ClearTimers(null);
    }

    /// <exception cref="NetworkException"></exception>
    /// <exception cref="ResponseParsingException"></exception>
    internal Task ClearTimers(SocketContext? context) {
        return _client.Send<JObject>(CommandFamily.Timer, "delete_all_rules", context);
    }

}