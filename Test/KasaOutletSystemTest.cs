using FakeItEasy;
using FluentAssertions;
using Kasa;
using Newtonsoft.Json.Linq;

namespace Test;

public class KasaOutletSystemTest: AbstractKasaOutletTest {

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task IsOutletOn(bool expected) {
        A.CallTo(() => Client.Send<SystemInfo>(CommandFamily.System, "get_sysinfo", null, null)).Returns(new SystemInfo {
            IsOutletOn = expected
        });

        bool actual = await Outlet.System.IsOutletOn();

        actual.Should().Be(expected);
    }

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 0)]
    public async Task TurnOutletOn(bool turnOn, int expected) {
        await Outlet.System.SetOutletOn(turnOn);
        A.CallTo(() => Client.Send<JObject>(CommandFamily.System, "set_relay_state", An<object>.That.Matches(o => o.Should().BeEquivalentTo(new { state = expected }, "")), null)).MustHaveHappened();
    }

    [Fact]
    public async Task GetSystemInfo() {
        SystemInfo expected = new();
        A.CallTo(() => Client.Send<SystemInfo>(CommandFamily.System, "get_sysinfo", null, null)).Returns(expected);

        SystemInfo actual = await Outlet.System.GetInfo();
        actual.Should().Be(expected);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task IsIndicatorLightOn(bool indicatorLightDisabled, bool expected) {
        A.CallTo(() => Client.Send<SystemInfo>(CommandFamily.System, "get_sysinfo", null, null)).Returns(new SystemInfo {
            IndicatorLightDisabled = indicatorLightDisabled
        });

        bool actual = await Outlet.System.IsIndicatorLightOn();

        actual.Should().Be(expected);
    }

    [Theory]
    [InlineData(true, 0)]
    [InlineData(false, 1)]
    public async Task TurnIndicatorLightOn(bool turnOn, int expected) {
        await Outlet.System.SetIndicatorLightOn(turnOn);
        A.CallTo(() => Client.Send<JObject>(CommandFamily.System, "set_led_off", An<object>.That.HasProperty("off", expected), null)).MustHaveHappened();
    }

    [Fact]
    public async Task Reboot() {
        await Outlet.System.Reboot(TimeSpan.FromSeconds(5));
        A.CallTo(() => Client.Send<JObject>(CommandFamily.System, "reboot", An<object>.That.HasProperty("delay", 5), null)).MustHaveHappened();
    }

    [Fact]
    public async Task GetName() {
        A.CallTo(() => Client.Send<SystemInfo>(CommandFamily.System, "get_sysinfo", null, null)).Returns(new SystemInfo {
            Name = "SX20"
        });

        string actual = await Outlet.System.GetName();

        actual.Should().Be("SX20");
    }

    [Fact]
    public async Task SetName() {
        await Outlet.System.SetName("abc");
        A.CallTo(() => Client.Send<JObject>(CommandFamily.System, "set_dev_alias", An<object>.That.HasProperty("alias", "abc"), null)).MustHaveHappened();
    }

    [Theory]
    [InlineData("")]
    [InlineData("0123456789012345678901234567890123456789")]
    [InlineData(" ")]
    public async Task SetNameInvalid(string name) {
        Func<Task> setName = async () => await Outlet.System.SetName(name);
        await setName.Should().ThrowAsync<ArgumentOutOfRangeException>();
        A.CallTo(() => Client.Send<JObject>(CommandFamily.System, "set_dev_alias", An<object>._, null)).MustNotHaveHappened();
    }

}