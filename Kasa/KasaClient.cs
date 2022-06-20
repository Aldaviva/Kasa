using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Kasa.Marshal;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Kasa;

internal class KasaClient: IKasaClient {

    private const int HeaderLength = 4;

    private static readonly  SemaphoreSlim  TcpMutex       = new(1);
    private static readonly  Encoding       Encoding       = Encoding.UTF8;
    internal static readonly JsonSerializer JsonSerializer = new();

    private readonly TcpClient _tcpClient;

    internal ushort Port       = 9999;
    private  long   _requestId = -1;
    private  bool   _disposed;

    public string Hostname { get; }

    private ILogger<KasaClient>? _logger;
    private ILoggerFactory?      _loggerFactory;

    public ILoggerFactory? LoggerFactory {
        get => _loggerFactory;
        set {
            _loggerFactory = value;
            _logger        = _loggerFactory?.CreateLogger<KasaClient>();
        }
    }

    public bool Connected => _tcpClient.Connected && _tcpClient.Client.Connected;

    static KasaClient() {
        JsonSerializer.Converters.Add(new MacAddressConverter());
        JsonSerializer.Converters.Add(new OperatingModeConverter());
        JsonSerializer.Converters.Add(new FeatureConverter());
    }

    public KasaClient(string hostname) {
        _tcpClient = new TcpClient(AddressFamily.InterNetwork);
        Hostname   = hostname;
    }

    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="SocketException"></exception>
    public Task Connect() {
        if (_tcpClient.Connected) {
            throw new InvalidOperationException("Already connected, go ahead and call a command method such as GetSystemInfo()");
        } else if (_disposed) {
            throw new InvalidOperationException($"Already disposed, please construct a new {nameof(KasaOutlet)} instance instead");
        }

        return _tcpClient.ConnectAsync(Hostname, Port);
    }

    /// <exception cref="SocketException"></exception>
    /// <exception cref="IOException"></exception>
    /// <exception cref="JsonReaderException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    // ExceptionAdjustment: P:System.Net.Sockets.TcpClient.ReceiveBufferSize get -T:System.Net.Sockets.SocketException
    // ExceptionAdjustment: M:System.Text.StringBuilder.AppendFormat(System.String,System.Object) -T:System.FormatException
    // ExceptionAdjustment: M:System.Threading.SemaphoreSlim.Release -T:System.Threading.SemaphoreFullException
    // ExceptionAdjustment: M:System.IO.Stream.ReadAsync(System.Byte[],System.Int32,System.Int32,System.Threading.CancellationToken) -T:System.NotSupportedException
    // ExceptionAdjustment: M:System.IO.Stream.WriteAsync(System.Byte[],System.Int32,System.Int32) -T:System.NotSupportedException
    // ExceptionAdjustment: M:System.IO.Stream.WriteAsync(System.Byte[],System.Int32,System.Int32,System.Threading.CancellationToken) -T:System.NotSupportedException
    // ExceptionAdjustment: M:System.Threading.Interlocked.Increment(System.Int64@) -T:System.NullReferenceException
    public async Task<T> Send<T>(CommandFamily commandFamily, string methodName, object? parameters = null) {
        EnsureConnected();
        await TcpMutex.WaitAsync().ConfigureAwait(false); //only one TCP write operation may occur in parallel
        try {
            // Send request

            long   requestId = Interlocked.Increment(ref _requestId);
            Stream tcpStream = GetNetworkStream();
            JObject request = new(new JProperty(commandFamily.ToJsonString(), new JObject(
                new JProperty(methodName, parameters is null ? null : JObject.FromObject(parameters)))));
            byte[] requestBytes = Serialize(request, requestId);

            await tcpStream.WriteAsync(requestBytes, 0, requestBytes.Length, CancellationToken.None).ConfigureAwait(false);

            // Receive response

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
        } finally {
            TcpMutex.Release();
        }
    }

    /// <exception cref="SocketException"></exception>
    internal virtual Stream GetNetworkStream() {
        EnsureConnected();
        return _tcpClient.GetStream();
    }

    protected internal byte[] Serialize(JToken request, long requestId) {
        string requestJson  = request.ToString(Formatting.None);
        byte[] requestBytes = new byte[HeaderLength + Encoding.GetByteCount(requestJson)];
        Array.Copy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(requestBytes.Length - HeaderLength)), requestBytes, HeaderLength);
        Encoding.GetBytes(requestJson, 0, requestJson.Length, requestBytes, HeaderLength);

        _logger?.LogTrace("tcp-outgoing-{requestId} >> {requestJson}", requestId, requestJson);
        return Cipher(requestBytes);
    }

    /// <exception cref="JsonReaderException"></exception>
    /// <exception cref="InvalidOperationException">If the device is missing a feature that is required to run the given method, such as running EnergyMeter.GetInstantaneousPowerUsage() on an EP10, which does not have the EnergyMeter Feature.</exception>
    protected internal T Deserialize<T>(IEnumerable<byte> responseBytes, long requestId, CommandFamily commandFamily, string methodName) {
        byte[] responseDeciphered = Decipher(responseBytes.ToArray());
        int    responseLength     = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(responseDeciphered, 0));
        string responseString     = Encoding.GetString(responseDeciphered, HeaderLength, responseLength);

        _logger?.LogTrace("tcp-outgoing-{requestId} << {responseString}", requestId, responseString);
        try {
            JToken innerResponse = JObject.Parse(responseString)[commandFamily.ToJsonString()]!;
            if (innerResponse["err_msg"]?.Value<string>() == "module not support") {
                throw new InvalidOperationException($"Kasa device is missing a feature required to run the {commandFamily.ToJsonString()}.{methodName} method. " +
                    $"You can check {nameof(IKasaOutlet)}.{CommandFamily.System}.{nameof(IKasaOutlet.ISystemCommands.GetInfo)}().Features to see which features your device has.");
            }

            return innerResponse[methodName]!.ToObject<T>(JsonSerializer)!;
        } catch (JsonReaderException e) {
            _logger?.LogError(e, "Failed to deserialize JSON to {destinationType}: {responseString}", typeof(T), responseString);
            throw;
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

    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="SocketException"></exception>
    // ExceptionAdjustment: M:System.Net.Sockets.Socket.Disconnect(System.Boolean) -T:System.PlatformNotSupportedException
    // ExceptionAdjustment: M:System.Net.Sockets.Socket.ConnectAsync(System.Net.Sockets.SocketAsyncEventArgs) -T:System.NotSupportedException
    // ExceptionAdjustment: M:System.Net.Sockets.Socket.ConnectAsync(System.Net.Sockets.SocketAsyncEventArgs) -T:System.Security.SecurityException
    protected internal virtual void EnsureConnected() {
        Socket socket = _tcpClient.Client;
        if (!socket.Connected) {
            try {
                // If the server vanished without closing the connection (e.g. reboot), we need to manually disconnect the socket before reconnecting it, otherwise we will get "InvalidOperationException: The operation is not allowed on non-connected sockets."
                socket.Disconnect(true);
            } catch (SocketException) {
                //socket has not been connected before
            }

            SocketAsyncEventArgs e = new() { RemoteEndPoint = new DnsEndPoint(Hostname, Port) };
            if (socket.ConnectAsync(e)) {
                ManualResetEventSlim connected = new();
                e.Completed += (sender, args) => connected.Set();
                connected.Wait();
            }

            switch (e.SocketError) {
            case SocketError.Success:
                return;

            default:
                throw e.ConnectByNameError;
            }
        }
    }

    public void Dispose() {
        _tcpClient.Close();
#if NET46_OR_GREATER || NETSTANDARD // there is only protected Dispose(bool) in NET452
        _tcpClient.Dispose();
#endif
        _disposed = true;
    }

}