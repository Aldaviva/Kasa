namespace Kasa;

/// <summary>
/// <para>A TP-Link Kasa device with multiple outlets. This class is the main entry point of the Kasa library for devices with more than one outlet, like the EP40. The corresponding interface is <see cref="IKasaMultiOutlet"/>. Individual outlet IDs used in method parameters are 0-indexed.</para>
/// <para>For devices with exactly one outlet like the EP10, you should instead construct a new instance of <see cref="KasaOutlet"/>, rather than this class.</para>
/// <para>You may optionally call <see cref="KasaOutlet.Connect"/> on each instance before using it. If you don't, it will connect automatically when sending the first command.</para>
/// <para>Remember to <c>Dispose</c> each instance when you're done using it in order to close the TCP connection with the device. Disposed instances may not be reused, even if you call <see cref="KasaOutlet.Connect"/> again.</para>
/// <para>To communicate with multiple Kasa devices, construct multiple <see cref="KasaMultiOutlet"/> instances, one per device.</para>
/// <para>Example usage:</para>
/// <code>using IKasaMultiOutlet outlet = new KasaMultiOutlet("192.168.1.100");
/// bool isOutletOn = await outlet.System.IsOutletOn(0);
/// if (!isOutletOn) {
///     await outlet.System.SetOutletOn(0, true);
/// }</code>
/// </summary>
public class KasaMultiOutlet: KasaOutlet, IKasaMultiOutlet, IKasaOutletBase.ISystemCommands.IMultiOutlet, IKasaOutletBase.ITimerCommandsMultiOutlet, IKasaOutletBase.IScheduleCommandsMultiOutlet {

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
    public new IKasaOutletBase.ISystemCommands.IMultiOutlet System => this;

    /// <inheritdoc />
    public new IKasaOutletBase.ITimerCommandsMultiOutlet Timer => this;

    /// <inheritdoc />
    public new IKasaOutletBase.IScheduleCommandsMultiOutlet Schedule => this;

    private async Task<ChildContext> GetChildContext(int outletId) => new((await _childIds.GetValue().ConfigureAwait(false))[outletId]);

    private static IList<string> SystemInfoToChildIds(SystemInfo systemInfo) => systemInfo.Children?.Select(child => child.Id).ToList() ?? [];

    /// <inheritdoc />
    protected override async Task<SystemInfo> GetInfo() {
        SystemInfo systemInfo = await base.GetInfo().ConfigureAwait(false);
        if (!_childIds.IsValueCreated) {
            _childIds.TrySetValue(SystemInfoToChildIds(systemInfo), true);
        }
        return systemInfo;
    }

    async Task<int> IKasaOutletBase.ISystemCommands.CountOutlets() => (await _childIds.GetValue().ConfigureAwait(false)).Count;

    /// <inheritdoc />
    async Task<bool> IKasaOutletBase.ISystemCommands.IMultiOutlet.IsOutletOn(int outletId) {
        ChildOutlet childOutlet = (await System.GetInfo().ConfigureAwait(false)).Children?.ElementAt(outletId)
            ?? throw new ArgumentOutOfRangeException(nameof(outletId), outletId, "Kasa device does not have multiple outlets");
        return childOutlet.IsOutletOn;
    }

    /// <inheritdoc />
    async Task IKasaOutletBase.ISystemCommands.IMultiOutlet.SetOutletOn(int outletId, bool turnOn) {
        ChildContext context = await GetChildContext(outletId).ConfigureAwait(false);
        await SetOutletOn(turnOn, context).ConfigureAwait(false);
    }

    /// <inheritdoc />
    async Task<string> IKasaOutletBase.ISystemCommands.IMultiOutlet.GetName(int outletId) {
        ChildOutlet childOutlet = (await System.GetInfo().ConfigureAwait(false)).Children?.ElementAt(outletId)
            ?? throw new ArgumentOutOfRangeException(nameof(outletId), outletId, "Kasa device does not have multiple outlets");
        return childOutlet.Name;
    }

    /// <inheritdoc />
    async Task IKasaOutletBase.ISystemCommands.IMultiOutlet.SetName(int outletId, string name) {
        ChildContext context = await GetChildContext(outletId).ConfigureAwait(false);
        await SetName(name, context).ConfigureAwait(false);
    }

    /// <inheritdoc />
    async Task<Timer?> IKasaOutletBase.ITimerCommandsMultiOutlet.Get(int outletId) {
        ChildContext context = await GetChildContext(outletId).ConfigureAwait(false);
        return await GetTimer(context).ConfigureAwait(false);
    }

    /// <inheritdoc />
    async Task<Timer> IKasaOutletBase.ITimerCommandsMultiOutlet.Start(int outletId, TimeSpan duration, bool setOutletOnWhenComplete) {
        ChildContext context = await GetChildContext(outletId).ConfigureAwait(false);
        return await StartTimer(duration, setOutletOnWhenComplete, context).ConfigureAwait(false);
    }

    /// <inheritdoc />
    async Task IKasaOutletBase.ITimerCommandsMultiOutlet.Clear(int outletId) {
        ChildContext context = await GetChildContext(outletId).ConfigureAwait(false);
        await ClearTimers(context).ConfigureAwait(false);
    }

    /// <inheritdoc />
    async Task<IEnumerable<Schedule>> IKasaOutletBase.IScheduleCommandsMultiOutlet.GetAll(int outletId) {
        ChildContext context = await GetChildContext(outletId).ConfigureAwait(false);
        return await GetAllSchedules(context).ConfigureAwait(false);
    }

    /// <inheritdoc />
    async Task<Schedule> IKasaOutletBase.IScheduleCommandsMultiOutlet.Save(int outletId, Schedule schedule) {
        ChildContext context = await GetChildContext(outletId).ConfigureAwait(false);
        return await SaveSchedule(schedule, context).ConfigureAwait(false);
    }

    /// <inheritdoc />
    async Task IKasaOutletBase.IScheduleCommandsMultiOutlet.Delete(int outletId, Schedule schedule) {
        ChildContext context = await GetChildContext(outletId).ConfigureAwait(false);
        await DeleteSchedule(schedule, context).ConfigureAwait(false);
    }

    /// <inheritdoc />
    async Task IKasaOutletBase.IScheduleCommandsMultiOutlet.Delete(int outletId, string scheduleId) {
        ChildContext context = await GetChildContext(outletId).ConfigureAwait(false);
        await DeleteSchedule(scheduleId, context).ConfigureAwait(false);
    }

    /// <inheritdoc />
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