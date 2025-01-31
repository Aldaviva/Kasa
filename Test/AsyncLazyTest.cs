using Kasa;

namespace Test;

public class AsyncLazyTest: IDisposable {

    private int _initializations;

    private readonly AsyncLazy<int> _asyncLazy;

    public AsyncLazyTest() {
        _asyncLazy = new AsyncLazy<int>(() => {
            Interlocked.Increment(ref _initializations);
            return ValueTask.FromResult(1);
        });
    }

    [Fact]
    public void ValueNotCreatedOnConstruction() {
        _asyncLazy.IsValueCreated.Should().BeFalse();
    }

    [Fact]
    public async Task EagerlyLoad() {
        bool success = _asyncLazy.TrySetValue(2);
        success.Should().BeTrue();
        _asyncLazy.IsValueCreated.Should().BeTrue();
        int actual = await _asyncLazy.GetValue();
        actual.Should().Be(2);

        success = _asyncLazy.TrySetValue(3);
        success.Should().BeFalse("value was already set");
        _asyncLazy.IsValueCreated.Should().BeTrue();
        actual = await _asyncLazy.GetValue();
        actual.Should().Be(2);
        _initializations.Should().Be(0);
    }

    [Fact]
    public async Task LazilyLoad() {
        int actual = await _asyncLazy.GetValue();
        actual.Should().Be(1);
        _asyncLazy.IsValueCreated.Should().BeTrue();
        _initializations.Should().Be(1);
    }

    [Fact]
    public async Task ConcurrentLazilyLoad() {
        int[] actuals = await Task.WhenAll(
            _asyncLazy.GetValue().AsTask(),
            _asyncLazy.GetValue().AsTask(),
            _asyncLazy.GetValue().AsTask());

        actuals.Should().AllBeEquivalentTo(1);
        _asyncLazy.IsValueCreated.Should().BeTrue();
        _initializations.Should().Be(1);
    }

    [Fact]
    public async Task DisposesInnerValue() {
        var              asyncLazy  = new AsyncLazy<MySyncDisposable>(() => ValueTask.FromResult(new MySyncDisposable()));
        MySyncDisposable disposable = await asyncLazy.GetValue();
        disposable.Disposed.Should().BeFalse();

        // ReSharper disable once MethodHasAsyncOverload - testing both implementations
        asyncLazy.Dispose();
        await asyncLazy.DisposeAsync();

        disposable.Disposed.Should().BeTrue();
    }

    [Fact]
    public async Task AsyncDisposesInnerValue() {
        var               asyncLazy  = new AsyncLazy<MyAsyncDisposable>(() => ValueTask.FromResult(new MyAsyncDisposable()));
        MyAsyncDisposable disposable = await asyncLazy.GetValue();
        disposable.Disposed.Should().BeFalse();

        await asyncLazy.DisposeAsync();

        disposable.Disposed.Should().BeTrue();
    }

    public void Dispose() {
        _asyncLazy.Dispose();
        GC.SuppressFinalize(this);
    }

    private class MySyncDisposable: IDisposable {

        public volatile bool Disposed;

        public void Dispose() {
            Disposed = true;
        }

    }

    private class MyAsyncDisposable: IAsyncDisposable {

        public volatile bool Disposed;

        public ValueTask DisposeAsync() {
            Disposed = true;
            return ValueTask.CompletedTask;
        }

    }

}