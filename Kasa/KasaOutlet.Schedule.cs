using Newtonsoft.Json.Linq;

namespace Kasa;

public partial class KasaOutlet {

    /// <inheritdoc />
    public IKasaOutletBase.IScheduleCommandsSingleOutlet Schedule => this;

    /// <inheritdoc />
    async Task<IEnumerable<Schedule>> IKasaOutletBase.IScheduleCommandsSingleOutlet.GetAll() {
        return await GetAllSchedules(null).ConfigureAwait(false);
    }

    internal async Task<IEnumerable<Schedule>> GetAllSchedules(ChildContext? context) {
        JObject response = await Client.Send<JObject>(CommandFamily.Schedule, "get_rules", null, context).ConfigureAwait(false);
        return response["rule_list"]!.ToObject<IEnumerable<Schedule>>(KasaClient.JsonSerializer)!;
    }

    /// <inheritdoc />
    async Task<Schedule> IKasaOutletBase.IScheduleCommandsSingleOutlet.Save(Schedule schedule) {
        return await SaveSchedule(schedule, null).ConfigureAwait(false);
    }

    internal async Task<Schedule> SaveSchedule(Schedule schedule, ChildContext? context) {
        string id;
        if (schedule.Id is null) {
            JObject created = await Client.Send<JObject>(CommandFamily.Schedule, "add_rule", schedule, context).ConfigureAwait(false);
            id = created["id"]!.ToObject<string>(KasaClient.JsonSerializer)!;
        } else {
            await Client.Send<JObject>(CommandFamily.Schedule, "edit_rule", schedule, context).ConfigureAwait(false);
            id = schedule.Id;
        }

        return (await ((IKasaOutletBase.IScheduleCommandsSingleOutlet) this).GetAll().ConfigureAwait(false)).FirstOrDefault(schedule1 => schedule1.Id == id);
    }

    /// <inheritdoc />
    Task IKasaOutletBase.IScheduleCommandsSingleOutlet.Delete(Schedule schedule) {
        return DeleteSchedule(schedule, null);
    }

    /// <exception cref="ArgumentException">If the <paramref name="schedule"/> parameter has a <c>null</c> value for the <see cref="Schedule.Id"/> property, possibly because it was newly constructed instead of being fetched from <see cref="IKasaOutletBase.IScheduleCommandsSingleOutlet.GetAll"/> or <see cref="Save"/>.</exception>
    internal Task DeleteSchedule(Schedule schedule, ChildContext? context) {
        if (schedule.Id is not null) {
            return DeleteSchedule(schedule.Id, context);
        } else {
            throw new ArgumentException(
                $"The input schedule must have been fetched from the Kasa outlet (using {nameof(IKasaOutletBase.IScheduleCommandsSingleOutlet.GetAll)}, but the given instance has a null {nameof(Kasa.Schedule.Id)}.");
        }
    }

    /// <inheritdoc />
    Task IKasaOutletBase.IScheduleCommandsSingleOutlet.Delete(string scheduleId) {
        return DeleteSchedule(scheduleId, null);
    }

    internal Task DeleteSchedule(string id, ChildContext? context) {
        return Client.Send<JObject>(CommandFamily.Schedule, "delete_rule", new { id }, context);
    }

    /// <inheritdoc />
    Task IKasaOutletBase.IScheduleCommandsSingleOutlet.DeleteAll() {
        return DeleteAllSchedules(null);
    }

    internal Task DeleteAllSchedules(ChildContext? context) {
        return Client.Send<JObject>(CommandFamily.Schedule, "delete_all_rules", null, context);
    }

}