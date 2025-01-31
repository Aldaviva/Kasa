namespace Kasa;

/// <summary>
/// Like <see cref="Lazy{T}"/>, but asynchronous.
/// </summary>
/// <typeparam name="T">type of the value to store</typeparam>
/// <param name="valueFactory">generator for the value, can be asynchronous</param>
internal class AsyncLazy<T>(Func<ValueTask<T>> valueFactory): IDisposable, IAsyncDisposable {

    private readonly SemaphoreSlim _mutex = new(1);

    private T _value = default!;

    public bool IsValueCreated { get; private set; }

    public async ValueTask<T> GetValue() {
        if (!IsValueCreated) {
            await _mutex.WaitAsync().ConfigureAwait(false);
            try {
                if (!IsValueCreated) {
                    _value         = await valueFactory().ConfigureAwait(false);
                    IsValueCreated = true;
                }
            } finally {
                _mutex.Release();
            }
        }
        return _value;
    }

    public bool TrySetValue(T value, bool impatient = false) {
        if (IsValueCreated) {
            return false;
        } else if (_mutex.Wait(impatient ? TimeSpan.Zero : Timeout.InfiniteTimeSpan)) {
            try {
                if (IsValueCreated) {
                    return false;
                } else {
                    _value         = value;
                    IsValueCreated = true;
                    return true;
                }
            } finally {
                _mutex.Release();
            }
        } else {
            return false;
        }
    }

    public void Dispose() {
        _mutex.Dispose();
        if (IsValueCreated && _value is IDisposable value) {
            value.Dispose();
        }
    }

    public async ValueTask DisposeAsync() {
        _mutex.Dispose();
        if (IsValueCreated) {
            switch (_value) {
                case IAsyncDisposable value:
                    await value.DisposeAsync().ConfigureAwait(false);
                    break;
                case IDisposable value:
                    value.Dispose();
                    break;
            }
        }
    }

}