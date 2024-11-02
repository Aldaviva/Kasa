namespace Kasa;

/// <summary>
/// <para>A TP-Link Kasa outlet with multiple sockets. This class is the main entry point of the Kasa library for devices with more than one socket, like the EP40. The corresponding interface is <see cref="IMultiSocketKasaOutlet"/>. Individual socket IDs used in method parameters are 0-indexed.</para>
/// <para>For devices with exactly one socket like the EP10, you should instead construct a new instance of <see cref="KasaOutlet"/>, rather than this class.</para>
/// <para>You may optionally call <see cref="KasaOutlet.Connect"/> on each instance before using it. If you don't, it will connect automatically when sending the first command.</para>
/// <para>Remember to <c>Dispose</c> each instance when you're done using it in order to close the TCP connection with the device. Disposed instances may not be reused, even if you call <see cref="KasaOutlet.Connect"/> again.</para>
/// <para>To communicate with multiple Kasa devices, construct multiple <see cref="MultiSocketKasaOutlet"/> instances, one per device.</para>
/// <para>Example usage:</para>
/// <code>using IMultiSocketKasaOutlet outlet = new MultiSocketKasaOutlet("192.168.1.100");
/// bool isOutletOn = await outlet.System.IsOutletOn(0);
/// if (!isOutletOn) {
///     await outlet.System.SetOutletOn(0, true);
/// }</code>
/// </summary>
public class MultiSocketKasaOutlet: KasaOutlet, IMultiSocketKasaOutlet, IKasaOutletBase.ISystemCommands.IMultiSocket, IKasaOutletBase.ITimerCommandsMultiSocket,
    IKasaOutletBase.IScheduleCommandsMultiSocket {

    private readonly AsyncLazy<IList<string>> _socketIds;

    /// <summary>
    /// <para>Construct a new instance of a <see cref="MultiSocketKasaOutlet"/> to talk to a Kasa outlet with multiple AC sockets using the given hostname.</para>
    /// <para>If the Kasa outlet has exactly one AC socket (like an EP10), construct a new <see cref="KasaOutlet"/> instead.</para>
    /// <para>After constructing an instance, you may optionally call <see cref="KasaOutlet.Connect"/> to establish a TCP connection to the device before you send commands. If you don't, it will connect automatically when you send the first command.</para>
    /// <para>To communicate with multiple Kasa devices, construct multiple <see cref="MultiSocketKasaOutlet"/> instances, one per device.</para>
    /// <para>Remember to <see cref="KasaOutlet.Dispose()"/> each instance when you're done using it and want to disconnect from the TCP session. Disposed instances may not be reused, even if you call <see cref="KasaOutlet.Connect"/> again.</para>
    /// </summary>
    /// <param name="hostname">The fully-qualified domain name or IP address of the Kasa device to which this instance should connect.</param>
    /// <param name="options">Non-required configuration parameters for the <see cref="MultiSocketKasaOutlet"/>, which can be used to fine-tune its behavior.</param>
    public MultiSocketKasaOutlet(string hostname, Options? options = null): base(hostname, options) {
        _socketIds = InitSocketIds();
    }

    internal MultiSocketKasaOutlet(IKasaClient client): base(client) {
        _socketIds = InitSocketIds();
    }

    private AsyncLazy<IList<string>> InitSocketIds() => new(async () => SystemInfoToSocketIds(await System.GetInfo().ConfigureAwait(false)));

    /// <inheritdoc />
    public new IKasaOutletBase.ISystemCommands.IMultiSocket System => this;

    /// <inheritdoc />
    public new IKasaOutletBase.ITimerCommandsMultiSocket Timer => this;

    /// <inheritdoc />
    public new IKasaOutletBase.IScheduleCommandsMultiSocket Schedule => this;

    private async Task<SocketContext> GetSocketContext(int socketId) => new((await _socketIds.GetValue().ConfigureAwait(false))[socketId]);

    private static IList<string> SystemInfoToSocketIds(SystemInfo systemInfo) => systemInfo.Sockets?.Select(socket => socket.Id).ToList() ?? [];

    /// <inheritdoc />
    protected override async Task<SystemInfo> GetInfo() {
        SystemInfo systemInfo = await base.GetInfo().ConfigureAwait(false);
        if (!_socketIds.IsValueCreated) {
            _socketIds.TrySetValue(SystemInfoToSocketIds(systemInfo), true);
        }
        return systemInfo;
    }

    async Task<int> IKasaOutletBase.ISystemCommands.CountSockets() => (await _socketIds.GetValue().ConfigureAwait(false)).Count;

    /// <inheritdoc />
    async Task<bool> IKasaOutletBase.ISystemCommands.IMultiSocket.IsSocketOn(int socketId) {
        Socket socket = (await System.GetInfo().ConfigureAwait(false)).Sockets?.ElementAt(socketId)
            ?? throw new ArgumentOutOfRangeException(nameof(socketId), socketId, "Kasa outlet does not have multiple sockets");
        return socket.IsOutletOn;
    }

    /// <inheritdoc />
    async Task IKasaOutletBase.ISystemCommands.IMultiSocket.SetSocketOn(int socketId, bool turnOn) {
        SocketContext context = await GetSocketContext(socketId).ConfigureAwait(false);
        await SetSocketOn(turnOn, context).ConfigureAwait(false);
    }

    /// <inheritdoc />
    async Task<string> IKasaOutletBase.ISystemCommands.IMultiSocket.GetName(int socketId) {
        Socket socket = (await System.GetInfo().ConfigureAwait(false)).Sockets?.ElementAt(socketId)
            ?? throw new ArgumentOutOfRangeException(nameof(socketId), socketId, "Kasa outlet does not have multiple sockets");
        return socket.Name;
    }

    /// <inheritdoc />
    async Task IKasaOutletBase.ISystemCommands.IMultiSocket.SetName(int socketId, string name) {
        SocketContext context = await GetSocketContext(socketId).ConfigureAwait(false);
        await SetName(name, context).ConfigureAwait(false);
    }

    /// <inheritdoc />
    async Task<Timer?> IKasaOutletBase.ITimerCommandsMultiSocket.Get(int socketId) {
        SocketContext context = await GetSocketContext(socketId).ConfigureAwait(false);
        return await GetTimer(context).ConfigureAwait(false);
    }

    /// <inheritdoc />
    async Task<Timer> IKasaOutletBase.ITimerCommandsMultiSocket.Start(int socketId, TimeSpan duration, bool setSocketOnWhenComplete) {
        SocketContext context = await GetSocketContext(socketId).ConfigureAwait(false);
        return await StartTimer(duration, setSocketOnWhenComplete, context).ConfigureAwait(false);
    }

    /// <inheritdoc />
    async Task IKasaOutletBase.ITimerCommandsMultiSocket.Clear(int socketId) {
        SocketContext context = await GetSocketContext(socketId).ConfigureAwait(false);
        await ClearTimers(context).ConfigureAwait(false);
    }

    /// <inheritdoc />
    async Task<IEnumerable<Schedule>> IKasaOutletBase.IScheduleCommandsMultiSocket.GetAll(int socketId) {
        SocketContext context = await GetSocketContext(socketId).ConfigureAwait(false);
        return await GetAllSchedules(context).ConfigureAwait(false);
    }

    /// <inheritdoc />
    async Task<Schedule> IKasaOutletBase.IScheduleCommandsMultiSocket.Save(int socketId, Schedule schedule) {
        SocketContext context = await GetSocketContext(socketId).ConfigureAwait(false);
        return await SaveSchedule(schedule, context).ConfigureAwait(false);
    }

    /// <inheritdoc />
    async Task IKasaOutletBase.IScheduleCommandsMultiSocket.Delete(int socketId, Schedule schedule) {
        SocketContext context = await GetSocketContext(socketId).ConfigureAwait(false);
        await DeleteSchedule(schedule, context).ConfigureAwait(false);
    }

    /// <inheritdoc />
    async Task IKasaOutletBase.IScheduleCommandsMultiSocket.Delete(int socketId, string scheduleId) {
        SocketContext context = await GetSocketContext(socketId).ConfigureAwait(false);
        await DeleteSchedule(scheduleId, context).ConfigureAwait(false);
    }

    /// <inheritdoc />
    async Task IKasaOutletBase.IScheduleCommandsMultiSocket.DeleteAll(int socketId) {
        SocketContext context = await GetSocketContext(socketId).ConfigureAwait(false);
        await DeleteAllSchedules(context).ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing) {
        if (disposing) {
            _socketIds.Dispose();
        }
        base.Dispose(disposing);
    }

    /// <inheritdoc />
    Task<bool> IKasaOutletBase.ISystemCommands.IMultiSocket.IsOutletOn(int socketId) => System.IsSocketOn(socketId);

    /// <inheritdoc />
    Task IKasaOutletBase.ISystemCommands.IMultiSocket.SetOutletOn(int socketId, bool turnOn) => System.SetSocketOn(socketId, turnOn);

}