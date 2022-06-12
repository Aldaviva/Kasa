using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Kasa.Marshal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Kasa;

internal class KasaClient: IKasaClient {

    private const int HeaderLength = 4;

    private static readonly  SemaphoreSlim  TcpMutex       = new(1);
    private static readonly  Encoding       Encoding       = Encoding.UTF8;
    internal static readonly JsonSerializer JsonSerializer = new();

    private TcpClient _tcpClient;

    private  ILogger<KasaClient> _logger    = new NullLogger<KasaClient>();
    internal ushort              Port       = 9999;
    private  long                _requestId = -1;
    private  bool                _disposed;

    public string Hostname { get; }

    public Options Options { get; set; } = new();

    public bool Connected => _tcpClient.Connected && _tcpClient.Client.Connected;

    static KasaClient() {
        JsonSerializer.Converters.Add(new MacAddressConverter());
        JsonSerializer.Converters.Add(new OperatingModeConverter());
        JsonSerializer.Converters.Add(new FeatureConverter());
    }

    public KasaClient(string hostname) {
        _tcpClient              =  new TcpClient(AddressFamily.InterNetwork);
        Hostname                =  hostname;
        Options.PropertyChanged += OnChangeOptions;
        OnChangeOptions();
    }

    private void OnChangeOptions(object? sender = null, PropertyChangedEventArgs? eventArgs = null) {
        if (eventArgs?.PropertyName is null or nameof(Options.LoggerFactory)) {
            _logger = Options.LoggerFactory?.CreateLogger<KasaClient>() ?? new NullLogger<KasaClient>();
        }

        if (eventArgs?.PropertyName is null or nameof(Options.SendTimeout)) {
            _tcpClient.SendTimeout = Math.Max(0, (int) Options.SendTimeout.TotalMilliseconds);
        }

        if (eventArgs?.PropertyName is null or nameof(Options.ReceiveTimeout)) {
            _tcpClient.ReceiveTimeout = Math.Max(0, (int) Options.ReceiveTimeout.TotalMilliseconds);
        }
    }

    /// <inheritDoc />
    // ExceptionAdjustment: M:System.Threading.SemaphoreSlim.Release -T:System.Threading.SemaphoreFullException
    public async Task Connect() {
        await TcpMutex.WaitAsync().ConfigureAwait(false);
        try {
            if (_tcpClient.Connected) {
                return;
            } else if (_disposed) {
                throw new ObjectDisposedException(nameof(KasaClient), $"Already disposed, please construct a new {nameof(KasaOutlet)} instance instead");
            }

            _logger.LogDebug("Connecting to Kasa device {host}:{port}", Hostname, Port);

            // try {
            //     return _tcpClient.ConnectAsync(Hostname, Port);
            // } catch (SocketException e) {
            //     Console.WriteLine(e);
            //     throw;
            // }

            Task Attempt() => _tcpClient.ConnectAsync(Hostname, Port);

#pragma warning disable Ex0100 // Member may throw undocumented exception
            await Retrier.InvokeWithRetry(Attempt, Options.MaxAttempts, _ => Options.RetryDelay, IsRetryAllowed).ConfigureAwait(false);
#pragma warning restore Ex0100 // Member may throw undocumented exception
        } catch (SocketException e) {
            throw new NetworkException("The TCP socket failed to connect", Hostname, e);
        } finally {
            TcpMutex.Release();
        }
    }

    /// <exception cref="NetworkException">if the TCP connection to the outlet failed and could not automatically reconnect</exception>
    /// <exception cref="ResponseParsingException">if the JSON received from the outlet contains unexpected data</exception>
    // ExceptionAdjustment: M:System.Threading.SemaphoreSlim.Release -T:System.Threading.SemaphoreFullException
    public async Task<T> Send<T>(CommandFamily commandFamily, string methodName, object? parameters = null) {
        await TcpMutex.WaitAsync().ConfigureAwait(false); //only one TCP write operation may occur in parallel, which is a requirement of TcpClient
        try {
            Task<T> Attempt() => SendWithoutRetry<T>(commandFamily, methodName, parameters);

#pragma warning disable Ex0100 // Member may throw undocumented exception
            return await Retrier.InvokeWithRetry(Attempt, Options.MaxAttempts, _ => Options.RetryDelay, IsRetryAllowed).ConfigureAwait(false);
#pragma warning restore Ex0100 // Member may throw undocumented exception
        } catch (SocketException e) {
            throw new NetworkException("The TCP socket failed to connect", Hostname, e);
        } catch (IOException e) {
            throw new NetworkException("The TCP server did not supply enough bytes", Hostname, e);
        } finally {
            TcpMutex.Release();
        }
    }

    /// <exception cref="SocketException"></exception>
    /// <exception cref="IOException"></exception>
    /// <exception cref="FeatureUnavailable"></exception>
    /// <exception cref="ResponseParsingException"></exception>
    // ExceptionAdjustment: M:System.Threading.Interlocked.Increment(System.Int64@) -T:System.NullReferenceException
    // ExceptionAdjustment: M:System.IO.Stream.WriteAsync(System.Byte[],System.Int32,System.Int32,System.Threading.CancellationToken) -T:System.NotSupportedException
    // ExceptionAdjustment: M:System.IO.Stream.ReadAsync(System.Byte[],System.Int32,System.Int32,System.Threading.CancellationToken) -T:System.NotSupportedException
    // ExceptionAdjustment: M:System.IO.Stream.WriteAsync(System.Byte[],System.Int32,System.Int32) -T:System.NotSupportedException
    private async Task<T> SendWithoutRetry<T>(CommandFamily commandFamily, string methodName, object? parameters) {
        /*
         * Send request
         */

        Stream tcpStream = await GetNetworkStream().ConfigureAwait(false);
        long   requestId = Interlocked.Increment(ref _requestId);
        JObject request = new(new JProperty(commandFamily.ToJsonString(), new JObject(
            new JProperty(methodName, parameters is null ? null : JObject.FromObject(parameters)))));
        byte[] requestBytes = Serialize(request, requestId);

        await tcpStream.WriteAsync(requestBytes, 0, requestBytes.Length, CancellationToken.None).ConfigureAwait(false);

        /*
         * Receive response
         */

        using MemoryStream responseBuffer = new();

        byte[] headerBuffer    = new byte[HeaderLength];
        int    headerBytesRead = await tcpStream.ReadAsync(headerBuffer, 0, HeaderLength, CancellationToken.None).ConfigureAwait(false);
        if (headerBytesRead != HeaderLength) {
            throw new IOException($"Failed to read {HeaderLength}-byte length header, actually read {headerBytesRead} bytes");
        }

        int expectedPayloadLength = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(headerBuffer, 0));
        await responseBuffer.WriteAsync(headerBuffer, 0, headerBytesRead).ConfigureAwait(false);

        byte[] payloadBuffer       = new byte[expectedPayloadLength];
        int    actualPayloadLength = await tcpStream.ReadAsync(payloadBuffer, 0, payloadBuffer.Length, CancellationToken.None).ConfigureAwait(false);
        if (actualPayloadLength != expectedPayloadLength) {
            throw new IOException($"Failed to read {expectedPayloadLength:N0}-byte length payload, actually read {actualPayloadLength:N0} bytes");
        }

        await responseBuffer.WriteAsync(payloadBuffer, 0, actualPayloadLength).ConfigureAwait(false);

        byte[] responseBytes = responseBuffer.ToArray();
        return Deserialize<T>(responseBytes, requestId, commandFamily, methodName);
    }

    /// <exception cref="SocketException">The TCP socket failed to connect.</exception>
    internal virtual async Task<Stream> GetNetworkStream() {
        await EnsureConnected().ConfigureAwait(false);
        return _tcpClient.GetStream();
    }

    protected internal byte[] Serialize(JToken request, long requestId) {
        string requestJson  = request.ToString(Formatting.None);
        byte[] requestBytes = new byte[HeaderLength + Encoding.GetByteCount(requestJson)];
        Array.Copy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(requestBytes.Length - HeaderLength)), requestBytes, HeaderLength);
        Encoding.GetBytes(requestJson, 0, requestJson.Length, requestBytes, HeaderLength);

        _logger.LogTrace("tcp-outgoing-{requestId} >> {requestJson}", requestId, requestJson);
        return Cipher(requestBytes);
    }

    /// <exception cref="FeatureUnavailable">If the device is missing a feature that is required to run the given method, such as running <c>EnergyMeter.GetInstantaneousPowerUsage()</c> on an EP10, which does not have the EnergyMeter Feature.</exception>
    /// <exception cref="ResponseParsingException">If the JSON response from the outlet cannot be deserialized into an object.</exception>
    protected internal T Deserialize<T>(IEnumerable<byte> responseBytes, long requestId, CommandFamily commandFamily, string methodName) {
        byte[] responseDeciphered = Decipher(responseBytes.ToArray());
        int    responseLength     = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(responseDeciphered, 0));
        string responseString     = Encoding.GetString(responseDeciphered, HeaderLength, responseLength);
        string requestMethod      = $"{commandFamily.ToJsonString()}.{methodName}";

        _logger.LogTrace("tcp-outgoing-{requestId} << {responseString}", requestId, responseString);
        try {
            JToken innerResponse = JObject.Parse(responseString)[commandFamily.ToJsonString()]!;
            if (innerResponse["err_msg"]?.Value<string>() == "module not support") {
                throw new FeatureUnavailable(requestMethod, Feature.EnergyMeter, Hostname);
            }

            return innerResponse[methodName]!.ToObject<T>(JsonSerializer)!;
        } catch (JsonReaderException e) {
            throw new ResponseParsingException(requestMethod, responseString, typeof(T), Hostname, e);
        }
    }

    protected internal static byte[] Cipher(IReadOnlyList<byte> inputBytes) => Cipher(inputBytes, false);

    protected internal static byte[] Decipher(IReadOnlyList<byte> inputBytes) => Cipher(inputBytes, true);

    private static byte[] Cipher(IReadOnlyList<byte> inputBytes, bool decipher) {
        byte[]              outputBytes = new byte[inputBytes.Count];
        IReadOnlyList<byte> keyStream   = decipher ? inputBytes : outputBytes;

        for (int i = 0; i < inputBytes.Count; i++) {
            outputBytes[i] = i switch {
                < HeaderLength => inputBytes[i],
                HeaderLength   => (byte) (inputBytes[i] ^ 171),
                _              => (byte) (inputBytes[i] ^ keyStream[i - 1])
            };
        }

        return outputBytes;
    }

    // ExceptionAdjustment: M:System.Net.Sockets.Socket.Disconnect(System.Boolean) -T:System.PlatformNotSupportedException
    // ExceptionAdjustment: M:System.Net.Sockets.Socket.ConnectAsync(System.Net.Sockets.SocketAsyncEventArgs) -T:System.NotSupportedException
    // ExceptionAdjustment: M:System.Net.Sockets.Socket.ConnectAsync(System.Net.Sockets.SocketAsyncEventArgs) -T:System.Security.SecurityException
    /// <exception cref="ObjectDisposedException">This instance has already been disposed.</exception>
    /// <exception cref="SocketException">The TCP socket failed to connect.</exception>
    protected internal virtual async Task EnsureConnected(bool forceReconnect = false) {
        if (_disposed) {
            throw new ObjectDisposedException(nameof(KasaClient), "This KasaClient has already been disposed. Please construct a new KasaOutlet instance and use that instead.");
        }

        Socket socket = _tcpClient.Client;
        if (!socket.Connected || forceReconnect) {
            /*try {
                // If the server vanished without closing the connection (e.g. reboot), we need to manually disconnect the socket before reconnecting it, otherwise we will get "InvalidOperationException: The operation is not allowed on non-connected sockets."
                // socket.DisconnectAsync()
                // socket.Shutdown(SocketShutdown.Both);
                socket.Disconnect(true);

                // _logger.LogDebug("Disconnecting from Kasa device {host}:{port}", Hostname, Port);
                await Task.Factory.FromAsync(socket.BeginDisconnect, socket.EndDisconnect, true, null).ConfigureAwait(false);

            } catch (Exception e) when (e is SocketException or WebException) {
                // } catch (SocketException) {
                //socket has not been connected before, continue to connect for the first time
                // Console.WriteLine();
                // } catch (WebException) {
                //timed out, try to connect again
                // Console.WriteLine();
                // } catch (Exception e) {
                //     Console.WriteLine(e);
            }*/

            _tcpClient.Dispose();
            _tcpClient = new TcpClient(AddressFamily.InterNetwork);

            // SocketAsyncEventArgs e = new() { RemoteEndPoint = new DnsEndPoint(Hostname, Port) };
            _logger.LogDebug("Connecting to Kasa device {host}:{port}", Hostname, Port);
            await _tcpClient.ConnectAsync(Hostname, Port).ConfigureAwait(false);
            // await Task.Factory.FromAsync(socket.BeginConnect, socket.EndConnect, Hostname, (int) Port, null).ConfigureAwait(false);

            // if (socket.ConnectAsync(e)) {
            //     ManualResetEventSlim connected = new();
            //     SocketException?     exception = null;
            //
            //     e.Completed += (_, args) => {
            //         if (args.ConnectByNameError is SocketException ex) {
            //             exception = ex;
            //         } else if (args.SocketError != SocketError.Success) {
            //             exception = new SocketException((int) args.SocketError);
            //         }
            //
            //         connected.Set();
            //     };
            //
            //     connected.Wait();
            //     if (exception != null) {
            //         throw exception;
            //     }
            // }
        }
    }

    internal static bool IsRetryAllowed(Exception exception) => exception switch {
        ObjectDisposedException                                            => false, // consumer reused object from this library after disposing it, retries would fail anyway, improper usage
        SocketException { SocketErrorCode: SocketError.HostNotFound }      => false, // mistyped hostname, possibly invalid
        SocketException { SocketErrorCode: SocketError.TimedOut }          => true,  // no such IP address, might be rebooting
        SocketException { SocketErrorCode: SocketError.ConnectionRefused } => true,  // wrong port, might be rebooting
        IOException                                                        => true,  // error reading or writing enough bytes, may fix itself later
        FeatureUnavailable                                                 => false,
        ResponseParsingException                                           => false,
        _                                                                  => true
    };

    public void Dispose() {
        _tcpClient.Dispose();
        _disposed = true;
    }

}