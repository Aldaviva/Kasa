using FakeItEasy;
using FluentAssertions;
using Kasa;
using Newtonsoft.Json.Linq;

namespace Test;

public class KasaOutletScheduleTest: AbstractKasaOutletTest {

    [Fact]
    public async Task GetAll() {
        JObject json = JObject.Parse(
            @"{""rule_list"":[{""id"":""FDC476554A5FC2936AD6EF14E6950DF9"",""name"":""Schedule Rule"",""enable"":1,""wday"":[1,1,0,0,0,0,0],""stime_opt"":0,""smin"":195,""sact"":1,""eact"":-1,""repeat"":1},{""id"":""901F46B4FFC33957D2A5383F2F83BDE0"",""name"":""Schedule Rule"",""enable"":1,""wday"":[0,0,1,1,0,0,0],""stime_opt"":1,""smin"":356,""soffset"":5,""sact"":0,""eact"":-1,""repeat"":1},{""id"":""13B98EDB80160333AF59C23B73036378"",""name"":""Schedule Rule"",""enable"":0,""wday"":[0,0,0,0,1,1,1],""stime_opt"":2,""smin"":1222,""soffset"":-10,""sact"":1,""eact"":-1,""repeat"":1}],""version"":2,""enable"":1,""err_code"":0}");
        A.CallTo(() => Client.Send<JObject>(CommandFamily.Schedule, "get_rules", null, null)).Returns(json);

        IList<Schedule> actual = (await Outlet.Schedule.GetAll()).ToArray();
        actual[0].Id.Should().Be("FDC476554A5FC2936AD6EF14E6950DF9");
        actual[0].TimeBasis.Should().Be(Schedule.Basis.StartOfDay);
        actual[0].Time.Should().Be(TimeSpan.FromHours(3).Add(TimeSpan.FromMinutes(15)));
        actual[0].Date.Should().BeNull();
        actual[0].DaysOfWeek.Should().BeEquivalentTo(new HashSet<DayOfWeek> { DayOfWeek.Sunday, DayOfWeek.Monday });
        actual[0].IsEnabled.Should().BeTrue();
        actual[0].Name.Should().Be("Schedule Rule");
        actual[0].IsRecurring.Should().BeTrue();
        actual[0].WillSetOutletOn.Should().BeTrue();

        actual[1].Id.Should().Be("901F46B4FFC33957D2A5383F2F83BDE0");
        actual[1].TimeBasis.Should().Be(Schedule.Basis.Sunrise);
        actual[1].Time.Should().Be(TimeSpan.FromMinutes(5));
        actual[1].Date.Should().BeNull();
        actual[1].DaysOfWeek.Should().BeEquivalentTo(new HashSet<DayOfWeek> { DayOfWeek.Tuesday, DayOfWeek.Wednesday });
        actual[1].IsEnabled.Should().BeTrue();
        actual[1].Name.Should().Be("Schedule Rule");
        actual[1].IsRecurring.Should().BeTrue();
        actual[1].WillSetOutletOn.Should().BeFalse();

        actual[2].Id.Should().Be("13B98EDB80160333AF59C23B73036378");
        actual[2].TimeBasis.Should().Be(Schedule.Basis.Sunset);
        actual[2].Time.Should().Be(TimeSpan.FromMinutes(10).Negate());
        actual[2].Date.Should().BeNull();
        actual[2].DaysOfWeek.Should().BeEquivalentTo(new HashSet<DayOfWeek> { DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday });
        actual[2].IsEnabled.Should().BeFalse();
        actual[2].Name.Should().Be("Schedule Rule");
        actual[2].IsRecurring.Should().BeTrue();
        actual[2].WillSetOutletOn.Should().BeTrue();
    }

    [Fact]
    public async Task Insert() {
        A.CallTo(() => Client.Send<JObject>(CommandFamily.Schedule, "add_rule", A<Schedule>._, null)).Returns(JObject.Parse(@"{""id"":""FE25867C0F5B34E3B83552888CAC5668"",""err_code"":0}"));
        A.CallTo(() => Client.Send<JObject>(CommandFamily.Schedule, "get_rules", null, null)).Returns(JObject.Parse(
            @"{""rule_list"":[{""id"":""FE25867C0F5B34E3B83552888CAC5668"",""name"":""Schedule Rule"",""enable"":1,""wday"":[0,0,1,1,1,0,0],""stime_opt"":0,""smin"":1185,""sact"":1,""eact"":-1,""repeat"":1}],""version"":2,""enable"":1,""err_code"":0}"));

        Schedule actual = await Outlet.Schedule.Save(new Schedule(true, new[] { DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday }, new TimeOnly(7 + 12, 45)));
        actual.Id.Should().Be("FE25867C0F5B34E3B83552888CAC5668");
        actual.TimeBasis.Should().Be(Schedule.Basis.StartOfDay);
        actual.Time.Should().Be(TimeSpan.FromHours(12 + 7).Add(TimeSpan.FromMinutes(45)));
        actual.Date.Should().BeNull();
        actual.DaysOfWeek.Should().BeEquivalentTo(new HashSet<DayOfWeek> { DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday });
        actual.IsEnabled.Should().BeTrue();
        actual.Name.Should().Be("Schedule Rule");
        actual.IsRecurring.Should().BeTrue();
        actual.WillSetOutletOn.Should().BeTrue();
    }

    [Fact]
    public async Task Update() {
        A.CallTo(() => Client.Send<JObject>(CommandFamily.Schedule, "edit_rule", A<Schedule>._, null)).Returns(JObject.Parse(@"{""err_code"":0}"));
        A.CallTo(() => Client.Send<JObject>(CommandFamily.Schedule, "get_rules", null, null)).Returns(JObject.Parse(
            @"{""rule_list"":[{""id"":""FE25867C0F5B34E3B83552888CAC5668"",""name"":""Schedule Rule"",""enable"":0,""wday"":[0,0,1,1,1,0,0],""stime_opt"":0,""smin"":1185,""sact"":1,""eact"":-1,""repeat"":1}],""version"":2,""enable"":1,""err_code"":0}"));

        Schedule actual = await Outlet.Schedule.Save(new Schedule(true, new[] { DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday }, new TimeOnly(7 + 12, 45))
            { Id = "FE25867C0F5B34E3B83552888CAC5668" });
        actual.Id.Should().Be("FE25867C0F5B34E3B83552888CAC5668");
        actual.TimeBasis.Should().Be(Schedule.Basis.StartOfDay);
        actual.Time.Should().Be(TimeSpan.FromHours(12 + 7).Add(TimeSpan.FromMinutes(45)));
        actual.Date.Should().BeNull();
        actual.DaysOfWeek.Should().BeEquivalentTo(new HashSet<DayOfWeek> { DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday });
        actual.IsEnabled.Should().BeFalse();
        actual.Name.Should().Be("Schedule Rule");
        actual.IsRecurring.Should().BeTrue();
        actual.WillSetOutletOn.Should().BeTrue();
    }

    [Fact]
    public async Task Delete() {
        JObject json = JObject.Parse(@"{""err_code"":0}");
        A.CallTo(() => Client.Send<JObject>(CommandFamily.Schedule, "delete_rules", A<object>.That.HasProperty("id", "123"), null)).Returns(json);

        Schedule schedule = new() { Id = "123" };
        await Outlet.Schedule.Delete(schedule);
    }

    [Fact]
    public async Task DeleteRequiresId() {
        Schedule   schedule = new();
        Func<Task> thrower  = async () => await Outlet.Schedule.Delete(schedule);
        await thrower.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task DeleteById() {
        JObject json = JObject.Parse(@"{""err_code"":0}");
        A.CallTo(() => Client.Send<JObject>(CommandFamily.Schedule, "delete_rules", A<object>.That.HasProperty("id", "123"), null)).Returns(json);

        await Outlet.Schedule.Delete("123");
    }

    [Fact]
    public async Task DeleteAll() {
        JObject json = JObject.Parse(@"{""err_code"":0}");
        A.CallTo(() => Client.Send<JObject>(CommandFamily.Schedule, "delete_all_rules", null, null)).Returns(json);

        await Outlet.Schedule.DeleteAll();
    }

    [Fact]
    public void SunRelative() {
        Schedule schedule = new(true, new[] { DayOfWeek.Sunday }, Schedule.Basis.Sunset, TimeSpan.FromMinutes(5));
        schedule.IsRecurring.Should().BeTrue();
        schedule.TimeBasis.Should().Be(Schedule.Basis.Sunset);
        schedule.Time.Should().Be(TimeSpan.FromMinutes(5));
        schedule.WillSetOutletOn.Should().BeTrue();
        schedule.Date.Should().BeNull();
    }

    [Fact]
    public void NonRepeating() {
        IEnumerable<Schedule> schedules = new Schedule[] {
            new(true, Array.Empty<DayOfWeek>(), new TimeOnly(3, 52)) {
                IsRecurring = false,
                Date        = new DateOnly(2022, 7, 3)
            },
            new(true, new DateTime(2022, 7, 3, 3, 52, 0)),
            new(true, new DateOnly(2022, 7, 3), Schedule.Basis.StartOfDay, TimeSpan.FromHours(3).Add(TimeSpan.FromMinutes(52)))
        };
        foreach (Schedule schedule in schedules) {
            schedule.IsRecurring.Should().BeFalse();
            schedule.TimeBasis.Should().Be(Schedule.Basis.StartOfDay);
            schedule.Time.Should().Be(TimeSpan.FromHours(3).Add(TimeSpan.FromMinutes(52)));
            schedule.WillSetOutletOn.Should().BeTrue();
            schedule.Date.Should().Be(new DateOnly(2022, 7, 3));
        }
    }

}