using System.Collections.Generic;
using System.Net.NetworkInformation;
using Newtonsoft.Json;

namespace Kasa;

public record struct SystemInfo {

    [JsonProperty("active_mode")] public OperatingMode OperatingMode { get; internal set; }
    [JsonProperty("alias")] public string Name { get; internal set; }
    [JsonProperty("dev_name")] public string ModelName { get; internal set; }
    [JsonProperty("deviceId")] public string DeviceId { get; internal set; }
    [JsonProperty("hwId")] public string HardwareId { get; internal set; }
    [JsonProperty("hw_ver")] public string HardwareVersion { get; internal set; }
    [JsonProperty("led_off")] internal bool IndicatorLightDisabled { get; set; }
    [JsonProperty("mac")] public PhysicalAddress MacAddress { get; internal set; }
    [JsonProperty("model")] public string ModelId { get; internal set; }
    [JsonProperty("oemId")] public string OemId { get; internal set; }
    [JsonProperty("relay_state")] internal bool IsOutletOn { get; set; }
    [JsonProperty("rssi")] public int Rssi { get; internal set; }
    [JsonProperty("sw_ver")] public string SoftwareVersion { get; internal set; }
    [JsonProperty("updating")] public bool Updating { get; internal set; }
    [JsonProperty("feature")] public ISet<Feature> Features { get; internal set; }

}