using System;
using Newtonsoft.Json;

namespace Kasa;

    /// <summary>
    /// Child element for outlet device with more than one plug
    /// </summary>
    public struct Child {
        [JsonProperty("id")] public string id { get; set; }  //  "800671BEB946D3C691ECBD2940991375202E165600",
        [JsonProperty("state")] public string state { get; set; }  //  1,
        [JsonProperty("alias")] public string alias { get; set; }  //  "Outside Plug 1",
        [JsonProperty("on_time")] public string on_time { get; set; }  //  1882,

        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static Feature FromJsonString(string jsonString) => jsonString.ToUpperInvariant() switch {
            "TIM" => Feature.Timer,
            "ENE" => Feature.EnergyMeter,
            _     => throw new ArgumentOutOfRangeException(nameof(jsonString), jsonString, "Unknown feature")
        };
    }