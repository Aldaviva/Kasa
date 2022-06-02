using Newtonsoft.Json;

namespace Kasa;

/// <summary>
/// Point-in-time measurement of the instantaneous electrical usage of the outlet, including amps, volts, and watts, as well as total watt-hours since boot.
/// </summary>
public struct PowerUsage {

    /// <summary>
    /// How much current is being used, in milliamperes (mA).
    /// </summary>
    [JsonProperty("current_ma")] public int Current { get; internal set; }

    /// <summary>
    /// How much voltage is being used, in millivolts (mV).
    /// </summary>
    [JsonProperty("voltage_mv")] public int Voltage { get; internal set; }

    /// <summary>
    /// How much power is being used, in milliwatts (mW).
    /// </summary>
    [JsonProperty("power_mw")] public int Power { get; internal set; }

    /// <summary>
    /// Running total of how much cumulative energy has been used since the Kasa device rebooted, in watt-hours (W⋅h).
    /// </summary>
    [JsonProperty("total_wh")] public int CumulativeEnergySinceBoot { get; internal set; }

}