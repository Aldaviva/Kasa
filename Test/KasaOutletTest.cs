using FakeItEasy;
using FluentAssertions;
using Kasa;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Test;

public class KasaOutletTest {

    private readonly IKasaClient _client = A.Fake<IKasaClient>();
    private readonly KasaOutlet  _outlet;

    public KasaOutletTest() {
        _outlet = new KasaOutlet(_client);
    }

    [Fact]
    public void Hostname() {
        KasaOutlet outlet = new("localhost");
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
        await _outlet.System.SetOutletOn(turnOn);
        A.CallTo(() => _client.Send<JObject>(CommandFamily.System, "set_relay_state", An<object>.That.Matches(o => o.Should().BeEquivalentTo(new { state = expected }, "")))).MustHaveHappened();
    }

    [Fact]
    public async Task GetSystemInfo() {
        SystemInfo expected = new();
        A.CallTo(() => _client.Send<SystemInfo>(CommandFamily.System, "get_sysinfo", null)).Returns(expected);

        SystemInfo actual = await _outlet.System.GetInfo();
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
        await _outlet.System.SetIndicatorLightOn(turnOn);
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

    [Theory]
    [InlineData("")]
    [InlineData("0123456789012345678901234567890123456789")]
    [InlineData(" ")]
    public async Task SetNameInvalid(string name) {
        Func<Task> setName = async () => await _outlet.System.SetName(name);
        await setName.Should().ThrowAsync<ArgumentOutOfRangeException>();
        A.CallTo(() => _client.Send<JObject>(CommandFamily.System, "set_dev_alias", An<object>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task GetTime() {
        JObject json = JObject.Parse(@"{""year"":2022,""month"":5,""mday"":29,""hour"":22,""min"":33,""sec"":22}");
        A.CallTo(() => _client.Send<JObject>(CommandFamily.Time, "get_time", null)).Returns(json);

        DateTime actual = await _outlet.Time.GetTime();
        actual.Should().Be(new DateTime(2022, 5, 29, 22, 33, 22));
    }

    [Theory]
    [InlineData(6, "Pacific Standard Time")]
    [InlineData(18, "Eastern Standard Time")]
    [InlineData(38, "UTC")]
    [InlineData(104, "New Zealand Standard Time")]
    [InlineData(75, "India Standard Time")]
    public async Task GetTimeZones(int kasaZoneIndex, string windowsZoneId) {
        JObject json = new(new JProperty("index", kasaZoneIndex));
        A.CallTo(() => _client.Send<JObject>(CommandFamily.Time, "get_timezone", null)).Returns(json);
        IEnumerable<TimeZoneInfo> actual = await _outlet.Time.GetTimeZones();
        actual.Should().Contain(info => info.Id == windowsZoneId);
    }

    [Theory]
    [InlineData(6, "Pacific Standard Time")]
    [InlineData(18, "Eastern Standard Time")]
    [InlineData(38, "UTC")]
    [InlineData(104, "New Zealand Standard Time")]
    [InlineData(75, "India Standard Time")]
    public async Task SetTimeZone(int kasaZoneIndex, string windowsZoneId) {
        await _outlet.Time.SetTimeZone(TimeZoneInfo.FindSystemTimeZoneById(windowsZoneId));
        A.CallTo(() => _client.Send<JObject>(CommandFamily.Time, "set_timezone", An<object>.That.HasProperty("index", kasaZoneIndex))).MustHaveHappened();
    }

    [Fact]
    public async Task HandlesAllPossibleWindowsTimeZones() {
        IEnumerable<TimeZoneInfo> allWindowsZones         = TimeZoneInfo.GetSystemTimeZones();
        ISet<string>              unsupportedWindowsZones = new HashSet<string> { "Magallanes Standard Time", "Mid-Atlantic Standard Time" };
        foreach (TimeZoneInfo windowsZone in allWindowsZones.Where(zone => !unsupportedWindowsZones.Contains(zone.Id))) {
            await _outlet.Time.SetTimeZone(windowsZone);
        }
    }

    [Theory]
    [InlineData("Magallanes Standard Time")]
    [InlineData("Mid-Atlantic Standard Time")]
    public async Task UnsupportedWindowsTimeZonesThrow(string zoneId) {
        Func<Task> setTimeZone = async () => await _outlet.Time.SetTimeZone(TimeZoneInfo.FindSystemTimeZoneById(zoneId));
        await setTimeZone.Should().ThrowAsync<TimeZoneNotFoundException>();
    }

    [Fact]
    public async Task UnsupportedKasaTimeZonesExcludedFromResult() {
        var                 map      = (Dictionary<int, IEnumerable<string>>) TimeZones.KasaIndicesToWindowsZoneIds;
        const int           kasaId   = 13;
        IEnumerable<string> oldValue = map[kasaId];
        map[kasaId] = new[] { "fake windows timezone ID" };

        JObject json = new(new JProperty("index", kasaId));
        A.CallTo(() => _client.Send<JObject>(CommandFamily.Time, "get_timezone", null)).Returns(json);

        IEnumerable<TimeZoneInfo> actual = await _outlet.Time.GetTimeZones();
        actual.Should().BeEmpty();
        map[kasaId] = oldValue;
    }

    [Fact]
    public async Task GetInstantaneousPowerUsage() {
        PowerUsage expected = new() { Current = 18, Voltage = 121995, Power = 967, CumulativeEnergySinceBoot = 0 };
        A.CallTo(() => _client.Send<PowerUsage>(CommandFamily.EnergyMeter, "get_realtime", null)).Returns(expected);
        PowerUsage actual = await _outlet.EnergyMeter.GetInstantaneousPowerUsage();
        actual.Current.Should().Be(18);
        actual.Voltage.Should().Be(121995);
        actual.Power.Should().Be(967);
        actual.CumulativeEnergySinceBoot.Should().Be(0);
    }

    [Fact]
    public async Task GetDailyEnergyUsageOneDay() {
        JObject json = JObject.Parse(@"{""day_list"": [{""year"": 2022, ""month"": 5, ""day"": 31, ""energy_wh"": 6}]}");
        A.CallTo(() => _client.Send<JObject>(CommandFamily.EnergyMeter, "get_daystat", An<object>.That.Matches(o => o.Should().BeEquivalentTo(new { year = 2022, month = 5 }, "")))).Returns(json);
        IList<int>? actual = await _outlet.EnergyMeter.GetDailyEnergyUsage(2022, 5);
        actual.Should().HaveCount(31);
        actual.Should().BeEquivalentTo(new List<int> { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 6 });
    }

    [Fact]
    public async Task GetDailyEnergyUsageTwoDays() {
        JObject json = JObject.Parse(@"{""day_list"": [{""year"": 2022, ""month"": 6, ""day"": 1, ""energy_wh"": 22}, {""year"": 2022, ""month"": 6, ""day"": 2, ""energy_wh"": 1}]}");
        A.CallTo(() => _client.Send<JObject>(CommandFamily.EnergyMeter, "get_daystat", An<object>.That.Matches(o => o.Should().BeEquivalentTo(new { year = 2022, month = 6 }, "")))).Returns(json);
        IList<int>? actual = await _outlet.EnergyMeter.GetDailyEnergyUsage(2022, 6);
        actual.Should().HaveCount(30);
        actual.Should().BeEquivalentTo(new List<int> { 22, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 });
    }

    [Fact]
    public async Task GetDailyEnergyUsageNotFound() {
        JObject json = JObject.Parse(@"{""day_list"": []}");
        A.CallTo(() => _client.Send<JObject>(CommandFamily.EnergyMeter, "get_daystat", An<object>.That.Matches(o => o.Should().BeEquivalentTo(new { year = 2022, month = 4 }, "")))).Returns(json);
        IList<int>? actual = await _outlet.EnergyMeter.GetDailyEnergyUsage(2022, 4);
        actual.Should().BeNull();
    }

    [Fact]
    public async Task GetMonthlyEnergyUsage() {
        JObject json = JObject.Parse(@"{""month_list"": [{""year"": 2022, ""month"": 5, ""energy_wh"": 6}, {""year"": 2022, ""month"": 6, ""energy_wh"": 18}]}");
        A.CallTo(() => _client.Send<JObject>(CommandFamily.EnergyMeter, "get_monthstat", An<object>.That.HasProperty("year", 2022))).Returns(json);
        IList<int>? actual = await _outlet.EnergyMeter.GetMonthlyEnergyUsage(2022);
        actual.Should().BeEquivalentTo(new List<int> { 0, 0, 0, 0, 6, 18, 0, 0, 0, 0, 0, 0 });
    }

    [Fact]
    public async Task GetMonthlyEnergyUsageNotFound() {
        JObject json = JObject.Parse(@"{""month_list"": []}");
        A.CallTo(() => _client.Send<JObject>(CommandFamily.EnergyMeter, "get_monthstat", An<object>.That.HasProperty("year", 2021))).Returns(json);
        IList<int>? actual = await _outlet.EnergyMeter.GetMonthlyEnergyUsage(2021);
        actual.Should().BeNull();
    }

    [Fact]
    public async Task ClearHistoricalUsage() {
        await _outlet.EnergyMeter.DeleteHistoricalUsage();
        A.CallTo(() => _client.Send<JObject>(CommandFamily.EnergyMeter, "erase_emeter_stat", null)).MustHaveHappened();
    }

    [Fact]
    public void Logging() {
        _outlet.LoggerFactory.Should().BeSameAs(_client.LoggerFactory);
        ILoggerFactory loggerFactory = A.Fake<ILoggerFactory>();

        _outlet.LoggerFactory = loggerFactory;

        _outlet.LoggerFactory.Should().BeSameAs(loggerFactory);
        A.CallToSet(() => _client.LoggerFactory).To(loggerFactory).MustHaveHappened();
        _outlet.LoggerFactory.Should().BeSameAs(_client.LoggerFactory);
    }

}