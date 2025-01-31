namespace Kasa;

/// <summary>
/// <para>A TP-Link Kasa outlet or plug. This class is the main entry point of the Kasa library for devices with exactly one outlet, like the EP10. The corresponding interface is <see cref="IKasaOutlet"/>.</para>
/// <para>For devices with multiple outlets like the EP40, you should instead construct a new instance of <see cref="MultiSocketKasaOutlet"/>, rather than this class.</para>
/// <para>You may optionally call <see cref="Connect"/> on each instance before using it. If you don't, it will connect automatically when sending the first command.</para>
/// <para>Remember to <c>Dispose</c> each instance when you're done using it in order to close the TCP connection with the device. Disposed instances may not be reused, even if you call <see cref="Connect"/> again.</para>
/// <para>To communicate with multiple Kasa devices, construct multiple <see cref="KasaOutlet"/> instances, one per device.</para>
/// <para>Example usage:</para>
/// <code>using IKasaOutlet outlet = new KasaOutlet("192.168.1.100");
/// bool isSocketOn = await outlet.System.IsSocketOn();
/// if(!isSocketOn){
///     await outlet.System.SetSocketOn(true);
/// }</code>
/// </summary>
public partial class KasaOutlet: IKasaOutlet, IKasaOutletBase.ISystemCommands.ISingleSocket, IKasaOutletBase.ITimeCommands, IKasaOutletBase.IEnergyMeterCommands,
    IKasaOutletBase.ITimerCommandsSingleSocket, IKasaOutletBase.IScheduleCommandsSingleSocket, IKasaOutletBase.ICloudCommands {

    private readonly IKasaClient _client;

    /// <inheritdoc />
    public string Hostname => _client.Hostname;

    /// <inheritdoc />
    public Options Options {
        get => _client.Options;
        set => _client.Options = value;
    }

    /// <summary>
    /// <para>Construct a new instance of a <see cref="KasaOutlet"/> to talk to a Kasa outlet with one AC socket using the given hostname.</para>
    /// <para>If the Kasa outlet has multiple AC sockets (like an EP40), construct a new <see cref="MultiSocketKasaOutlet"/> instead.</para>
    /// <para>After constructing an instance, you may optionally call <see cref="Connect"/> to establish a TCP connection to the device before you send commands. If you don't, it will connect automatically when you send the first command.</para>
    /// <para>To communicate with multiple Kasa devices, construct multiple <see cref="KasaOutlet"/> instances, one per device.</para>
    /// <para>Remember to <see cref="Dispose()"/> each instance when you're done using it and want to disconnect from the TCP session. Disposed instances may not be reused, even if you call <see cref="Connect"/> again.</para>
    /// </summary>
    /// <param name="hostname">The fully-qualified domain name or IP address of the Kasa device to which this instance should connect.</param>
    /// <param name="options">Non-required configuration parameters for the <see cref="KasaOutlet"/>, which can be used to fine-tune its behavior.</param>
    public KasaOutlet(string hostname, Options? options = null): this(new KasaClient(hostname) { Options = options ?? new Options() }) { }

    internal KasaOutlet(IKasaClient client) {
        _client = client;
    }

    /// <inheritdoc />
    public Task Connect() {
        return _client.Connect();
    }

    /// <summary>
    /// <para>Disconnects and disposes the TCP client.</para>
    /// </summary>
    /// <param name="disposing"><c>true</c> to dispose the TCP client, <c>false</c> if you're running in a finalizer and it's already been disposed</param>
    protected virtual void Dispose(bool disposing) {
        if (disposing) {
            _client.Dispose();
        }
    }

    /// <summary>
    /// <para>Disconnect and dispose the TCP client.</para>
    /// <para>After calling this method, you can't use this instance again, even if you call <see cref="Connect"/> again. Instead, you should construct a new instance.</para>
    /// </summary>
    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

}