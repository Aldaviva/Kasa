using System.Diagnostics.CodeAnalysis;

namespace Kasa;

/// <summary>
/// Conveniently create <see cref="KasaOutlet"/> and <see cref="MultiSocketKasaOutlet"/> instances by testing the capabilities of a given hostname, without knowing ahead of time if the outlet has multiple sockets.
/// </summary>
public static class KasaOutletFactory {

    /// <summary>
    /// <para>Convenience method to construct a Kasa outlet client instance without knowing ahead of time how many sockets it has.</para>
    /// <para>This has some limitations, and it may be better to perform this logic in your own code, or to use configuration or parameterization, because</para>
    /// <list type="bullet"><item><description>this is less type-safe because the base class is always returned, so you will have to choose the correct subinterface or subclass to cast it to with reflection (<c>is</c>, <c>switch</c>, <c>IsAssignableTo</c>, etc).</description></item>
    /// <item><description>this requires the outlet to be online and reachable at construction time, whereas the class constructors are faster, synchronous, exceptionless, and offline.</description></item></list>
    /// <para>An alternative to using this class is to construct a new instance of <see cref="KasaOutlet"/> (single socket) or <see cref="MultiSocketKasaOutlet"/> (multiple sockets).</para>
    /// </summary>
    /// <param name="hostname">The fully-qualified domain name or IP address of the Kasa device to connect to.</param>
    /// <param name="options">Non-required configuration parameters for the <see cref="KasaOutlet"/>, which can be used to fine-tune its behavior.</param>
    /// <returns>A subclass of <see cref="IKasaOutletBase"/> depending on the number of sockets on the outlet with the given <paramref name="hostname"/>: single socket outlets (like the EP10) will return an <see cref="IKasaOutlet"/>, and multi-socket outlets (like the EP40) will return an <see cref="IMultiSocketKasaOutlet"/>.</returns>
    /// <exception cref="NetworkException">if the TCP connection to the outlet failed and could not automatically reconnect</exception>
    /// <exception cref="ResponseParsingException">if the JSON received from the outlet contained unexpected data</exception>
    public static async Task<IKasaOutletBase> CreateOutlet(string hostname, Options? options = null) {
        IKasaOutlet singleSocketOutlet = SingleSocketOutletFactory(hostname, options);
        if (await singleSocketOutlet.System.CountSockets().ConfigureAwait(false) == 1) {
            return singleSocketOutlet;
        } else {
            singleSocketOutlet.Dispose();
            return MultiSocketOutletFactory(hostname, options);
        }
    }

    internal static Func<string, Options?, IKasaOutlet> SingleSocketOutletFactory = [ExcludeFromCodeCoverage](hostname, options) => new KasaOutlet(hostname, options);

    internal static Func<string, Options?, IMultiSocketKasaOutlet> MultiSocketOutletFactory = [ExcludeFromCodeCoverage](hostname, options) => new MultiSocketKasaOutlet(hostname, options);

}