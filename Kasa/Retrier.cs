using System;
using System.Threading;
using System.Threading.Tasks;

namespace Kasa;

/// <summary>
/// Run a function with retries if it throws an exception.
/// </summary>
internal static class Retrier {

    private static readonly TimeSpan MaxDelay = TimeSpan.FromMilliseconds(int.MaxValue);

    /// <summary>
    /// Run the given <paramref name="action"/> at most <paramref name="maxAttempts"/> times, until it returns without throwing an exception.
    /// </summary>
    /// <param name="action">An action which is prone to sometimes throw exceptions.</param>
    /// <param name="maxAttempts">The total number of times <paramref name="action"/> is allowed to run in this invocation. Must be at least 1, if you pass 0 it will clip to 1. Defaults to 2. This is equal to 1 initial attempt plus <c>maxAttempts-1</c> retries if it throws an exception.</param>
    /// <param name="delay">How long to wait between attempts. Defaults to no delay. This is a function of how many attempts have already failed (starting from <c>1</c>), to allow for strategies such as exponential back-off. Values outside the range <c>[0 ms, int.MaxValue ms]</c> will be clipped.</param>
    /// <param name="isRetryAllowed">Allows certain exceptions that indicate permanent failures to not trigger retries. For example, <see cref="ArgumentOutOfRangeException"/> will usually be thrown every time you call a function with the same arguments, so there is no reason to retry, and <paramref name="isRetryAllowed"/> could return <c>false</c> in that case. Defaults to retrying on every exception besides <see cref="OutOfMemoryException"/>.</param>
    /// <param name="beforeRetry">Action to run between attempts, possibly to clean up some state before the next retry. For example, you may want to disconnect a failed connection before reconnecting. Defaults to no action.</param>
    /// <param name="cancellationToken">Allows you to cancel the delay between attempts.</param>
    /// <exception cref="Exception">Any exception thrown by <paramref name="action"/> on its final attempt.</exception>
    public static void InvokeWithRetry(Action                 action,
                                       uint?                  maxAttempts       = 2,
                                       Func<int, TimeSpan>?   delay             = null,
                                       Func<Exception, bool>? isRetryAllowed    = null,
                                       Action?                beforeRetry       = null,
                                       CancellationToken      cancellationToken = default) {
        for (int attempt = 0; !maxAttempts.HasValue || attempt < maxAttempts - 1; attempt++) {
            try {
                action.Invoke();
                return;
            } catch (Exception e) when (e is not OutOfMemoryException) {
                // This brace-less syntax convinces dotCover that the "throw;" statement is fully covered.
                if (!isRetryAllowed?.Invoke(e) ?? false) throw;

                if (GetDelay(delay, attempt) is { } duration) {
                    Task.Delay(duration, cancellationToken).GetAwaiter().GetResult();
                }

                beforeRetry?.Invoke();
            }
        }

        action.Invoke();
    }

    /// <summary>
    /// Run the given <paramref name="func"/> at most <paramref name="maxAttempts"/> times, until it returns without throwing an exception.
    /// </summary>
    /// <param name="func">An action which is prone to sometimes throw exceptions.</param>
    /// <param name="maxAttempts">The total number of times <paramref name="func"/> is allowed to run in this invocation. Must be at least 1, if you pass 0 it will clip to 1. Defaults to 2. This is equal to 1 initial attempt plus <c>maxAttempts-1</c> retries if it throws an exception.</param>
    /// <param name="delay">How long to wait between attempts. Defaults to no delay. This is a function of how many attempts have already failed (starting from <c>1</c>), to allow for strategies such as exponential back-off. Values outside the range <c>[0 ms, int.MaxValue ms]</c> will be clipped.</param>
    /// <param name="isRetryAllowed">Allows certain exceptions that indicate permanent failures to not trigger retries. For example, <see cref="ArgumentOutOfRangeException"/> will usually be thrown every time you call a function with the same arguments, so there is no reason to retry, and <paramref name="isRetryAllowed"/> could return <c>false</c> in that case. Defaults to retrying on every exception besides <see cref="OutOfMemoryException"/>.</param>
    /// <param name="beforeRetry">Action to run between attempts, possibly to clean up some state before the next retry. For example, you may want to disconnect a failed connection before reconnecting. Defaults to no action.</param>
    /// <param name="cancellationToken">Allows you to cancel the delay between attempts.</param>
    /// <exception cref="Exception">Any exception thrown by <paramref name="func"/> on its final attempt.</exception>
    public static T InvokeWithRetry<T>(Func<T>                func,
                                       uint?                  maxAttempts       = 2,
                                       Func<int, TimeSpan>?   delay             = null,
                                       Func<Exception, bool>? isRetryAllowed    = null,
                                       Action?                beforeRetry       = null,
                                       CancellationToken      cancellationToken = default) {
        for (int attempt = 0; !maxAttempts.HasValue || attempt < maxAttempts - 1; attempt++) {
            try {
                return func.Invoke();
            } catch (Exception e) when (e is not OutOfMemoryException) {
                if (!isRetryAllowed?.Invoke(e) ?? false) throw;

                if (GetDelay(delay, attempt) is { } duration) {
                    Task.Delay(duration, cancellationToken).GetAwaiter().GetResult();
                }

                beforeRetry?.Invoke();
            }
        }

        return func.Invoke();
    }

    /// <summary>
    /// Run the given <paramref name="func"/> at most <paramref name="maxAttempts"/> times, until it returns without throwing an exception.
    /// </summary>
    /// <param name="func">An action which is prone to sometimes throw exceptions.</param>
    /// <param name="maxAttempts">The total number of times <paramref name="func"/> is allowed to run in this invocation. Must be at least 1, if you pass 0 it will clip to 1. Defaults to 2. This is equal to 1 initial attempt plus <c>maxAttempts-1</c> retries if it throws an exception.</param>
    /// <param name="delay">How long to wait between attempts. Defaults to no delay. This is a function of how many attempts have already failed (starting from <c>1</c>), to allow for strategies such as exponential back-off. Values outside the range <c>[0 ms, int.MaxValue ms]</c> will be clipped.</param>
    /// <param name="isRetryAllowed">Allows certain exceptions that indicate permanent failures to not trigger retries. For example, <see cref="ArgumentOutOfRangeException"/> will usually be thrown every time you call a function with the same arguments, so there is no reason to retry, and <paramref name="isRetryAllowed"/> could return <c>false</c> in that case. Defaults to retrying on every exception besides <see cref="OutOfMemoryException"/>.</param>
    /// <param name="beforeRetry">Action to run between attempts, possibly to clean up some state before the next retry. For example, you may want to disconnect a failed connection before reconnecting. Defaults to no action.</param>
    /// <param name="cancellationToken">Allows you to cancel the delay between attempts.</param>
    /// <exception cref="Exception">Any exception thrown by <paramref name="func"/> on its final attempt.</exception>
    public static async Task InvokeWithRetry(Func<Task>             func,
                                             uint?                  maxAttempts       = 2,
                                             Func<int, TimeSpan>?   delay             = null,
                                             Func<Exception, bool>? isRetryAllowed    = null,
                                             Func<Task>?            beforeRetry       = null,
                                             CancellationToken      cancellationToken = default) {
        for (int attempt = 0; !maxAttempts.HasValue || attempt < maxAttempts - 1; attempt++) {
            try {
                await func.Invoke().ConfigureAwait(false);
                return;
            } catch (Exception e) when (e is not OutOfMemoryException) {
                if (!isRetryAllowed?.Invoke(e) ?? false) throw;

                if (GetDelay(delay, attempt) is { } duration) {
                    await Task.Delay(duration, cancellationToken).ConfigureAwait(false);
                }

                beforeRetry?.Invoke();
            }
        }

        await func.Invoke().ConfigureAwait(false);
    }

    /// <summary>
    /// Run the given <paramref name="func"/> at most <paramref name="maxAttempts"/> times, until it returns without throwing an exception.
    /// </summary>
    /// <param name="func">An action which is prone to sometimes throw exceptions.</param>
    /// <param name="maxAttempts">The total number of times <paramref name="func"/> is allowed to run in this invocation. Must be at least 1, if you pass 0 it will clip to 1. Defaults to 2. This is equal to 1 initial attempt plus <c>maxAttempts-1</c> retries if it throws an exception.</param>
    /// <param name="delay">How long to wait between attempts. Defaults to no delay. This is a function of how many attempts have already failed (starting from <c>1</c>), to allow for strategies such as exponential back-off. Values outside the range <c>[0 ms, int.MaxValue ms]</c> will be clipped.</param>
    /// <param name="isRetryAllowed">Allows certain exceptions that indicate permanent failures to not trigger retries. For example, <see cref="ArgumentOutOfRangeException"/> will usually be thrown every time you call a function with the same arguments, so there is no reason to retry, and <paramref name="isRetryAllowed"/> could return <c>false</c> in that case. Defaults to retrying on every exception besides <see cref="OutOfMemoryException"/>.</param>
    /// <param name="beforeRetry">Action to run between attempts, possibly to clean up some state before the next retry. For example, you may want to disconnect a failed connection before reconnecting. Defaults to no action.</param>
    /// <param name="cancellationToken">Allows you to cancel the delay between attempts.</param>
    /// <exception cref="Exception">Any exception thrown by <paramref name="func"/> on its final attempt.</exception>
    public static async Task<T> InvokeWithRetry<T>(Func<Task<T>>          func,
                                                   uint?                  maxAttempts       = 2,
                                                   Func<int, TimeSpan>?   delay             = null,
                                                   Func<Exception, bool>? isRetryAllowed    = null,
                                                   Func<Task>?            beforeRetry       = null,
                                                   CancellationToken      cancellationToken = default) {
        for (int attempt = 0; !maxAttempts.HasValue || attempt < maxAttempts - 1; attempt++) {
            try {
                return await func.Invoke().ConfigureAwait(false);
            } catch (Exception e) when (e is not OutOfMemoryException) {
                if (!isRetryAllowed?.Invoke(e) ?? false) throw;

                if (GetDelay(delay, attempt) is { } duration) {
                    await Task.Delay(duration, cancellationToken).ConfigureAwait(false);
                }

                beforeRetry?.Invoke();
            }
        }

        return await func.Invoke().ConfigureAwait(false);
    }

    private static TimeSpan? GetDelay(Func<int, TimeSpan>? delay, int attempt) => delay?.Invoke(attempt) switch {
        { } duration when duration <= TimeSpan.Zero => null,
        { } duration when duration > MaxDelay       => MaxDelay,
        { } duration                                => duration,
        null                                        => null
    };

}