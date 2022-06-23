using Newtonsoft.Json;

namespace Kasa;
public struct Outlet {
    /// <summary>
    /// Unique identifier of the outlet on a strip
    /// </summary>
    [JsonProperty("id")] public string ID { get; internal set; }
    /// <summary>
    /// Name of the outlet
    /// </summary>
    [JsonProperty("alias")] public string Alias { get; internal set; }
    [JsonProperty("state")] public bool IsOn { get; internal set; }
    [JsonProperty("on_time")] public long OnTime { get; internal set; }
}