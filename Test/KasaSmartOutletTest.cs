using FakeItEasy;
using FluentAssertions;
using Kasa;
using Newtonsoft.Json.Linq;

namespace Test;

public class KasaSmartOutletTest {

    private readonly IKasaClient     _client = A.Fake<IKasaClient>();
    private readonly KasaSmartOutlet _outlet;

    public KasaSmartOutletTest() {
        _outlet = new KasaSmartOutlet(_client);
    }

    [Fact]
    public void Hostname() {
        KasaSmartOutlet outlet = new("localhost");
        outlet.Hostname.Should().Be("localhost");
    }

    [Fact]
    public void Connect() {
        _outlet.Connect();
        A.CallTo(() => _client.Connect()).MustHaveHappened();
    }

    [Fact]
    public void Dispose() {
        _outlet.Dispose();
        A.CallTo(() => _client.Dispose()).MustHaveHappened();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task IsOutletOn(bool expected) {
        A.CallTo(() => _client.Send<SystemInfo>(CommandFamily.System, "get_sysinfo", null)).Returns(new SystemInfo {
            IsOutletOn = expected
        });

        bool actual = await _outlet.System.IsOutletOn();

        actual.Should().Be(expected);
    }

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 0)]
    public async Task TurnOutletOn(bool turnOn, int expected) {
        await _outlet.System.SetOutlet(turnOn);
        A.CallTo(() => _client.Send<JObject>(CommandFamily.System, "set_relay_state", An<object>.That.Matches(o => o.Should().BeEquivalentTo(new { state = expected }, "")))).MustHaveHappened();
    }

    [Fact]
    public async Task GetSystemInfo() {
        SystemInfo expected = new();
        A.CallTo(() => _client.Send<SystemInfo>(CommandFamily.System, "get_sysinfo", null)).Returns(expected);

        SystemInfo actual = await _outlet.System.GetSystemInfo();
        actual.Should().Be(expected);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task IsIndicatorLightOn(bool indicatorLightDisabled, bool expected) {
        A.CallTo(() => _client.Send<SystemInfo>(CommandFamily.System, "get_sysinfo", null)).Returns(new SystemInfo {
            IndicatorLightDisabled = indicatorLightDisabled
        });

        bool actual = await _outlet.System.IsIndicatorLightOn();

        actual.Should().Be(expected);
    }

    [Theory]
    [InlineData(true, 0)]
    [InlineData(false, 1)]
    public async Task TurnIndicatorLightOn(bool turnOn, int expected) {
        await _outlet.System.SetIndicatorLight(turnOn);
        A.CallTo(() => _client.Send<JObject>(CommandFamily.System, "set_led_off", An<object>.That.HasProperty("off", expected))).MustHaveHappened();
    }

    [Fact]
    public async Task Reboot() {
        await _outlet.System.Reboot(TimeSpan.FromSeconds(5));
        A.CallTo(() => _client.Send<JObject>(CommandFamily.System, "reboot", An<object>.That.HasProperty("delay", 5))).MustHaveHappened();
    }

    [Fact]
    public async Task SetName() {
        await _outlet.System.SetName("abc");
        A.CallTo(() => _client.Send<JObject>(CommandFamily.System, "set_dev_alias", An<object>.That.HasProperty("alias", "abc"))).MustHaveHappened();
    }

    [Fact]
    public async Task GetTime() {
        JObject json = JObject.Parse(@"{""year"":2022,""month"":5,""mday"":29,""hour"":22,""min"":33,""sec"":22}");
        A.CallTo(() => _client.Send<JObject>(CommandFamily.Time, "get_time", null)).Returns(json);

        DateTime actual = await _outlet.Time.GetTime();
        actual.Should().Be(new DateTime(2022, 5, 29, 22, 33, 22));
    }

}