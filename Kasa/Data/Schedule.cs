using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

// ReSharper disable PropertyCanBeMadeInitOnly.Local - not available in .NET Standard 2.0
// ReSharper disable PropertyCanBeMadeInitOnly.Global - not available in .NET Standard 2.0

namespace Kasa;

/// <summary>
/// <para>A time-of-day–based rule for use with the <see cref="IKasaOutlet.Schedule"/> family of commands.</para>
/// <para>These schedules can turn the outlet on or off at a specified time of day, with optional weekly recurrence, and can optionally be relative to sunrise and sunset instead of midnight.</para>
/// </summary>
public struct Schedule {

    /// <summary>
    /// <para>Whether the schedule is running (<c>true</c>) or paused (<c>false</c>).</para>
    /// </summary>
    [JsonProperty("enable")] public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// <para>Unique identifier for this schedule rule.</para>
    /// <para>This value will be <c>null</c> for Schedules that are newly created, before they are saved. After they are saved, the returned copy of the Schedule will have this property filled in with a non-null value.</para>
    /// <para>The format of this string is 32 characters of uppercase hexadecimal digits, for example, <c>C886CC2A26D38845C27DA4D5CDA0957D</c>.</para>
    /// </summary>
    [JsonProperty("id")] public string? Id { get; set; } = null;

    /// <summary>
    /// <para>The optional name of the schedule.</para>
    /// <para>Only visible in the outlet's API, not the Kasa Android app.</para>
    /// <para>The Android app always sets this value to <c>Schedule Rule</c>, which is also the default for this library.</para>
    /// </summary>
    public string Name { get; set; } = "Schedule Rule";

    /// <summary>
    /// <para><c>true</c> if this schedule recurs on a weekly basis, or <c>false</c> if it will only run once on the given <see cref="Date"/>.</para>
    /// <para>Recurring schedules must specify the days of the week for the weekly recurrence using the <see cref="DaysOfWeek"/> property. When enabled, the recurrence is every 1 week; there is no week interval option to allow a schedule to run, for example, every 2 weeks.</para>
    /// </summary>
    [JsonProperty("repeat")] public bool IsRecurring { get; set; }

    /// <summary>
    /// Whether to turn the outlet on (<c>true</c>) or off (<c>false</c>) when the schedule occurs.
    /// </summary>
    [JsonProperty("sact")] public bool WillSetOutletOn { get; set; }

    /// <summary>
    /// Whether the given <see cref="Time"/> value is relative to the start of the day (midnight), sunrise, or sunset.
    /// </summary>
    [JsonProperty("stime_opt")] public Basis TimeBasis { get; set; } = Basis.StartOfDay;

    /// <summary>
    /// <para>When <see cref="IsRecurring"/> is <c>true</c>, controls the days of the week on which the schedule recurs, or the empty set for non-recurring schedules.</para>
    /// </summary>
    [JsonProperty("wday")] public ISet<DayOfWeek> DaysOfWeek { get; set; }

    /// <summary>
    /// <para>When <see cref="IsRecurring"/> is <c>false</c>, controls the date on which the schedule will occur once, or <c>null</c> for recurring schedules.</para>
    /// </summary>
    [JsonIgnore] public DateOnly? Date {
        get => Year > 0 && Month > 0 && Day > 0 ? new DateOnly(Year, Month, Day) : null;
        set {
            Year  = value?.Year ?? 0;
            Month = value?.Month ?? 0;
            Day   = value?.Day ?? 0;
        }
    }

    /// <summary>
    /// <para>The time of day at which the schedule will occur.</para>
    /// <para>If <see cref="TimeBasis"/> is <see cref="Basis.StartOfDay"/>, this is the period after midnight. For example, 1:35 PM would be <c>new TimeSpan(1+12, 35, 0)</c>, assuming there was no Daylight Saving Time transition or leap second that day.</para>
    /// <para>If <see cref="TimeBasis"/> is <see cref="Basis.Sunrise"/>, this is the period after sunrise</para>
    /// <para>If <see cref="TimeBasis"/> is <see cref="Basis.Sunset"/>, this is the period after sunset, which depends on the outlet's location being updated by the mobile app (currently not exposed by this library). To specify a period of time before sunset instead of after, use a negative <see cref="TimeSpan"/>.</para>
    /// <para>Sunrise and Sunset depend on the outlet's location being updated by the Kasa mobile app (currently not exposed by this library).</para>
    /// <para>To specify a period of time before sunrise or sunset instead of after, use a negative <see cref="TimeSpan"/>.</para>
    /// </summary>
    [JsonIgnore]
    public TimeSpan Time {
        get => TimeSpan.FromMinutes(TimeBasis == Basis.StartOfDay ? MinutesSinceStartOfDay : MinutesSinceSunriseOrSunset);
        private set {
            int minutes = (int) value.TotalMinutes;
            if (TimeBasis == Basis.StartOfDay) {
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

    /// <summary>
    /// <para>Construct a new schedule that recurs weekly, on specific days, at a specific time of day.</para>
    /// </summary>
    /// <param name="willSetOutletOn">Whether to turn the outlet on (<c>true</c>) or off (<c>false</c>) when the schedule recurs.</param>
    /// <param name="weeklyRecurrence">Days of the week on which this schedule will recur every week.</param>
    /// <param name="timeOfDay">The time of day at which to turn the outlet on or off each day the schedule recurs.</param>
    public Schedule(bool willSetOutletOn, IEnumerable<DayOfWeek>? weeklyRecurrence, TimeOnly timeOfDay) {
        DaysOfWeek      = new HashSet<DayOfWeek>(weeklyRecurrence ?? Enumerable.Empty<DayOfWeek>());
        IsRecurring     = DaysOfWeek.Any();
        WillSetOutletOn = willSetOutletOn;
        TimeBasis       = Basis.StartOfDay;
        Time            = timeOfDay.ToTimeSpan();
    }

    /// <summary>
    /// <para>Construct a new schedule that recurs weekly, on specific days, at a specific period before or after sunrise or sunset.</para>
    /// </summary>
    /// <param name="willSetOutletOn">Whether to turn the outlet on (<c>true</c>) or off (<c>false</c>) when the schedule recurs.</param>
    /// <param name="weeklyRecurrence">Days of the week on which this schedule will recur every week.</param>
    /// <param name="timeBasis">The starting point to which <paramref name="time"/> is relative. Allows the schedule to recur based on the time of day, sunrise, or sunset.</param>
    /// <param name="time">The period of time after <paramref name="timeBasis"/> at which to turn the outlet on or off each day the schedule recurs. Can be negative if the <paramref name="timeBasis"/> is <see cref="Basis.Sunrise"/> or <see cref="Basis.Sunset"/> to recur before sunrise or sunset instead of after.</param>
    public Schedule(bool willSetOutletOn, IEnumerable<DayOfWeek>? weeklyRecurrence, Basis timeBasis, TimeSpan time) {
        DaysOfWeek      = new HashSet<DayOfWeek>(weeklyRecurrence ?? Enumerable.Empty<DayOfWeek>());
        IsRecurring     = DaysOfWeek.Any();
        WillSetOutletOn = willSetOutletOn;
        TimeBasis       = timeBasis;
        Time            = time;
    }

    /// <summary>
    /// Construct a one-time, non-recurring schedule that will occur only once at a specific date and time.
    /// </summary>
    /// <param name="willSetOutletOn">Whether to turn the outlet on (<c>true</c>) or off (<c>false</c>) when the schedule occurs.</param>
    /// <param name="singleOccurrence">The date and time at which the outlet will turn on or off.</param>
    public Schedule(bool willSetOutletOn, DateTime singleOccurrence) {
        DaysOfWeek      = new HashSet<DayOfWeek>();
        IsRecurring     = false;
        WillSetOutletOn = willSetOutletOn;
        TimeBasis       = Basis.StartOfDay;
        Date            = DateOnly.FromDateTime(singleOccurrence);
        Time            = singleOccurrence.TimeOfDay;
    }

    /// <summary>
    /// Construct a one-time, non-recurring schedule that will occur only once at a specific date and a specific period before or after sunrise or sunset.
    /// </summary>
    /// <param name="willSetOutletOn">Whether to turn the outlet on (<c>true</c>) or off (<c>false</c>) when the schedule occurs.</param>
    /// <param name="singleOccurrence">The date on which the outlet will turn on or off.</param>
    /// <param name="timeBasis">The starting point to which <paramref name="time"/> is relative. Allows the schedule to occur based on the time of day, sunrise, or sunset.</param>
    /// <param name="time">The period of time after <paramref name="timeBasis"/> at which to turn the outlet on or off. Can be negative if the <paramref name="timeBasis"/> is <see cref="Basis.Sunrise"/> or <see cref="Basis.Sunset"/> to occur before sunrise or sunset instead of after.</param>
    public Schedule(bool willSetOutletOn, DateOnly singleOccurrence, Basis timeBasis, TimeSpan time) {
        DaysOfWeek      = new HashSet<DayOfWeek>();
        IsRecurring     = false;
        WillSetOutletOn = willSetOutletOn;
        TimeBasis       = timeBasis;
        Date            = singleOccurrence;
        Time            = time;
    }

    /// <summary>
    /// Reference points for specifying schedule time periods.
    /// </summary>
    public enum Basis {

        /// <summary>
        /// Times will be relative to the start of the day, or midnight assuming there were no Daylight Saving Time transitions or leap seconds that day.
        /// </summary>
        StartOfDay = 0,

        /// <summary>
        /// <para>Times will be relative to local sunrise, and can be negative to express a period before sunrise instead of after.</para>
        /// <para>Accurate sunrise times rely on the Kasa mobile app to set the outlet's geographic location, which is currently not exposed by this library.</para>
        /// </summary>
        Sunrise = 1,

        /// <summary>
        /// <para>Times will be relative to local sunset, and can be negative to express a period before sunset instead of after.</para>
        /// <para>Accurate sunset times rely on the Kasa mobile app to set the outlet's geographic location, which is currently not exposed by this library.</para>
        /// </summary>
        Sunset = 2

    }

}