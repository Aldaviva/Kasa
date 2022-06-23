using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

namespace Kasa;

/// <summary>
/// <para>A TP-Link Kasa outlet on a power strip.</para>
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
public class KasaStripOutlet : IKasaOutlet.IEnergyMeterCommands, IDisposable {
    readonly string _id;
    readonly IKasaClient _client;

    /// <summary>
    /// <para>Construct a new instance of a <see cref="KasaStripOutlet"/> to talk to an outlet on a strip.</para>
    /// <para>After constructing an instance, you may optionally call <see cref="Connect"/> to establish a TCP connection to the device before you send commands. If you don't, it will connect automatically when you send the first command.</para>
    /// <para>To communicate with multiple outlets, construct multiple <see cref="KasaStripOutlet"/> instances, one per device.</para>
    /// </summary>
    /// <param name="hostOrIP">The fully-qualified domain name or IP address of the Kasa device to which this instance should connect.</param>
    /// <param name="childID">ID of the outlet on the strip. See <see cref="Outlet.ID"/> from <see cref="SystemInfo.Children"/></param>
    /// <remarks>
    /// <para>Remember to <see cref="Dispose()"/> each instance when you're done using it and want to disconnect from the TCP session. Disposed instances may not be reused, even if you call <see cref="Connect"/> again.</para>
    /// </remarks>
    public KasaStripOutlet(string hostOrIP, string childID) {
        if (string.IsNullOrEmpty(childID)) throw new ArgumentException($"'{nameof(childID)}' cannot be null or empty.", nameof(childID));

        _client = new KasaClient(hostOrIP);
        _id = childID;
    }

    public IOptions ClientOptions => _client.Options;

    /// <summary>
    /// <para>Connects to the outlet.</para>
    /// <para>You may optionally call this to explicitly connect before sending any commands on the outlet.</para>
    /// <para>If you don't call this method before sending a command, then this instance will automatically connect before sending that command.</para>
    /// <para>If this instance is already connected, then this call does nothing and returns immediately.</para>
    /// </summary>
    /// <exception cref="ObjectDisposedException">This instance has already been disposed.</exception>
    /// <exception cref="NetworkException">The TCP connection failed.</exception>
    /// <remarks>
    /// Explicit connection may be helpful for early detection of errors, as well as for reducing the latency of the first command. Automatic connection may be more convenient because there are fewer methods to invoke.
    /// </remarks>
    public Task Connect() => _client.Connect();

    /// <inheritdoc/>
    public Task DeleteHistoricalUsage() {
        throw new System.NotImplementedException();
    }

    /// <inheritdoc/>
    public Task<IList<int>?> GetDailyEnergyUsage(int year, int month) {
        throw new System.NotImplementedException();
    }

    /// <inheritdoc/>
    public Task<PowerUsage> GetInstantaneousPowerUsage() {
        var command = new Command(CommandFamily.EnergyMeter, "get_realtime");
        this.ConfigureRequest(command.Json);
        return _client.Send<PowerUsage>(command, CancellationToken.None);
    }

    /// <inheritdoc/>
    public Task<IList<int>?> GetMonthlyEnergyUsage(int year) {
        throw new System.NotImplementedException();
    }

    void ConfigureRequest(JObject request) {
        request["context"] = new JObject(new JProperty("child_ids", new JArray(_id)));
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
