namespace Kasa;

public class KasaMultiOutlet: KasaOutlet, IKasaMultiOutlet, IKasaOutletBase.ISystemCommands.MultiOutlet, IKasaOutletBase.ITimerCommandsMultiOutlet, IKasaOutletBase.IScheduleCommandsMultiOutlet {

    private readonly AsyncLazy<IList<string>> _childIds;

    /// <inheritdoc cref="KasaOutlet(string, Options?)" />
    public KasaMultiOutlet(string hostname, Options? options = null): base(hostname, options) {
        _childIds = InitChildIds();
    }

    internal KasaMultiOutlet(IKasaClient client): base(client) {
        _childIds = InitChildIds();
    }

    private AsyncLazy<IList<string>> InitChildIds() => new(async () => SystemInfoToChildIds(await System.GetInfo().ConfigureAwait(false)));

    /// <inheritdoc />
    public new IKasaOutletBase.ISystemCommands.MultiOutlet System => this;

    /// <inheritdoc />
    public new IKasaOutletBase.ITimerCommandsMultiOutlet Timer => this;

    /// <inheritdoc />
    public new IKasaOutletBase.IScheduleCommandsMultiOutlet Schedule => this;

    /// <exception cref="ArgumentOutOfRangeException"><paramref name="outletId"/> is outside the range of 0-indexed outlets on the device</exception>
    private async Task<ChildContext> GetChildContext(int outletId) => new((await _childIds.GetValue().ConfigureAwait(false))[outletId]);

    private static IList<string> SystemInfoToChildIds(SystemInfo systemInfo) => systemInfo.Children?.Select(child => child.Id).ToList() ?? [];

    protected override async Task<SystemInfo> GetInfo() {
        SystemInfo systemInfo = await base.GetInfo().ConfigureAwait(false);
        if (!_childIds.IsValueCreated) {
            _childIds.TrySetValue(SystemInfoToChildIds(systemInfo), true);
        }
        return systemInfo;
    }

    async Task<int> IKasaOutletBase.ISystemCommands.CountOutlets() => (await _childIds.GetValue().ConfigureAwait(false)).Count;

    /// <exception cref="ArgumentOutOfRangeException"><paramref name="outletId"/> is outside the range of 0-indexed outlets on the device</exception>
    async Task<bool> IKasaOutletBase.ISystemCommands.MultiOutlet.IsOutletOn(int outletId) {
        ChildOutlet childOutlet = (await System.GetInfo().ConfigureAwait(false)).Children?.ElementAt(outletId)
            ?? throw new ArgumentOutOfRangeException(nameof(outletId), outletId, "Kasa device does not have multiple outlets");
        return childOutlet.IsOutletOn;
    }

    /// <exception cref="ArgumentOutOfRangeException"><paramref name="outletId"/> is outside the range of 0-indexed outlets on the device</exception>
    async Task IKasaOutletBase.ISystemCommands.MultiOutlet.SetOutletOn(int outletId, bool turnOn) {
        ChildContext context = await GetChildContext(outletId).ConfigureAwait(false);
        await SetOutletOn(turnOn, context).ConfigureAwait(false);
    }

    /// <exception cref="ArgumentOutOfRangeException"><paramref name="outletId"/> is outside the range of 0-indexed outlets on the device</exception>
    async Task<string> IKasaOutletBase.ISystemCommands.MultiOutlet.GetName(int outletId) {
        ChildOutlet childOutlet = (await System.GetInfo().ConfigureAwait(false)).Children?.ElementAt(outletId)
            ?? throw new ArgumentOutOfRangeException(nameof(outletId), outletId, "Kasa device does not have multiple outlets");
        return childOutlet.Name;
    }

    /// <exception cref="ArgumentOutOfRangeException"><paramref name="outletId"/> is outside the range of 0-indexed outlets on the device</exception>
    async Task IKasaOutletBase.ISystemCommands.MultiOutlet.SetName(int outletId, string name) {
        ChildContext context = await GetChildContext(outletId).ConfigureAwait(false);
        await SetName(name, context).ConfigureAwait(false);
    }

    async Task<Timer?> IKasaOutletBase.ITimerCommandsMultiOutlet.Get(int outletId) {
        ChildContext context = await GetChildContext(outletId).ConfigureAwait(false);
        return await GetTimer(context).ConfigureAwait(false);
    }

    async Task<Timer> IKasaOutletBase.ITimerCommandsMultiOutlet.Start(int outletId, TimeSpan duration, bool setOutletOnWhenComplete) {
        ChildContext context = await GetChildContext(outletId).ConfigureAwait(false);
        return await StartTimer(duration, setOutletOnWhenComplete, context).ConfigureAwait(false);
    }

    async Task IKasaOutletBase.ITimerCommandsMultiOutlet.Clear(int outletId) {
        ChildContext context = await GetChildContext(outletId).ConfigureAwait(false);
        await ClearTimers(context).ConfigureAwait(false);
    }

    async Task<IEnumerable<Schedule>> IKasaOutletBase.IScheduleCommandsMultiOutlet.GetAll(int outletId) {
        ChildContext context = await GetChildContext(outletId).ConfigureAwait(false);
        return await GetAllSchedules(context).ConfigureAwait(false);
    }

    async Task<Schedule> IKasaOutletBase.IScheduleCommandsMultiOutlet.Save(int outletId, Schedule schedule) {
        ChildContext context = await GetChildContext(outletId).ConfigureAwait(false);
        return await SaveSchedule(schedule, context).ConfigureAwait(false);
    }

    async Task IKasaOutletBase.IScheduleCommandsMultiOutlet.Delete(int outletId, Schedule schedule) {
        ChildContext context = await GetChildContext(outletId).ConfigureAwait(false);
        await DeleteSchedule(schedule, context).ConfigureAwait(false);
    }

    async Task IKasaOutletBase.IScheduleCommandsMultiOutlet.Delete(int outletId, string scheduleId) {
        ChildContext context = await GetChildContext(outletId).ConfigureAwait(false);
        await DeleteSchedule(scheduleId, context).ConfigureAwait(false);
    }

    async Task IKasaOutletBase.IScheduleCommandsMultiOutlet.DeleteAll(int outletId) {
        ChildContext context = await GetChildContext(outletId).ConfigureAwait(false);
        await DeleteAllSchedules(context).ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing) {
        if (disposing) {
            _childIds.Dispose();
        }
        base.Dispose(disposing);
    }

}