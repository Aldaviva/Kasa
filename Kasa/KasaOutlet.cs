using System;
using System.Threading.Tasks;

namespace Kasa;

/// <summary>
/// <para>A TP-Link Kasa outlet or plug. This class is the main entry point of the Kasa library. The corresponding interface is <see cref="IKasaOutlet"/>.</para>
/// <para>You may optionally call <see cref="Connect"/> on each instance before using it. If you don't, it will connect automatically when sending the first command.</para>
/// <para>Remember to <c>Dispose</c> each instance when you're done using it in order to close the TCP connection with the device. Disposed instances may not be reused, even if you call <see cref="Connect"/> again.</para>
/// <para>To communicate with multiple Kasa devices, construct multiple <see cref="KasaOutlet"/> instances, one per device.</para>
/// <para>Example usage:</para>
/// <code>using IKasaOutlet outlet = new KasaOutlet("192.168.1.100");
/// bool isOutletOn = await outlet.System.IsOutletOn();
/// if(!isOutletOn){
///     await outlet.System.SetOutletOn(true);
/// }</code>
/// </summary>
public partial class KasaOutlet: IKasaOutlet, IKasaOutlet.ISystemCommands, IKasaOutlet.ITimeCommands, IKasaOutlet.IEnergyMeterCommands, IKasaOutlet.ITimerCommands, IKasaOutlet.IScheduleCommands {

    private readonly IKasaClient _client;

    /// <inheritdoc />
    public string Hostname => _client.Hostname;

    /// <inheritdoc />
    public Options Options {
        get => _client.Options;
        set => _client.Options = value;
    }

    /// <summary>
    /// <para>Construct a new instance of a <see cref="KasaClient"/> to talk to a Kasa device with the given hostname.</para>
    /// <para>After constructing an instance, you may optionally call <see cref="Connect"/> to establish a TCP connection to the device before you send commands. If you don't, it will connect automatically when you send the first command.</para>
    /// <para>To communicate with multiple Kasa devices, construct multiple <see cref="KasaOutlet"/> instances, one per device.</para>
    /// <para>Remember to <see cref="Dispose()"/> each instance when you're done using it and want to disconnect from the TCP session. Disposed instances may not be reused, even if you call <see cref="Connect"/> again.</para>
    /// </summary>
    /// <param name="hostname">The fully-qualified domain name or IP address of the Kasa device to which this instance should connect.</param>
    /// <param name="options">Non-required configuration parameters for the <see cref="IKasaOutlet"/>, which can be used to fine-tune its behavior.</param>
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
    /// <para>Subclasses should call this base method with <c>disposing = true</c> in their overriding <c>Dispose</c> implementations.</para>
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