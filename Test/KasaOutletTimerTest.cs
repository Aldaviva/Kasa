using FakeItEasy;
using FluentAssertions;
using Kasa;
using Newtonsoft.Json.Linq;
using Timer = Kasa.Timer;

namespace Test;

public class KasaOutletTimerTest: AbstractKasaOutletTest {

    [Fact]
    public async Task GetOne() {
        JObject json = JObject.Parse(@"{""rule_list"":[{""id"":""BD8AED3F853C175935F0A5BC24C454F4"",""name"":""test"",""enable"":1,""delay"":1800,""act"":1,""remain"":1800}],""err_code"":0}");
        A.CallTo(() => Client.Send<JObject>(CommandFamily.Timer, "get_rules", null)).Returns(json);

        Timer actual = (await Outlet.Timer.Get())!.Value;
        actual.Name.Should().Be("test");
        actual.IsEnabled.Should().BeTrue();
        actual.TotalDuration.Should().Be(TimeSpan.FromMinutes(30));
        actual.RemainingDuration.Should().Be(TimeSpan.FromMinutes(30));
        actual.WillSetOutletOn.Should().BeTrue();
    }

    [Fact]
    public async Task GetElapsed() {
        JObject json = JObject.Parse(@"{""rule_list"":[{""id"":""BD8AED3F853C175935F0A5BC24C454F4"",""name"":""test"",""enable"":0,""delay"":0,""act"":1,""remain"":0}],""err_code"":0}");
        A.CallTo(() => Client.Send<JObject>(CommandFamily.Timer, "get_rules", null)).Returns(json);

        Timer? actual = await Outlet.Timer.Get();
        actual.Should().BeNull();
    }

    [Fact]
    public async Task GetNone() {
        JObject json = JObject.Parse(@"{""rule_list"":[],""err_code"":0}");
        A.CallTo(() => Client.Send<JObject>(CommandFamily.Timer, "get_rules", null)).Returns(json);

        Timer? actual = await Outlet.Timer.Get();
        actual.Should().BeNull();
    }

    [Fact]
    public async Task Clear() {
        JObject json = JObject.Parse(@"{""err_code"":0}");
        A.CallTo(() => Client.Send<JObject>(CommandFamily.Timer, "delete_all_rules", null)).Returns(json);

        await Outlet.Timer.Clear();
    }

    [Fact]
    public async Task Set() {
        A.CallTo(() => Client.Send<JObject>(CommandFamily.Timer, "get_rules", null))
            .Returns(JObject.Parse(@"{""rule_list"":[{""id"":""BD8AED3F853C175935F0A5BC24C454F4"",""name"":""test"",""enable"":1,""delay"":1800,""act"":1,""remain"":1800}],""err_code"":0}"));

        Timer actual = await Outlet.Timer.Start(TimeSpan.FromMinutes(30), true);

        actual.Name.Should().Be("test");
        actual.IsEnabled.Should().BeTrue();
        actual.TotalDuration.Should().Be(TimeSpan.FromMinutes(30));
        actual.RemainingDuration.Should().Be(TimeSpan.FromMinutes(30));
        actual.WillSetOutletOn.Should().BeTrue();

        A.CallTo(() => Client.Send<JObject>(CommandFamily.Timer, "delete_all_rules", null)).MustHaveHappened()
            .Then(A.CallTo(() => Client.Send<JObject>(CommandFamily.Timer, "add_rule", new Timer(TimeSpan.FromMinutes(30), true))).MustHaveHappened())
            .Then(A.CallTo(() => Client.Send<JObject>(CommandFamily.Timer, "get_rules", null)).MustHaveHappened());
    }

}