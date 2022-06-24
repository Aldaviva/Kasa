using FakeItEasy;
using FluentAssertions;
using Kasa;
using Newtonsoft.Json.Linq;

namespace Test;

public class KasaOutletTimeTest: AbstractKasaOutletTest {

    [Fact]
    public async Task GetTime() {
        JObject json = JObject.Parse(@"{""year"":2022,""month"":5,""mday"":29,""hour"":22,""min"":33,""sec"":22}");
        A.CallTo(() => Client.Send<JObject>(CommandFamily.Time, "get_time", null)).Returns(json);

        DateTime actual = await Outlet.Time.GetTime();
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
        A.CallTo(() => Client.Send<JObject>(CommandFamily.Time, "get_timezone", null)).Returns(json);
        IEnumerable<TimeZoneInfo> actual = await Outlet.Time.GetTimeZones();
        actual.Should().Contain(info => info.Id == windowsZoneId);
    }

    [Fact]
    public async Task GetTimeZoneWithOffset() {
        A.CallTo(() => Client.Send<JObject>(CommandFamily.Time, "get_time", null)).Returns(JObject.Parse(@"{""year"":2022,""month"":5,""mday"":29,""hour"":22,""min"":33,""sec"":22}"));
        A.CallTo(() => Client.Send<JObject>(CommandFamily.Time, "get_timezone", null)).Returns(new JObject(new JProperty("index", 6)));

        DateTimeOffset actual = await Outlet.Time.GetTimeWithZoneOffset();

        actual.Should().Be(new DateTimeOffset(2022, 5, 29, 22, 33, 22, TimeSpan.FromHours(-7)));
    }

    [Theory]
    [InlineData(6, "Pacific Standard Time")]
    [InlineData(18, "Eastern Standard Time")]
    [InlineData(38, "UTC")]
    [InlineData(104, "New Zealand Standard Time")]
    [InlineData(75, "India Standard Time")]
    public async Task SetTimeZone(int kasaZoneIndex, string windowsZoneId) {
        await Outlet.Time.SetTimeZone(TimeZoneInfo.FindSystemTimeZoneById(windowsZoneId));
        A.CallTo(() => Client.Send<JObject>(CommandFamily.Time, "set_timezone", An<object>.That.HasProperty("index", kasaZoneIndex))).MustHaveHappened();
    }

    [Fact]
    public async Task HandlesAllPossibleWindowsTimeZones() {
        IEnumerable<TimeZoneInfo> allWindowsZones         = TimeZoneInfo.GetSystemTimeZones();
        ISet<string>              unsupportedWindowsZones = new HashSet<string> { "Magallanes Standard Time", "Mid-Atlantic Standard Time" };
        foreach (TimeZoneInfo windowsZone in allWindowsZones.Where(zone => !unsupportedWindowsZones.Contains(zone.Id))) {
            await Outlet.Time.SetTimeZone(windowsZone);
        }
    }

    [Theory]
    [InlineData("Magallanes Standard Time")]
    [InlineData("Mid-Atlantic Standard Time")]
    public async Task UnsupportedWindowsTimeZonesThrow(string zoneId) {
        Func<Task> setTimeZone = async () => await Outlet.Time.SetTimeZone(TimeZoneInfo.FindSystemTimeZoneById(zoneId));
        await setTimeZone.Should().ThrowAsync<TimeZoneNotFoundException>();
    }

    [Fact]
    public async Task UnsupportedKasaTimeZonesExcludedFromResult() {
        var                 map      = (Dictionary<int, IEnumerable<string>>) TimeZones.KasaIndicesToWindowsZoneIds;
        const int           kasaId   = 13;
        IEnumerable<string> oldValue = map[kasaId];
        map[kasaId] = new[] { "fake windows timezone ID" };

        JObject json = new(new JProperty("index", kasaId));
        A.CallTo(() => Client.Send<JObject>(CommandFamily.Time, "get_timezone", null)).Returns(json);

        IEnumerable<TimeZoneInfo> actual = await Outlet.Time.GetTimeZones();
        actual.Should().BeEmpty();
        map[kasaId] = oldValue;
    }

}