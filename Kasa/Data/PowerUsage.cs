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

    /// <summary>
    /// Create a fake response object from <see cref="IKasaOutlet.IEnergyMeterCommands.GetInstantaneousPowerUsage"/>, useful for mocking.
    /// </summary>
    /// <param name="current">How much current is being used, in milliamperes (mA).</param>
    /// <param name="voltage">How much voltage is being used, in millivolts (mV).</param>
    /// <param name="power">How much power is being used, in milliwatts (mW).</param>
    /// <param name="cumulativeEnergySinceBoot">Running total of how much cumulative energy has been used since the Kasa device rebooted, in watt-hours (W⋅h).</param>
    public PowerUsage(int current, int voltage, int power, int cumulativeEnergySinceBoot) {
        Current                   = current;
        Voltage                   = voltage;
        Power                     = power;
        CumulativeEnergySinceBoot = cumulativeEnergySinceBoot;
    }

}