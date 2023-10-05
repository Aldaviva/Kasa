using System.Net;
using System.Net.Sockets;
using System.Text;
using Kasa;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Test;

public class KasaClientTest {

    private readonly KasaClient _client = new TestableKasaClient("0.0.0.0"); //allows RetryConnect() to fail faster than if this is 127.0.0.1

    public KasaClientTest() {
        _client.Options.MaxAttempts = 1;
    }

    [Fact]
    public void Hostname() {
        _client.Hostname.Should().Be("0.0.0.0");
    }

    [Fact]
    public void Cipher() {
        byte[] cleartext = new UTF8Encoding(false).GetBytes(@"{""system"":{""get_sysinfo"":null}}");
        byte[] expected  = Convert.FromBase64String("0PKB+Iv/mvfV75S20bTAn+yV5o/hh+jK8J7rh+uW6w==");
        byte[] actual    = KasaClient.Cipher(cleartext);
        actual.Should().BeEquivalentTo(expected, Convert.ToBase64String(actual));
    }

    [Fact]
    public void Decipher() {
        byte[] ciphertext = new UTF8Encoding(false).GetBytes(@"{""system"":{""set_led_off"":{""err_code"":0}}}");
        byte[] expected   = Convert.FromBase64String("0FlRCgoHEQhPGEFZURYRKzMJATswCQBEGEFZRxcALTwMCwFHGApNAAA=");
        byte[] actual     = KasaClient.Decipher(ciphertext);
        actual.Should().BeEquivalentTo(expected, Convert.ToBase64String(actual));
    }

    [Fact]
    public void Serialize() {
        byte[] actual   = _client.Serialize(new JObject(new JProperty("system", new JObject(new JProperty("get_sysinfo", (object?) null)))), 1);
        byte[] expected = Convert.FromBase64String("0PKB+Iv/mvfV75S20bTAn+yV5o/hh+jK8J7rh+uW6w==");
        actual.Should().BeEquivalentTo(expected, Convert.ToBase64String(actual));
    }

    [Fact]
    public void Deserialize() {
        byte[]  responseBytes = Convert.FromBase64String("0PKB+Iv/mvfV75S2xaDUi+eC5rnWsNb0zrWX8oDyrc6hxaCCuIj1iPU=");
        JObject actual        = _client.Deserialize<JObject>(responseBytes, 1, new JObject(), CommandFamily.System, "set_led_off");
        actual.Should().BeEquivalentTo(JObject.Parse(@"{""err_code"":0}"));
    }

    [Fact]
    public void DeserializeInvalidJson() {
        byte[] responseBytes = { 0x00 };
        Action thrower       = () => _client.Deserialize<JObject>(responseBytes, 1, new JObject(), CommandFamily.System, "test");
        thrower.Should().Throw<ResponseParsingException>();
    }

    [Fact]
    public void DeserializeMissingFeature() {
        byte[] responseBytes = Convert.FromBase64String("0PKX+p/rjvze5J+92KrYh+SL74qokr+OooDll+W616TD4dv5lPuf6objw63CtpblkOCQ/43526bb");
        Action thrower       = () => _client.Deserialize<JObject>(responseBytes, 1, new JObject(), CommandFamily.EnergyMeter, "get_realtime");
        thrower.Should().Throw<FeatureUnavailable>();
    }

    [Fact]
    public void DeserializeInvalidArgument() {
        byte[] responseBytes = Convert.FromBase64String("0PKB4orvi/6S99XvlLbXs9eI+o/jhqSe5cei0KL9nvGV8NLoxfba+J3vncKv3LuZo4HohvCR/ZTw0LHDpNG82bfD4ZzhnA==");
        Action thrower       = () => _client.Deserialize<JObject>(responseBytes, 1, new JObject(), CommandFamily.Schedule, "add_rule");
        thrower.Should().Throw<ArgumentException>();
    }

    [Fact]
    public async Task Send() {
        Stream fakeStream = await _client.GetNetworkStream();

        A.CallTo(() => fakeStream.ReadAsync(A<byte[]>._, 0, 4, A<CancellationToken>._)).ReturnsLazily((byte[] destination, int offset, int length, CancellationToken _) => {
            Array.Copy(new byte[] { 0x00, 0x00, 0x00, 0x29 }, 0, destination, offset, length);
            return Task.FromResult(length);
        });

        byte[] responseAfterHeader = Convert.FromBase64String("0PKB+Iv/mvfV75S2xaDUi+eC5rnWsNb0zrWX8oDyrc6hxaCCuIj1iPU=");
        A.CallTo(() => fakeStream.ReadAsync(A<byte[]>._, 0, 0x29, A<CancellationToken>._)).ReturnsLazily((byte[] destination, int offset, int length, CancellationToken _) => {
            Array.Copy(responseAfterHeader, 0, destination, offset, length);
            return Task.FromResult(length);
        });

        JObject actual = await _client.Send<JObject>(CommandFamily.System, "set_led_off", new { off = 0 });

        byte[] expectedHeader  = { 0, 0, 0, 36 };
        byte[] expectedPayload = Convert.FromBase64String("0PKB+Iv/mvfV75S2xaDUi+eC5rnWsNb0zrWX+J742uDQrdCt");
        A.CallTo(() => fakeStream.WriteAsync(A<byte[]>.That.IsSameSequenceAs(expectedHeader), 0, expectedHeader.Length, A<CancellationToken>._)).MustHaveHappened().Then(
            A.CallTo(() => fakeStream.WriteAsync(A<byte[]>.That.IsSameSequenceAs(expectedPayload), 0, expectedPayload.Length, A<CancellationToken>._)).MustHaveHappened());

        actual.Should().BeEquivalentTo(JObject.Parse(@"{""err_code"":0}"));
    }

    [Fact]
    public async Task SendHeaderTooShort() {
        Stream fakeStream = await _client.GetNetworkStream();
        A.CallTo(() => fakeStream.ReadAsync(A<byte[]>._, 0, 4, A<CancellationToken>._)).ReturnsLazily((byte[] destination, int offset, int length, CancellationToken _) => {
            Array.Copy(new byte[] { 0x00, 0x00, 0x00 }, 0, destination, offset, 3);
            return Task.FromResult(3);
        });

        Func<Task> thrower = async () => await _client.Send<JObject>(CommandFamily.System, "set_led_off", new { off = 0 });
        await thrower.Should().ThrowAsync<NetworkException>();
    }

    [Fact]
    public async Task ReceiveMultiplePackets() {
        Stream fakeStream = await _client.GetNetworkStream();
        A.CallTo(() => fakeStream.ReadAsync(A<byte[]>._, 0, 4, A<CancellationToken>._)).ReturnsLazily((byte[] destination, int offset, int length, CancellationToken _) => {
            Array.Copy(new byte[] { 0, 0, 4, 12 }, 0, destination, offset, length);
            return Task.FromResult(length);
        });

        A.CallTo(() => fakeStream.ReadAsync(A<byte[]>._, 0, 1036, A<CancellationToken>._)).ReturnsLazily((byte[] destination, int offset, int length, CancellationToken _) => {
            byte[] responseChunk1 = Convert.FromBase64String(
                "0PKB4orvi/6S99XvlLbRtMCf7Zj0keLA+oGj0aTIrfKe94Tw0uizyOqD58X/3Z6mnqjrqJrb6d+bqJConKnq2O+r6t6ar+yo6dng1eKmhKiK5IXoja+Vt+SH74rum/eSsuCV+Zy+krDVu9q41LGTqZi0luGF5J2/hd7uwvLe78Py3u/D89/vsp68z7vSv9qF6pruzPbG6si71r/R88n4yfHE6Mq52LvP7dfmyuiN7I/72ePO/9Pxg+aW85LmxP7Psp7lx67K6NLwx/PF8LOHxvLH/sqLuv64isjww4LGhLKGsYm9/7r4vYuphafJqMWggriayarCp8O22r+fzbjUsZO/nfiW95X5nL6EtZm7zKjJsJKo88Pv3vLD797yw+/f88OespDjl/6T9qnGtsLg2urG5Jf6k/3f5dTg0ufL6Zr7mOzO9MToyq/Ordn7wezd8dOhxLTRsMTm3O2QvMfljOjK8NLmoJmvl9aToePQ5NPrqpLTkNXs3emv7qiYq5/b46CZqYunheuK54KgmrjriOCF4ZT4nb3vmvaTsZ2/2rTVt9u+nKaXu5nuiuuSsIrR4c380ODM/NDgzPzQ4L2Rs8C03bDViuWV4cP5yeXHtNmw3vzG98b0we3PvN2+yujS48/tiOmK/tzmy/rW9Ibjk/aX48H7yreb4MKrz+3X9cDwsoCyhsT0zYy+iLqMyvjM+Mn+x/OwiLGGx/a0hMD31fnbtdS53P7E5rXWvtu/yqbD47HEqM3vw+GE6ovpheDC+MjkxrHVtM3v1Y6+kqKOvpKijr+To4+/4s7sn+uC74rVusq+nKaWupjrhu+Bo5mhk6aKqNu62a2PtYSoiu+O7Zm7gaydsZPhhPSR8ISmnKyAotu+362PtYe3hbebudS71aHJ69HkyOqO75a0jryK99ugguuPrZe18beGv/m7gruDxfLA8bKFxIDDgLXwwfPD9Mz6vobC87eVuZv1lPmcvoSm9Zb+m/+K5oOj8YToja+DocSqy6nFoIK4iKSG8ZX0ja+Vzv/T4s7/0+LO/9Pizv+ijqzfq8KvypX6iv7c5tb62KvGr8Hj2ejd5cnrmPma7sz2xurIrcyv2/nD7t/z0aPGttOyxuTe75K+xeeO6sjy0ODS5tGToZWh4KLg2Ora49rt2Zup696bouShkaPmpeTW9Nj6lPWY/d/lx5T3n/qe64fiwpDliezO4sCly6rIpMHj2enF55D0lezO9K+esoOvnrKDr56yg6+ew+/Nvsqjzqv0m+ufvYe3m7nKp86ggriJvISoivmY+4+tl6eLqcytzrqYoo++krDCp9ey06eFv47zroKg1rPBstu02vjC8Nz+m/WU9pr/3efW+ti9zw==");
            Array.Copy(responseChunk1, 0, destination, offset, responseChunk1.Length);
            return Task.FromResult(responseChunk1.Length);
        });

        A.CallTo(() => fakeStream.ReadAsync(A<byte[]>._, 1024, 12, A<CancellationToken>._)).ReturnsLazily((byte[] destination, int offset, int length, CancellationToken _) => {
            byte[] responseChunk2 = Convert.FromBase64String("veKB7orvzffHuse6");
            Array.Copy(responseChunk2, 0, destination, offset, responseChunk2.Length);
            return Task.FromResult(responseChunk2.Length);
        });

        JObject actual = await _client.Send<JObject>(CommandFamily.Schedule, "get_rules");
        actual.Should().BeEquivalentTo(JObject.Parse(
            @"{""rule_list"":[{""id"":""C886CC2A26D38845C27DA4D5CDA0957D"",""name"":""Schedule Rule"",""enable"":1,""wday"":[0,0,1,1,1,0,0],""stime_opt"":0,""smin"":1185,""sact"":1,""eact"":-1,""repeat"":1},{""id"":""7465C4A4594A1DF2B83ADB64784BEBE6"",""name"":""Schedule Rule"",""enable"":1,""wday"":[0,1,1,1,1,0,0],""stime_opt"":0,""smin"":1425,""sact"":0,""eact"":-1,""repeat"":1},{""id"":""4F968AE2B3478A8ACE914FAF034D8C90"",""name"":""Schedule Rule"",""enable"":1,""wday"":[0,1,0,0,0,0,0],""stime_opt"":0,""smin"":1125,""sact"":1,""eact"":-1,""repeat"":1},{""id"":""50B224B09A2626F2441794C897A1B0D7"",""name"":""Schedule Rule"",""enable"":0,""wday"":[0,0,0,0,1,0,0],""stime_opt"":0,""smin"":825,""sact"":1,""eact"":-1,""repeat"":0,""year"":2022,""month"":5,""day"":26},{""id"":""DF19FB998F721C7ADCC5E120786D8D1D"",""name"":""Schedule Rule"",""enable"":0,""wday"":[1,1,1,1,1,1,1],""stime_opt"":0,""smin"":158,""sact"":0,""eact"":-1,""repeat"":1},{""id"":""0247B244ABB8209974B2B5E9FE02ECA2"",""name"":""Schedule Rule"",""enable"":0,""wday"":[1,1,1,1,1,1,1],""stime_opt"":0,""smin"":158,""sact"":0,""eact"":-1,""repeat"":1}],""version"":2,""enable"":1,""err_code"":0}"));
    }

    [Fact]
    public async Task EnsureConnected() {
        (TcpListener server, ushort _, Reference<TcpClient?> serverSocket, KasaClient kasaClient) = StartTestServer();

        await kasaClient.EnsureConnected();
        await Task.Delay(100);
        kasaClient.Connected.Should().BeTrue();
        serverSocket.Value.Should().NotBeNull();
        serverSocket.Value!.Client.Connected.Should().BeTrue();
        Stream networkStream = await kasaClient.GetNetworkStream();
        networkStream.Should().NotBeNull().And.BeOfType<NetworkStream>();

        kasaClient.Dispose();
        kasaClient.Connected.Should().BeFalse();

        serverSocket.Value.Close();
        server.Stop();
    }

    [Fact]
    public async Task Reconnect() {
        (TcpListener server, ushort _, Reference<TcpClient?> serverSocket, KasaClient kasaClient) = StartTestServer();

        await kasaClient.Connect();
        await Task.Delay(100);
        kasaClient.Connected.Should().BeTrue();
        serverSocket.Value.Should().NotBeNull();
        serverSocket.Value!.Client.Connected.Should().BeTrue();
        Stream networkStream = await kasaClient.GetNetworkStream();
        networkStream.Should().NotBeNull().And.BeOfType<NetworkStream>();

        // This doesn't actually exercise the Socket.Disconnect() call in EnsureConnected(). Disconnect() is required, because it fails in real-life scenarios, but I can't synthesize it with my in-process test TCP server. Maybe .NET's Socket implementation is always polite enough to send FIN on shutdown, but the Kasa TCP server just crashes itself on reboot?
        serverSocket.Value.Client.Close();

        await kasaClient.EnsureConnected();
        await Task.Delay(100);
        kasaClient.Connected.Should().BeTrue();

        kasaClient.Dispose();
        kasaClient.Connected.Should().BeFalse();

        serverSocket.Value.Close();
        server.Stop();
    }

    [Fact]
    public void ConnectFailsIfAlreadyDisposed() {
        KasaClient kasaClient = new("localhost");
        kasaClient.Dispose();
        Func<Task> connect = () => kasaClient.Connect();
        connect.Should().ThrowExactlyAsync<ObjectDisposedException>();
    }

    [Fact]
    public void AutoconnectFailsIfAlreadyDisposed() {
        KasaClient kasaClient = new("localhost");
        kasaClient.Dispose();
        Func<Task> thrower = () => kasaClient.EnsureConnected();
        thrower.Should().ThrowExactlyAsync<ObjectDisposedException>();
    }

    [Fact]
    public void EnsureConnectedFailsIfAlreadyDisposed() {
        KasaClient kasaClient = new("localhost");
        kasaClient.Dispose();
        Func<Task> thrower = () => kasaClient.EnsureConnected();
        thrower.Should().ThrowExactlyAsync<ObjectDisposedException>();
    }

    [Fact]
    public async Task Connect() {
        (TcpListener server, ushort _, Reference<TcpClient?> serverSocket, KasaClient kasaClient) = StartTestServer();
        kasaClient.Connected.Should().BeFalse();

        await kasaClient.Connect();
        await Task.Delay(100);
        kasaClient.Connected.Should().BeTrue();
        serverSocket.Value.Should().NotBeNull();
        serverSocket.Value!.Client.Connected.Should().BeTrue();
        Stream networkStream = await kasaClient.GetNetworkStream();
        networkStream.Should().NotBeNull().And.BeOfType<NetworkStream>();

        Func<Task> assertConnected = async () => await kasaClient.EnsureConnected();
        await assertConnected.Should().NotThrowAsync();

        Func<Task> connect = async () => await kasaClient.Connect();
        await connect.Should().NotThrowAsync();

        kasaClient.Dispose();
        kasaClient.Connected.Should().BeFalse();

        serverSocket.Value.Close();
        server.Stop();
    }

    [Fact]
    public async Task RetryConnect() {
        _client.Options = new Options { MaxAttempts = 2, RetryDelay = TimeSpan.Zero, SendTimeout = TimeSpan.Zero, ReceiveTimeout = TimeSpan.Zero };
        Func<Task> thrower = async () => await _client.Connect();
        await thrower.Should().ThrowAsync<NetworkException>();
    }

    [Fact]
    public async Task RetrySend() {
        KasaClient client = new("0.0.0.0") {
            Options = new Options { MaxAttempts = 2, RetryDelay = TimeSpan.Zero, SendTimeout = TimeSpan.Zero, ReceiveTimeout = TimeSpan.Zero }
        };
        Func<Task> thrower = async () => await client.Send<JObject>(CommandFamily.System, "test");
        await thrower.Should().ThrowAsync<NetworkException>();
    }

    private static (TcpListener server, ushort serverPort, Reference<TcpClient?> serverSocket, KasaClient kasaClient) StartTestServer(ushort? desiredPort = null) {
        TcpListener? server     = null;
        ushort?      serverPort = desiredPort;
        while (!server?.Server.IsBound ?? true) {
            serverPort ??= (ushort) Random.Shared.Next(1024, 65536);
            server     =   new TcpListener(IPAddress.Loopback, serverPort.Value);
            try {
                server.Start();
            } catch (SocketException e) {
                if (e.SocketErrorCode != SocketError.AccessDenied) { // already in use
                    throw;
                }

                serverPort = null;
            }
        }

        Reference<TcpClient?> tcpServerSocket = new();
        server!.AcceptTcpClientAsync().ContinueWith(task => tcpServerSocket.Value = task.Result);
        KasaClient kasaClient = new("localhost") { Port = serverPort!.Value };
        return (server, serverPort.Value, tcpServerSocket, kasaClient);
    }

    [Fact]
    public void IsRetryAllowed() {
        KasaClient.IsRetryAllowed(new ObjectDisposedException(null)).Should().BeFalse();
        KasaClient.IsRetryAllowed(new SocketException((int) SocketError.HostNotFound)).Should().BeFalse();
        KasaClient.IsRetryAllowed(new SocketException((int) SocketError.TimedOut)).Should().BeTrue();
        KasaClient.IsRetryAllowed(new SocketException((int) SocketError.ConnectionRefused)).Should().BeTrue();
        KasaClient.IsRetryAllowed(new IOException(null)).Should().BeTrue();
        KasaClient.IsRetryAllowed(new FeatureUnavailable("method", Feature.EnergyMeter, "host")).Should().BeFalse();
        KasaClient.IsRetryAllowed(new ResponseParsingException("method", "<invalid json>", typeof(JObject), "host", new JsonReaderException())).Should().BeFalse();
    }

    /// <summary>
    /// <see href="https://github.com/Aldaviva/Kasa/issues/15"/>
    /// </summary>
    [Fact]
    public async Task MultipleClientInstanceLiveness() {
        TestableKasaClient client1 = new("0.0.0.0");
        TestableKasaClient client2 = new("0.0.0.0");

        Stream stream1 = await client1.GetNetworkStream();
        Stream stream2 = await client2.GetNetworkStream();

        SemaphoreSlim blocker1 = new(1);
        A.CallTo(() => stream1.WriteAsync(A<byte[]>._, A<int>._, A<int>._, A<CancellationToken>._)).Invokes(() => blocker1.WaitAsync());

        A.CallTo(() => stream2.ReadAsync(A<byte[]>._, 0, 4, A<CancellationToken>._)).ReturnsLazily((byte[] destination, int offset, int length, CancellationToken _) => {
            Array.Copy(new byte[] { 0x00, 0x00, 0x00, 0x29 }, 0, destination, offset, length);
            return Task.FromResult(length);
        });
        byte[] responseAfterHeader = Convert.FromBase64String("0PKB+Iv/mvfV75S2xaDUi+eC5rnWsNb0zrWX8oDyrc6hxaCCuIj1iPU=");
        A.CallTo(() => stream2.ReadAsync(A<byte[]>._, 0, 0x29, A<CancellationToken>._)).ReturnsLazily((byte[] destination, int offset, int length, CancellationToken _) => {
            Array.Copy(responseAfterHeader, 0, destination, offset, length);
            return Task.FromResult(length);
        });

        ManualResetEventSlim finished2 = new();

        client1.Send<JObject>(CommandFamily.System, "set_led_off", new { off = 0 });
        client2.Send<JObject>(CommandFamily.System, "set_led_off", new { off = 0 }).ContinueWith(task => finished2.Set(), TaskContinuationOptions.OnlyOnRanToCompletion);

        finished2.Wait(2000).Should().BeTrue();
    }

}

internal class TestableKasaClient: KasaClient {

    private readonly Stream _networkStream = A.Fake<Stream>();

    public TestableKasaClient(string hostname): base(hostname) { }

    protected internal override Task EnsureConnected(bool forceReconnect = false) {
        // we're not actually connected during most tests, so don't throw
        return Task.CompletedTask;
    }

    internal override Task<Stream> GetNetworkStream() {
        return Task.FromResult(_networkStream);
    }

}

internal class Reference<T> {

    public T Value { get; set; } = default!;

}