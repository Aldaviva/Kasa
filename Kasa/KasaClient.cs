using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
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
using Newtonsoft.Json.Serialization;

namespace Kasa;

internal class KasaClient: IKasaClient {

    private static readonly SemaphoreSlim TcpMutex = new(1);
    private static readonly Encoding      Encoding = new UTF8Encoding(false);

    internal static readonly JsonSerializerSettings JsonSettings = new() {
        ContractResolver = new DefaultContractResolver { NamingStrategy = new SnakeCaseNamingStrategy() },
        Converters = new JsonConverter[] {
            new MacAddressConverter(),
            new OperatingModeConverter(),
            new FeatureConverter(),
            new DaysOfWeekConverter()
        }
    };

    internal static readonly JsonSerializer JsonSerializer = JsonSerializer.Create(JsonSettings);

    private TcpClient           _tcpClient;
    private Options             _options   = new();
    private ILogger<KasaClient> _logger    = new NullLogger<KasaClient>();
    private long                _requestId = -1;
    private bool                _disposed;

    internal ushort Port = 9999;

    public string Hostname { get; }

    public Options Options {
        get => _options;
        set {
            _options = value;
            OnChangeOptions();
        }
    }

    public bool Connected => _tcpClient.Connected && _tcpClient.Client.Connected;

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

    /// <inheritdoc />
    // ExceptionAdjustment: M:System.Threading.SemaphoreSlim.Release -T:System.Threading.SemaphoreFullException
    public async Task<T> Send<T>(CommandFamily commandFamily, string methodName, object? parameters = null, string[]? childIds = null)
    {
        await TcpMutex.WaitAsync().ConfigureAwait(false); //only one TCP write operation may occur in parallel, which is a requirement of TcpClient
        try {
            Task<T> Attempt() => SendWithoutRetry<T>(commandFamily, methodName, parameters, childIds);

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
    private async Task<T> SendWithoutRetry<T>(CommandFamily commandFamily, string methodName, object? parameters, string[]? childIds = null)
    {
        /*
         * Send request
         */

        Stream tcpStream = await GetNetworkStream().ConfigureAwait(false);
        long requestId = Interlocked.Increment(ref _requestId);
        JObject request = BuildRequest(commandFamily, methodName, parameters, childIds);

        byte[] requestBytes = Serialize(request, requestId);
        byte[] headerBuffer = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(requestBytes.Length));

        await tcpStream.WriteAsync(headerBuffer, 0, headerBuffer.Length, CancellationToken.None).ConfigureAwait(false);
        await tcpStream.WriteAsync(requestBytes, 0, requestBytes.Length, CancellationToken.None).ConfigureAwait(false);

        /*
         * Receive response
         */

        headerBuffer = new byte[headerBuffer.Length];
        if (headerBuffer.Length != await tcpStream.ReadAsync(headerBuffer, 0, headerBuffer.Length, CancellationToken.None).ConfigureAwait(false)) {
            throw new IOException(
                $"Failed to read {headerBuffer.Length}-byte length header, actually read {await tcpStream.ReadAsync(headerBuffer, 0, headerBuffer.Length, CancellationToken.None).ConfigureAwait(false)} bytes");
        }

        int expectedPayloadLength = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(headerBuffer, 0));

        byte[] payloadBuffer         = new byte[expectedPayloadLength];
        int    payloadLengthReceived = 0;
        while (payloadLengthReceived < expectedPayloadLength) {
            // Kasa sends 1024-byte chunks
            int chunkLength = await tcpStream.ReadAsync(payloadBuffer, payloadLengthReceived, payloadBuffer.Length - payloadLengthReceived, CancellationToken.None).ConfigureAwait(false);
            payloadLengthReceived += chunkLength;
        }

        return Deserialize<T>(payloadBuffer, requestId, request, commandFamily, methodName);
    }

    private static JObject BuildRequest(CommandFamily commandFamily, string methodName, object? parameters, string[]? childIds = null)
    {
        JObject request = new JObject();

        try
        {
            if (childIds is null)
            {
                request = new
                (
                    new JProperty(
                        commandFamily.ToJsonString(),
                        new JObject
                        (
                            new JProperty(methodName, parameters is null ? null : JObject.FromObject(parameters, JsonSerializer))
                        )
                    )
                );
            }
            else
            {
                request = new
                (
                    new JProperty
                    (
                        "context", new JObject
                        (
                            new JProperty
                            (
                                "child_ids", JToken.FromObject(childIds, JsonSerializer)
                            )
                        )
                    ),
                    new JProperty(
                        commandFamily.ToJsonString(),
                        new JObject
                        (
                            new JProperty(methodName, parameters is null ? null : JObject.FromObject(parameters, JsonSerializer))
                        )
                    )
                );
            }
            // request.Add(
            //     new JProperty
            //     (
            //         "context", new JObject
            //         (
            //             new JProperty
            //             (
            //                 "child_ids", JToken.FromObject(childIds, JsonSerializer)
            //             )
            //         )
            //     )
            // );
        }
        catch (Exception ex)
        {
            var serializedObject1 = Newtonsoft.Json.JsonConvert.SerializeObject(request);
        }

        var serializedObject = Newtonsoft.Json.JsonConvert.SerializeObject(request);

        return request;
    }

    /// <exception cref="SocketException">The TCP socket failed to connect.</exception>
    internal virtual async Task<Stream> GetNetworkStream() {
        await EnsureConnected().ConfigureAwait(false);
        return _tcpClient.GetStream();
    }

    protected internal byte[] Serialize(JToken request, long requestId) {
        string requestJson  = request.ToString(Formatting.None);
        byte[] requestBytes = Encoding.GetBytes(requestJson);

        _logger.LogTrace("tcp-outgoing-{requestId} >> {requestJson}", requestId, requestJson);
        return Cipher(requestBytes);
    }

    /// <exception cref="ArgumentException">If the device returns <c>-3 invalid argument</c></exception>
    /// <exception cref="ArgumentOutOfRangeException">If a feature is unavailable but we have no mapping for it in <see cref="Feature"/>.</exception>
    /// <exception cref="FeatureUnavailable">If the device is missing a feature that is required to run the given method, such as running <c>EnergyMeter.GetInstantaneousPowerUsage()</c> on an EP10, which does not have the EnergyMeter Feature.</exception>
    /// <exception cref="ResponseParsingException">If the JSON response from the outlet cannot be deserialized into an object.</exception>
    protected internal T Deserialize<T>(byte[] responseBytes, long requestId, JObject request, CommandFamily commandFamily, string methodName) {
        byte[] responseDeciphered = Decipher(responseBytes);
        string responseString     = Encoding.GetString(responseDeciphered);
        string requestMethod      = $"{commandFamily.ToJsonString()}.{methodName}";

        _logger.LogTrace("tcp-outgoing-{requestId} << {responseString}", requestId, responseString);
        try {
            JToken innerResponse = JObject.Parse(responseString)[commandFamily.ToJsonString()]!;
            switch ((innerResponse["err_msg"] ?? innerResponse[methodName]?["err_msg"])?.Value<string>()) {
                case "module not support":
                    Feature requiredFeature = commandFamily.GetRequiredFeature();
                    throw new FeatureUnavailable(requestMethod, requiredFeature, Hostname);
                case "invalid argument":
                    throw new ArgumentException($"Invalid argument to {commandFamily}.{methodName} in request:\n" + request.ToString(Formatting.Indented));
                default:
                    return innerResponse[methodName]!.ToObject<T>(JsonSerializer)!;
            }
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
            outputBytes[i] = (byte) (inputBytes[i] ^ (i == 0 ? 171 : keyStream[i - 1]));
        }

        return outputBytes;
    }

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
            _tcpClient.Dispose();
            _tcpClient = new TcpClient(AddressFamily.InterNetwork);

            _logger.LogDebug("Connecting to Kasa device {host}:{port}", Hostname, Port);
            await _tcpClient.ConnectAsync(Hostname, Port).ConfigureAwait(false);
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