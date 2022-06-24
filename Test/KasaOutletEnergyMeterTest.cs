using FakeItEasy;
using FluentAssertions;
using Kasa;
using Newtonsoft.Json.Linq;

namespace Test;

public class KasaOutletEnergyMeterTest: AbstractKasaOutletTest {

    [Fact]
    public async Task GetInstantaneousPowerUsage() {
        PowerUsage expected = new() { Current = 18, Voltage = 121995, Power = 967, CumulativeEnergySinceBoot = 0 };
        A.CallTo(() => Client.Send<PowerUsage>(CommandFamily.EnergyMeter, "get_realtime", null)).Returns(expected);
        PowerUsage actual = await Outlet.EnergyMeter.GetInstantaneousPowerUsage();
        actual.Current.Should().Be(18);
        actual.Voltage.Should().Be(121995);
        actual.Power.Should().Be(967);
        actual.CumulativeEnergySinceBoot.Should().Be(0);
    }

    [Fact]
    public async Task GetDailyEnergyUsageOneDay() {
        JObject json = JObject.Parse(@"{""day_list"": [{""year"": 2022, ""month"": 5, ""day"": 31, ""energy_wh"": 6}]}");
        A.CallTo(() => Client.Send<JObject>(CommandFamily.EnergyMeter, "get_daystat", An<object>.That.Matches(o => o.Should().BeEquivalentTo(new { year = 2022, month = 5 }, "")))).Returns(json);
        IList<int>? actual = await Outlet.EnergyMeter.GetDailyEnergyUsage(2022, 5);
        actual.Should().HaveCount(31);
        actual.Should().BeEquivalentTo(new List<int> { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 6 });
    }

    [Fact]
    public async Task GetDailyEnergyUsageTwoDays() {
        JObject json = JObject.Parse(@"{""day_list"": [{""year"": 2022, ""month"": 6, ""day"": 1, ""energy_wh"": 22}, {""year"": 2022, ""month"": 6, ""day"": 2, ""energy_wh"": 1}]}");
        A.CallTo(() => Client.Send<JObject>(CommandFamily.EnergyMeter, "get_daystat", An<object>.That.Matches(o => o.Should().BeEquivalentTo(new { year = 2022, month = 6 }, "")))).Returns(json);
        IList<int>? actual = await Outlet.EnergyMeter.GetDailyEnergyUsage(2022, 6);
        actual.Should().HaveCount(30);
        actual.Should().BeEquivalentTo(new List<int> { 22, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 });
    }

    [Fact]
    public async Task GetDailyEnergyUsageNotFound() {
        JObject json = JObject.Parse(@"{""day_list"": []}");
        A.CallTo(() => Client.Send<JObject>(CommandFamily.EnergyMeter, "get_daystat", An<object>.That.Matches(o => o.Should().BeEquivalentTo(new { year = 2022, month = 4 }, "")))).Returns(json);
        IList<int>? actual = await Outlet.EnergyMeter.GetDailyEnergyUsage(2022, 4);
        actual.Should().BeNull();
    }

    [Fact]
    public async Task GetMonthlyEnergyUsage() {
        JObject json = JObject.Parse(@"{""month_list"": [{""year"": 2022, ""month"": 5, ""energy_wh"": 6}, {""year"": 2022, ""month"": 6, ""energy_wh"": 18}]}");
        A.CallTo(() => Client.Send<JObject>(CommandFamily.EnergyMeter, "get_monthstat", An<object>.That.HasProperty("year", 2022))).Returns(json);
        IList<int>? actual = await Outlet.EnergyMeter.GetMonthlyEnergyUsage(2022);
        actual.Should().BeEquivalentTo(new List<int> { 0, 0, 0, 0, 6, 18, 0, 0, 0, 0, 0, 0 });
    }

    [Fact]
    public async Task GetMonthlyEnergyUsageNotFound() {
        JObject json = JObject.Parse(@"{""month_list"": []}");
        A.CallTo(() => Client.Send<JObject>(CommandFamily.EnergyMeter, "get_monthstat", An<object>.That.HasProperty("year", 2021))).Returns(json);
        IList<int>? actual = await Outlet.EnergyMeter.GetMonthlyEnergyUsage(2021);
        actual.Should().BeNull();
    }

    [Fact]
    public async Task ClearHistoricalUsage() {
        await Outlet.EnergyMeter.DeleteHistoricalUsage();
        A.CallTo(() => Client.Send<JObject>(CommandFamily.EnergyMeter, "erase_emeter_stat", null)).MustHaveHappened();
    }

}