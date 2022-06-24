using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Kasa;

public partial class KasaOutlet {

    /// <inheritdoc />
    public IKasaOutlet.IEnergyMeterCommands EnergyMeter => this;

    /// <inheritdoc />
    Task<PowerUsage> IKasaOutlet.IEnergyMeterCommands.GetInstantaneousPowerUsage() {
        return _client.Send<PowerUsage>(CommandFamily.EnergyMeter, "get_realtime");
    }

    /// <inheritdoc />
    async Task<IList<int>?> IKasaOutlet.IEnergyMeterCommands.GetDailyEnergyUsage(int year, int month) {
        JArray     response = (JArray) (await _client.Send<JObject>(CommandFamily.EnergyMeter, "get_daystat", new { year, month }).ConfigureAwait(false))["day_list"]!;
        List<int>? results  = null;

        if (response.Count > 0) {
            int daysInMonth = new DateTime(year, month, 1).AddMonths(1).AddDays(-1).Day;
            results = new List<int>(Enumerable.Repeat(0, daysInMonth));
            foreach (JToken dayEntry in response) {
                int day    = dayEntry["day"]!.Value<int>();
                int energy = dayEntry["energy_wh"]!.Value<int>();
                results[day - 1] = energy;
            }
        }

        return results;
    }

    /// <inheritdoc />
    async Task<IList<int>?> IKasaOutlet.IEnergyMeterCommands.GetMonthlyEnergyUsage(int year) {
        JArray     response = (JArray) (await _client.Send<JObject>(CommandFamily.EnergyMeter, "get_monthstat", new { year }).ConfigureAwait(false))["month_list"]!;
        List<int>? results  = null;

        if (response.Count > 0) {
            results = new List<int>(Enumerable.Repeat(0, 12));
            foreach (JToken monthEntry in response) {
                int month  = monthEntry["month"]!.Value<int>();
                int energy = monthEntry["energy_wh"]!.Value<int>();
                results[month - 1] = energy;
            }
        }

        return results;
    }

    /// <inheritdoc />
    Task IKasaOutlet.IEnergyMeterCommands.DeleteHistoricalUsage() {
        return _client.Send<JObject>(CommandFamily.EnergyMeter, "erase_emeter_stat");
    }

}