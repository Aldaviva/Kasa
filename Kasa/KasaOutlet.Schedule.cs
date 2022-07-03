using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Kasa;

public partial class KasaOutlet {

    /// <inheritdoc />
    public IKasaOutlet.IScheduleCommands Schedule => this;

    /// <inheritdoc />
    async Task<IEnumerable<Schedule>> IKasaOutlet.IScheduleCommands.GetAll() {
        JObject response = await _client.Send<JObject>(CommandFamily.Schedule, "get_rules").ConfigureAwait(false);
        return response["rule_list"]!.ToObject<IEnumerable<Schedule>>(KasaClient.JsonSerializer)!;
    }

    /// <inheritdoc />
    async Task<Schedule> IKasaOutlet.IScheduleCommands.Save(Schedule schedule) {
        string id;
        if (schedule.Id is null) {
            JObject created = await _client.Send<JObject>(CommandFamily.Schedule, "add_rule", schedule).ConfigureAwait(false);
            id = created["id"]!.ToObject<string>(KasaClient.JsonSerializer)!;
        } else {
            await _client.Send<JObject>(CommandFamily.Schedule, "edit_rule", schedule).ConfigureAwait(false);
            id = schedule.Id;
        }

        return (await ((IKasaOutlet.IScheduleCommands) this).GetAll().ConfigureAwait(false)).FirstOrDefault(schedule1 => schedule1.Id == id);
    }

    /// <inheritdoc />
    Task IKasaOutlet.IScheduleCommands.Delete(Schedule schedule) {
        if (schedule.Id is not null) {
            return ((IKasaOutlet.IScheduleCommands) this).Delete(schedule.Id);
        } else {
            throw new ArgumentException(
                $"The input schedule must have been fetched from the Kasa outlet (using {nameof(IKasaOutlet.IScheduleCommands.GetAll)}, but the given instance has a null {nameof(Kasa.Schedule.Id)}.");
        }
    }

    /// <inheritdoc />
    Task IKasaOutlet.IScheduleCommands.Delete(string id) {
        return _client.Send<JObject>(CommandFamily.Schedule, "delete_rule", new { id });
    }

    /// <inheritdoc />
    Task IKasaOutlet.IScheduleCommands.DeleteAll() {
        return _client.Send<JObject>(CommandFamily.Schedule, "delete_all_rules");
    }

}