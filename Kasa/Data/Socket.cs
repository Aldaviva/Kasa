using Newtonsoft.Json;

namespace Kasa;

internal readonly struct Socket {

    [JsonProperty("alias")] public string Name { get; internal init; }

    [JsonProperty("id")] public string Id { get; internal init; }

    [JsonProperty("state")] public bool IsOn { get; internal init; }

}