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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using slf4net;

namespace Kasa;

internal interface IKasaClient: IDisposable {

    string Hostname { get; }
    bool Connected { get; }

    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="SocketException"></exception>
    Task Connect();

    /// <exception cref="SocketException"></exception>
    /// <exception cref="IOException"></exception>
    /// <exception cref="JsonReaderException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    Task<T> Send<T>(CommandFamily commandFamily, string methodName, object? parameters = null);

}

internal class KasaClient: IKasaClient {

    private const int HeaderLength = 4;

    private static readonly  ILogger        Logger         = LoggerFactory.GetLogger(typeof(KasaSmartOutlet));
    private static readonly  ILogger        WireLogger     = LoggerFactory.GetLogger("wire");
    private static readonly  SemaphoreSlim  TcpMutex       = new(1);
    private static readonly  Encoding       Encoding       = Encoding.UTF8;
    internal static readonly JsonSerializer JsonSerializer = new();

    private readonly TcpClient _tcpClient;

    internal ushort Port       = 9999;
    private  long   _requestId = -1;
    private  bool   _disposed;

    public string Hostname { get; }

    public bool Connected => _tcpClient.Connected;

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
            throw new InvalidOperationException("Already disposed, please construct a new KasaSmartPlug instance instead");
        }

        return _tcpClient.ConnectAsync(Hostname, Port);
    }

    // ExceptionAdjustment: P:System.Net.Sockets.TcpClient.ReceiveBufferSize get -T:System.Net.Sockets.SocketException
    // ExceptionAdjustment: M:System.Text.StringBuilder.AppendFormat(System.String,System.Object) -T:System.FormatException
    // ExceptionAdjustment: M:System.Threading.SemaphoreSlim.Release -T:System.Threading.SemaphoreFullException
    // ExceptionAdjustment: M:System.IO.Stream.ReadAsync(System.Byte[],System.Int32,System.Int32,System.Threading.CancellationToken) -T:System.NotSupportedException
    // ExceptionAdjustment: M:System.IO.Stream.WriteAsync(System.Byte[],System.Int32,System.Int32) -T:System.NotSupportedException
    // ExceptionAdjustment: M:System.IO.Stream.WriteAsync(System.Byte[],System.Int32,System.Int32,System.Threading.CancellationToken) -T:System.NotSupportedException
    /// <exception cref="SocketException"></exception>
    /// <exception cref="IOException"></exception>
    /// <exception cref="JsonReaderException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task<T> Send<T>(CommandFamily commandFamily, string methodName, object? parameters = null) {
        AssertConnected();
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

    internal virtual Stream GetNetworkStream() => _tcpClient.GetStream();

    protected internal static byte[] Serialize(JToken request, long requestId) {
        string requestJson  = request.ToString(Formatting.None);
        byte[] requestBytes = new byte[HeaderLength + Encoding.GetByteCount(requestJson)];
        Array.Copy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(requestBytes.Length - HeaderLength)), requestBytes, HeaderLength);
        Encoding.GetBytes(requestJson, 0, requestJson.Length, requestBytes, HeaderLength);

        WireLogger.Trace("tcp-outgoing-{0} >> {1}", requestId, requestJson);
        return Cipher(requestBytes);
    }

    /// <exception cref="JsonReaderException"></exception>
    protected internal static T Deserialize<T>(IEnumerable<byte> responseBytes, long requestId, CommandFamily commandFamily, string methodName) {
        byte[] responseDeciphered = Decipher(responseBytes.ToArray());
        int    responseLength     = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(responseDeciphered, 0));
        string responseString     = Encoding.GetString(responseDeciphered, HeaderLength, responseLength);

        WireLogger.Trace("tcp-outgoing-{0} << {1}", requestId, responseString);
        try {
            return JObject.Parse(responseString)[commandFamily.ToJsonString()]![methodName]!.ToObject<T>(JsonSerializer)!;
        } catch (JsonReaderException e) {
            Logger.Error(e, "Failed to deserialize JSON to {0}: {1}", typeof(T), responseString);
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
    protected internal virtual void AssertConnected() {
        if (!Connected) {
            throw new InvalidOperationException("Not connected. Call Connect() first.");
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