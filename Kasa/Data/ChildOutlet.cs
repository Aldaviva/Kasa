using Newtonsoft.Json;

namespace Kasa;

internal readonly struct ChildOutlet {

    [JsonProperty("alias")] public string Name { get; internal init; }

    [JsonProperty("id")] public string Id { get; internal init; }

    [JsonProperty("state")] public bool IsOutletOn { get; internal init; }

}