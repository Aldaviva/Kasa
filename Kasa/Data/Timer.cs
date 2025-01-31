using Newtonsoft.Json;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using TimeSpanConverter = Kasa.Marshal.TimeSpanConverter;

namespace Kasa;

/// <summary>
/// <para>A countdown timer rule for use with the <see cref="IKasaOutlet.Timer"/> family of commands.</para>
/// <para>These timers can turn a socket on or off after a specified duration.</para>
/// </summary>
public struct Timer {

    /// <summary>
    /// <para>The optional name of the timer.</para>
    /// <para>Only visible in the outlet's API, not the Kasa Android app.</para>
    /// <para>The Android app always sets this value to <c>add timer</c>, which is also the default for this library.</para>
    /// </summary>
    public string Name { get; set; } = "add timer";

    /// <summary>
    /// <para>Whether the timer is running (<c>true</c>) or paused (<c>false</c>).</para>
    /// <para>When the timer elapses and is no longer running, this value will become <c>false</c>.</para>
    /// </summary>
    [JsonProperty("enable")] public bool IsEnabled { get; set; }

    /// <summary>
    /// <para>Whether to turn the socket on (<c>true</c>) or off (<c>false</c>) when the timer elapses.</para>
    /// </summary>
    [JsonProperty("act")] public bool WillSetSocketOn { get; set; }

    /// <inheritdoc cref="WillSetSocketOn" />
    [JsonIgnore]
    [Obsolete($"This property was poorly named, and has been renamed to {nameof(WillSetSocketOn)}", false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    [ExcludeFromCodeCoverage]
    public bool WillSetOutletOn {
        get => WillSetSocketOn;
        set => WillSetSocketOn = value;
    }

    /// <summary>
    /// <para>How long the timer should wait, since it was started, before turning on or off.</para>
    /// <para>When the timer elapses and is no longer running, this value will become <see cref="TimeSpan.Zero"/>.</para>
    /// <para>To get the duration remaining from the current time, call <see cref="IKasaOutletBase.ITimerCommandsSingleSocket.Get"/> and check the <see cref="RemainingDuration"/> property.</para>
    /// </summary>
    [JsonProperty("delay")]
    [JsonConverter(typeof(TimeSpanConverter))]
    public TimeSpan TotalDuration { get; set; }

    /// <summary>
    /// <para>How much time is left on a timer before it completes and turns the outlet on or off.</para>
    /// <para>When the timer elapses and is no longer running, this value will become <see cref="TimeSpan.Zero"/>.</para>
    /// <para>You can't change the timer duration by setting this property. To change the duration, set <see cref="TotalDuration"/> instead. This property is only settable so you can create instances for mocking in unit tests.</para>
    /// </summary>
    [JsonIgnore] public TimeSpan RemainingDuration { get; set; }

#pragma warning disable IDE0051 // Remove unused private members - used to make a deserialized-only property in Json.NET
    [JsonProperty("remain")]
    [JsonConverter(typeof(TimeSpanConverter))]
    private TimeSpan RemainingDurationJson {
        set => RemainingDuration = value;
    }
#pragma warning restore IDE0051 // Remove unused private members

    /// <summary>
    /// <para>A countdown timer rule for use with the <see cref="IKasaOutlet.Timer"/> family of commands.</para>
    /// <para>These timers can turn the socket on or off after a specified duration.</para>
    /// </summary>
    /// <param name="duration">How long the timer should wait, after being set on the outlet, before turning on or off.</param>
    /// <param name="willSetSocketOn">Whether to turn the socket on (<c>true</c>) or off (<c>false</c>) when the timer elapses.</param>
    public Timer(TimeSpan duration, bool willSetSocketOn): this() {
        IsEnabled       = true;
        TotalDuration   = duration;
        WillSetSocketOn = willSetSocketOn;
    }

}