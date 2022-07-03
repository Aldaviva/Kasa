using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

// ReSharper disable PropertyCanBeMadeInitOnly.Local - not available in .NET Standard 2.0
// ReSharper disable PropertyCanBeMadeInitOnly.Global - not available in .NET Standard 2.0

namespace Kasa;

public struct Schedule {

    [JsonProperty("enable")] public bool IsEnabled { get; set; } = true;
    [JsonProperty("id")] public string? Id { get; set; } = null;
    public string Name { get; set; } = "Schedule Rule";
    public bool Repeat { get; set; }
    [JsonProperty("sact")] public bool WillSetOutletOn { get; set; }
    [JsonProperty("stime_opt")] public Basis StartTimeBasis { get; set; } = Basis.StartOfDay;
    [JsonProperty("wday")] public ISet<DayOfWeek> DaysOfWeek { get; set; }

    [JsonIgnore] public DateOnly? Date {
        get => Year > 0 && Month > 0 && Day > 0 ? new DateOnly(Year, Month, Day) : null;
        set {
            Year  = value?.Year ?? 0;
            Month = value?.Month ?? 0;
            Day   = value?.Day ?? 0;
        }
    }

    [JsonIgnore]
    public TimeSpan TimeSinceBasis {
        get => TimeSpan.FromMinutes(StartTimeBasis == Basis.StartOfDay ? MinutesSinceStartOfDay : MinutesSinceSunriseOrSunset);
        private set {
            int minutes = (int) value.TotalMinutes;
            if (StartTimeBasis == Basis.StartOfDay) {
                MinutesSinceStartOfDay = minutes;
            } else {
                MinutesSinceSunriseOrSunset = minutes;
            }
        }
    }

    [JsonProperty] private int Year { get; set; } = 0;
    [JsonProperty] private int Month { get; set; } = 0;
    [JsonProperty] private int Day { get; set; } = 0;
    [JsonProperty("smin")] private int MinutesSinceStartOfDay { get; set; } = 0;
    [JsonProperty("soffset")] private int MinutesSinceSunriseOrSunset { get; set; } = 0;

// Remove unused private members - required to avoid "invalid argument" errors
#pragma warning disable IDE0051,CS0414
    [JsonProperty("etime_opt")] private readonly int _unused  = -1; // required on create (-1 on create, 0 on list)
    [JsonProperty("eact")]      private readonly int _unused2 = -1; // required on update
    [JsonProperty("emin")]      private readonly int _unused3 = 0;  // required on update
#pragma warning restore IDE0051,CS0414

    public Schedule(bool willSetOutletOn, IEnumerable<DayOfWeek>? weeklyRepetition, TimeOnly timeOfDay) {
        DaysOfWeek      = new HashSet<DayOfWeek>(weeklyRepetition ?? Enumerable.Empty<DayOfWeek>());
        Repeat          = DaysOfWeek.Any();
        WillSetOutletOn = willSetOutletOn;
        StartTimeBasis  = Basis.StartOfDay;
        TimeSinceBasis  = timeOfDay.ToTimeSpan();
    }

    public Schedule(bool willSetOutletOn, IEnumerable<DayOfWeek>? weeklyRepetition, Basis timeBasis, TimeSpan timeSinceBasis) {
        DaysOfWeek      = new HashSet<DayOfWeek>(weeklyRepetition ?? Enumerable.Empty<DayOfWeek>());
        Repeat          = DaysOfWeek.Any();
        WillSetOutletOn = willSetOutletOn;
        StartTimeBasis  = timeBasis;
        TimeSinceBasis  = timeSinceBasis;
    }

    public Schedule(bool willSetOutletOn, DateTime singleOccurrence) {
        DaysOfWeek      = new HashSet<DayOfWeek>();
        Repeat          = false;
        WillSetOutletOn = willSetOutletOn;
        StartTimeBasis  = Basis.StartOfDay;
        Date            = DateOnly.FromDateTime(singleOccurrence);
        TimeSinceBasis  = singleOccurrence.TimeOfDay;
    }

    public Schedule(bool willSetOutletOn, DateOnly singleOccurrence, Basis timeBasis, TimeSpan timeSinceBasis) {
        DaysOfWeek      = new HashSet<DayOfWeek>();
        Repeat          = false;
        WillSetOutletOn = willSetOutletOn;
        StartTimeBasis  = timeBasis;
        Date            = singleOccurrence;
        TimeSinceBasis  = timeSinceBasis;
    }

    public enum Basis {

        StartOfDay = 0,
        Sunrise    = 1,
        Sunset     = 2

    }

}