using System;

namespace Kasa;

public enum Feature {

    Timer,
    EnergyMeter

}

internal static class Features {

    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static Feature FromJsonString(string jsonString) => jsonString.ToUpperInvariant() switch {
        "TIM" => Feature.Timer,
        "ENE" => Feature.EnergyMeter,
        _     => throw new ArgumentOutOfRangeException(nameof(jsonString), jsonString, "Unknown feature")
    };

}