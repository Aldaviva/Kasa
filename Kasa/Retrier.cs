using System;
using System.Threading;
using System.Threading.Tasks;

namespace Kasa;

internal class Retrier {

    private static readonly TimeSpan MaxDelay = TimeSpan.FromMilliseconds(int.MaxValue);

    /// <exception cref="Exception"></exception>
    public static void InvokeWithRetry(Action                 action,
                                       uint?                  maxAttempts    = 2,
                                       Func<int, TimeSpan>?   delay          = null,
                                       Func<Exception, bool>? isRetryAllowed = null,
                                       Action?                beforeRetry    = null) {
        Exception? exception = null;
        for (int attempt = 0; !maxAttempts.HasValue || attempt < maxAttempts || (attempt == 0 && maxAttempts == 0); attempt++) {
            if (exception != null) {
                if (isRetryAllowed?.Invoke(exception) ?? true) {
                    if (GetDelay(delay, attempt) is { } duration) {
                        Thread.Sleep(duration);
                    }

                    beforeRetry?.Invoke();
                } else {
                    break;
                }
            }

            try {
                action.Invoke();
            } catch (Exception e)when (e is not OutOfMemoryException) {
                exception = e;
            }
        }

        throw exception!;
    }

    /// <exception cref="Exception"></exception>
    public static T InvokeWithRetry<T>(Func<T>                action,
                                       uint?                  maxAttempts    = 2,
                                       Func<int, TimeSpan>?   delay          = null,
                                       Func<Exception, bool>? isRetryAllowed = null,
                                       Action?                beforeRetry    = null) {
        Exception? exception = null;
        for (int attempt = 0; !maxAttempts.HasValue || attempt < maxAttempts || (attempt == 0 && maxAttempts == 0); attempt++) {
            if (exception != null) {
                if (isRetryAllowed?.Invoke(exception) ?? true) {
                    if (GetDelay(delay, attempt) is { } duration) {
                        Thread.Sleep(duration);
                    }

                    beforeRetry?.Invoke();
                } else {
                    break;
                }
            }

            try {
                return action.Invoke();
            } catch (Exception e)when (e is not OutOfMemoryException) {
                exception = e;
            }
        }

        throw exception!;
    }

    /// <exception cref="Exception"></exception>
    public static async Task InvokeWithRetry(Func<Task>             action,
                                             uint?                  maxAttempts    = 2,
                                             Func<int, TimeSpan>?   delay          = null,
                                             Func<Exception, bool>? isRetryAllowed = null,
                                             Func<Task>?            beforeRetry    = null) {
        Exception? exception = null;
        for (int attempt = 0; !maxAttempts.HasValue || attempt < maxAttempts || (attempt == 0 && maxAttempts == 0); attempt++) {
            if (exception != null) {
                if (isRetryAllowed?.Invoke(exception) ?? true) {
                    if (GetDelay(delay, attempt) is { } duration) {
                        await Task.Delay(duration).ConfigureAwait(false);
                    }

                    beforeRetry?.Invoke();
                } else {
                    break;
                }
            }

            try {
                await action.Invoke().ConfigureAwait(false);
                return;
            } catch (Exception e)when (e is not OutOfMemoryException) {
                exception = e;
            }
        }

        throw exception!;
    }

    /// <exception cref="Exception"></exception>
    public static async Task<T> InvokeWithRetry<T>(Func<Task<T>>          action,
                                                   uint?                  maxAttempts    = 2,
                                                   Func<int, TimeSpan>?   delay          = null,
                                                   Func<Exception, bool>? isRetryAllowed = null,
                                                   Func<Task>?            beforeRetry    = null) {
        Exception? exception = null;
        for (int attempt = 0; !maxAttempts.HasValue || attempt < maxAttempts || (attempt == 0 && maxAttempts == 0); attempt++) {
            if (exception != null) {
                if (isRetryAllowed?.Invoke(exception) ?? true) {
                    if (GetDelay(delay, attempt) is { } duration) {
                        await Task.Delay(duration).ConfigureAwait(false);
                    }

                    beforeRetry?.Invoke();
                } else {
                    break;
                }
            }

            try {
                return await action.Invoke().ConfigureAwait(false);
            } catch (Exception e) when (e is not OutOfMemoryException) {
                exception = e;
            }
        }

        throw exception!;
    }

    private static TimeSpan? GetDelay(Func<int, TimeSpan>? delay, int attempt) => delay?.Invoke(attempt) switch {
        { } duration when duration <= TimeSpan.Zero => null,
        { } duration when duration > MaxDelay       => MaxDelay,
        { } duration                                => duration,
        null                                        => null
    };

}