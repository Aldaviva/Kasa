using Newtonsoft.Json.Linq;

namespace Kasa;

public partial class KasaOutlet {

    /// <inheritdoc />
    public IKasaOutletBase.IScheduleCommandsSingleSocket Schedule => this;

    /// <inheritdoc />
    async Task<IEnumerable<Schedule>> IKasaOutletBase.IScheduleCommandsSingleSocket.GetAll() {
        return await GetAllSchedules(null).ConfigureAwait(false);
    }

    /// <exception cref="NetworkException"></exception>
    /// <exception cref="ResponseParsingException"></exception>
    internal async Task<IEnumerable<Schedule>> GetAllSchedules(SocketContext? context) {
        JObject response = await _client.Send<JObject>(CommandFamily.Schedule, "get_rules", null, context).ConfigureAwait(false);
        return response["rule_list"]!.ToObject<IEnumerable<Schedule>>(KasaClient.JsonSerializer)!;
    }

    /// <inheritdoc />
    async Task<Schedule> IKasaOutletBase.IScheduleCommandsSingleSocket.Save(Schedule schedule) {
        return await SaveSchedule(schedule, null).ConfigureAwait(false);
    }

    /// <exception cref="NetworkException"></exception>
    /// <exception cref="ResponseParsingException"></exception>
    internal async Task<Schedule> SaveSchedule(Schedule schedule, SocketContext? context) {
        string id;
        if (schedule.Id is null) {
            JObject created = await _client.Send<JObject>(CommandFamily.Schedule, "add_rule", schedule, context).ConfigureAwait(false);
            id = created["id"]!.ToObject<string>(KasaClient.JsonSerializer)!;
        } else {
            await _client.Send<JObject>(CommandFamily.Schedule, "edit_rule", schedule, context).ConfigureAwait(false);
            id = schedule.Id;
        }

        return (await ((IKasaOutletBase.IScheduleCommandsSingleSocket) this).GetAll().ConfigureAwait(false)).FirstOrDefault(schedule1 => schedule1.Id == id);
    }

    /// <inheritdoc />
    Task IKasaOutletBase.IScheduleCommandsSingleSocket.Delete(Schedule schedule) {
        return DeleteSchedule(schedule, null);
    }

    /// <exception cref="ArgumentException">If the <paramref name="schedule"/> parameter has a <c>null</c> value for the <see cref="Schedule.Id"/> property, possibly because it was newly constructed instead of being fetched from <see cref="IKasaOutletBase.IScheduleCommandsSingleSocket.GetAll"/> or <see cref="IKasaOutletBase.IScheduleCommandsSingleSocket.Save"/>.</exception>
    /// <exception cref="NetworkException"></exception>
    /// <exception cref="ResponseParsingException"></exception>
    internal Task DeleteSchedule(Schedule schedule, SocketContext? context) {
        if (schedule.Id is not null) {
            return DeleteSchedule(schedule.Id, context);
        } else {
            throw new ArgumentException(
                $"The input schedule must have been fetched from the Kasa outlet (using {nameof(IKasaOutletBase.IScheduleCommandsSingleSocket.GetAll)}, but the given instance has a null {nameof(Kasa.Schedule.Id)}.");
        }
    }

    /// <inheritdoc />
    Task IKasaOutletBase.IScheduleCommandsSingleSocket.Delete(string scheduleId) {
        return DeleteSchedule(scheduleId, null);
    }

    /// <exception cref="NetworkException"></exception>
    /// <exception cref="ResponseParsingException"></exception>
    internal Task DeleteSchedule(string id, SocketContext? context) {
        return _client.Send<JObject>(CommandFamily.Schedule, "delete_rule", new { id }, context);
    }

    /// <inheritdoc />
    Task IKasaOutletBase.IScheduleCommandsSingleSocket.DeleteAll() {
        return DeleteAllSchedules(null);
    }

    /// <exception cref="NetworkException"></exception>
    /// <exception cref="ResponseParsingException"></exception>
    internal Task DeleteAllSchedules(SocketContext? context) {
        return _client.Send<JObject>(CommandFamily.Schedule, "delete_all_rules", null, context);
    }

}