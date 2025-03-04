using Newtonsoft.Json;
using System.Net.NetworkInformation;

namespace Kasa;

/// <summary>
/// Data about the device, including hardware, software, configuration, and current state.
/// </summary>
public readonly struct SystemInfo {

    /// <summary>
    /// How the Kasa device has been configured to run. This corresponds to what you've selected in the Kasa mobile app (Schedule, Timer).
    /// </summary>
    [JsonProperty("active_mode")] public OperatingMode OperatingMode { get; internal init; }

    /// <summary>
    /// <para>The name or alias of the device that you chose during setup.</para>
    /// <para>You can change this value using <c>IKasaOutlet.System.SetName(string)</c>.</para>
    /// </summary>
    [JsonProperty("alias")] internal string Name { get; init; }

    /// <summary>
    /// The long marketing name of the family of devices. Not unique with respect to models, as both the Kasa EP10 and KP125 have <c>Smart Wi-Fi Plug Mini</c> as their model description. Can be <c>null</c>, as is the case with the EP40.
    /// </summary>
    [JsonProperty("dev_name")] public string? ModelFamily { get; internal init; }

    /// <summary>
    /// <para>40-character uppercase hexadecimal string that is unique for each device instance.</para>
    /// <para>Example: <c>8006C153CFEBDE93CD3572549B5A47611F49F0D2</c></para>
    /// </summary>
    [JsonProperty("deviceId")] public string DeviceId { get; internal init; }

    /// <summary>
    /// <para>32-character uppercase hexadecimal string that is unique for each model, the same uniqueness as <see cref="OemId"/></para>
    /// <para>Examples:
    /// <list type="bullet">
    /// <item><term>AE6865C67F6A54B756C0B5812472C825</term><description>(EP10 v1.8)</description></item>
    /// <item><term>51F7FD94AB9EFF012B93C4B41A44DB32</term><description>(KP125 v1.8)</description></item>
    /// <item><term>B3B7B05B758C3EDA8F9C69FECDBA2111</term><description>(EP40 v1.26)</description></item>
    /// </list></para>
    /// </summary>
    [JsonProperty("hwId")] public string HardwareId { get; internal init; }

    /// <summary>
    /// <para>The version of the device's hardware, such as <c>1.0</c>.</para>
    /// <para>Unfortunately, this is not trustworthy, because version 1.8 of the EP10 and KP125 report 1.0 in this field, so you should probably ignore this value.</para>
    /// </summary>
    [JsonProperty("hw_ver")] public string HardwareVersion { get; internal init; }

    [JsonProperty("led_off")] internal bool IndicatorLightDisabled { get; init; }

    /// <summary>
    /// <para>The MAC address of the device. This is also printed on a sticker on the device itself if you need to identify it.</para>
    /// </summary>
    [JsonProperty("mac")] public PhysicalAddress MacAddress { get; internal init; }

    /// <summary>
    /// The short name of the model, such as <c>EP10(US)</c> for the Kasa EP10, <c>KP125(US)</c> for the Kasa KP125, or <c>EP40(US)</c> for the Kasa EP40.
    /// </summary>
    [JsonProperty("model")] public string ModelName { get; internal init; }

    /// <summary>
    /// <para>32-character uppercase hexadecimal string that is unique for each model, the same uniqueness as <see cref="HardwareId"/>.</para>
    /// <para>Examples:
    /// <list type="bullet">
    /// <item><term>41372DE62C896B2C0E93C20D70B62DDB</term><description>(EP10 v1.8)</description></item>
    /// <item><term>39559FAFF2ECC2AA8A7A082C4E33B815</term><description>(KP125 v1.8)</description></item>
    /// <item><term>2F9215F1DCBF7DC17F80E2B0CACD47FC</term><description>(EP40 v1.26)</description></item>
    /// </list></para>
    /// </summary>
    [JsonProperty("oemId")] public string OemId { get; internal init; }

    [JsonProperty("relay_state")] internal bool IsSocketOn { get; init; }

    /// <summary>
    /// <para>Received Signal Strength Indicator. The signal strength of the device's Wi-Fi connection, expressed as a non-positive number such as <c>-61</c>. The strongest signal has a value of <c>0</c>, while a very weak signal has a value around <c>-77</c>.</para>
    /// <para>The Kasa mobile app reports this value with <c>dBm</c> as the unit, although RSSI is intrinsically unitless, arbitrary, and unstandardized, so take dBm with a grain of salt.</para>
    /// </summary>
    [JsonProperty("rssi")] public int SignalStrength { get; internal init; }

    /// <summary>
    /// The version of the firmware that is running on the device, such as <c>1.0.2 Build 200915 Rel.085940</c> or <c>1.0.8 Build 220130 Rel.174717</c>.
    /// </summary>
    [JsonProperty("sw_ver")] public string SoftwareVersion { get; internal init; }

    /// <summary>
    /// Whether or not the device is currently performing a software update.
    /// </summary>
    [JsonProperty("updating")] public bool Updating { get; internal init; }

    /// <summary>
    /// <para>The capabilities of the device.</para>
    /// <para>These will vary by model, for example, the EP10 has a timer but no energy meter, while the KP125 has both.</para>
    /// </summary>
    [JsonProperty("feature")] public ISet<Feature> Features { get; internal init; }

    [JsonProperty("children")] internal IEnumerable<Socket>? Sockets { get; init; }
    [JsonProperty("child_num")] internal int? SocketCount { get; init; }

    /// <inheritdoc />
    public override string ToString() =>
        $"{nameof(MacAddress)}: {MacAddress}, {nameof(ModelFamily)}: {ModelFamily}, {nameof(ModelName)}: {ModelName}, {nameof(DeviceId)}: {DeviceId}, {nameof(HardwareId)}: {HardwareId}, {nameof(HardwareVersion)}: {HardwareVersion}, {nameof(OemId)}: {OemId}, {nameof(SoftwareVersion)}: {SoftwareVersion}, {nameof(SignalStrength)}: {SignalStrength}, {nameof(OperatingMode)}: {OperatingMode}, {nameof(Updating)}: {Updating}";

}