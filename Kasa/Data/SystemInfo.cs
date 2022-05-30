using System.Collections.Generic;
using System.Net.NetworkInformation;
using Newtonsoft.Json;

namespace Kasa;

/// <summary>
/// Data about the device, including hardware, software, configuration, and current state.
/// </summary>
public record struct SystemInfo {

    /// <summary>
    /// How the Kasa device has been configured to run. This corresponds to what you've selected in the Kasa mobile app (Schedule, Timer).
    /// </summary>
    [JsonProperty("active_mode")] public OperatingMode OperatingMode { get; internal set; }

    /// <summary>
    /// <para>The name or alias of the device that you chose during setup.</para>
    /// <para>You can change this value using <c>IKasaOutlet.System.SetName(string)</c>.</para>
    /// </summary>
    [JsonProperty("alias")] public string Name { get; internal set; }

    /// <summary>
    /// The long marketing name of the device, such as <c>Smart Wi-Fi Plug Mini</c> for the Kasa EP10.
    /// </summary>
    [JsonProperty("dev_name")] public string ModelDescription { get; internal set; }

    [JsonProperty("deviceId")] public string DeviceId { get; internal set; }

    [JsonProperty("hwId")] public string HardwareId { get; internal set; }

    /// <summary>
    /// <para>The version of the device's hardware, such as <c>1.0</c>.</para>
    /// <para>Unfortunately, this is not trustworthy, because version 1.8 of the EP10 reports 1.0 in this field, so you should probably ignore this value.</para>
    /// </summary>
    [JsonProperty("hw_ver")] public string HardwareVersion { get; internal set; }

    [JsonProperty("led_off")] internal bool IndicatorLightDisabled { get; set; }

    /// <summary>
    /// <para>The MAC address of the device. This is also printed on a sticker on the device itself if you need to identify it.</para>
    /// </summary>
    [JsonProperty("mac")] public PhysicalAddress MacAddress { get; internal set; }

    /// <summary>
    /// The short name of the model, such as <c>EP10(US)</c> for the Kasa EP10.
    /// </summary>
    [JsonProperty("model")] public string ModelName { get; internal set; }

    [JsonProperty("oemId")] public string OemId { get; internal set; }
    [JsonProperty("relay_state")] internal bool IsOutletOn { get; set; }

    /// <summary>
    /// <para>Received Signal Strength Indicator. The signal strength of the device's wi-fi connection, expressed as a negative number such as <c>-61</c>. Higher strength signals have values closer to positive infinity.</para>
    /// <para>The Kasa mobile app reports this value with dBm as the unit, although RSSI is instrinsically unitless, arbitrary, and unstandardized, so take dBm with a grain of salt.</para>
    /// </summary>
    // TODO figure out range of possible values
    [JsonProperty("rssi")] public int Rssi { get; internal set; }

    /// <summary>
    /// The version of the firmware that is running on the device, such as <c>1.0.2 Build 200915 Rel.085940</c>.
    /// </summary>
    [JsonProperty("sw_ver")] public string SoftwareVersion { get; internal set; }

    /// <summary>
    /// Whether or not the device is currently performing a software update.
    /// </summary>
    [JsonProperty("updating")] public bool Updating { get; internal set; }

    /// <summary>
    /// <para>The capabilities of the device.</para>
    /// <para>These will vary by model, for example, the EP10 has a timer but no energy meter, while the KP115 has both.</para>
    /// </summary>
    [JsonProperty("feature")] public ISet<Feature> Features { get; internal set; }

}