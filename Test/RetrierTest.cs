using Kasa;

namespace Test;

public class RetrierTest {

    [Fact]
    public void ActionImmediateSuccess() {
        Failer failer = new(0);
        Retrier.InvokeWithRetry(() => failer.InvokeAction());
        failer.InvocationCount.Should().Be(1);
    }

    [Fact]
    public void ActionRetrySuccess() {
        Failer failer = new(1);
        Retrier.InvokeWithRetry(() => failer.InvokeAction(), delay: _ => TimeSpan.Zero);
        failer.InvocationCount.Should().Be(2);
    }

    [Fact]
    public void ActionRetryFailure() {
        Failer failer  = new(2);
        Action thrower = () => Retrier.InvokeWithRetry(() => failer.InvokeAction(), delay: _ => TimeSpan.FromMilliseconds(1));
        thrower.Should().ThrowExactly<Failure>();
        failer.InvocationCount.Should().Be(2);
    }

    [Fact]
    public void ActionRetryNotAllowed() {
        Failer failer  = new(1);
        Action thrower = () => Retrier.InvokeWithRetry(() => failer.InvokeAction(), isRetryAllowed: _ => false);
        thrower.Should().ThrowExactly<Failure>();
        failer.InvocationCount.Should().Be(1);
    }

    [Fact]
    public void FuncImmediateSuccess() {
        Failer failer = new(0);
        Retrier.InvokeWithRetry(() => failer.InvokeFunc());
        failer.InvocationCount.Should().Be(1);
    }

    [Fact]
    public void FuncRetrySuccess() {
        Failer failer = new(1);
        Retrier.InvokeWithRetry(() => failer.InvokeFunc());
        failer.InvocationCount.Should().Be(2);
    }

    [Fact]
    public void FuncRetryFailure() {
        Failer     failer  = new(2);
        Func<bool> thrower = () => Retrier.InvokeWithRetry(() => failer.InvokeFunc(), delay: _ => TimeSpan.FromMilliseconds(1));
        thrower.Should().ThrowExactly<Failure>();
        failer.InvocationCount.Should().Be(2);
    }

    [Fact]
    public void FuncRetryNotAllowed() {
        Failer     failer  = new(1);
        Func<bool> thrower = () => Retrier.InvokeWithRetry(() => failer.InvokeFunc(), isRetryAllowed: _ => false);
        thrower.Should().ThrowExactly<Failure>();
        failer.InvocationCount.Should().Be(1);
    }

    [Fact]
    public async Task AsyncActionImmediateSuccess() {
        Failer failer = new(0);
        await Retrier.InvokeWithRetry(async () => await failer.InvokeActionAsync());
        failer.InvocationCount.Should().Be(1);
    }

    [Fact]
    public async Task AsyncActionRetrySuccess() {
        Failer failer = new(1);
        await Retrier.InvokeWithRetry(async () => await failer.InvokeActionAsync(), delay: _ => TimeSpan.Zero);
        failer.InvocationCount.Should().Be(2);
    }

    [Fact]
    public async Task AsyncActionRetryFailure() {
        Failer     failer  = new(2);
        Func<Task> thrower = async () => await Retrier.InvokeWithRetry(async () => await failer.InvokeActionAsync(), delay: _ => TimeSpan.FromMilliseconds(1));
        await thrower.Should().ThrowExactlyAsync<Failure>();
        failer.InvocationCount.Should().Be(2);
    }

    [Fact]
    public async Task AsyncActionRetryNotAllowed() {
        Failer     failer  = new(1);
        Func<Task> thrower = async () => await Retrier.InvokeWithRetry(async () => await failer.InvokeActionAsync(), isRetryAllowed: _ => false);
        await thrower.Should().ThrowExactlyAsync<Failure>();
        failer.InvocationCount.Should().Be(1);
    }

    [Fact]
    public async Task AsyncFuncImmediateSuccess() {
        Failer failer = new(0);
        await Retrier.InvokeWithRetry(async () => await failer.InvokeFuncAsync());
        failer.InvocationCount.Should().Be(1);
    }

    [Fact]
    public async Task AsyncFuncRetrySuccess() {
        Failer failer = new(1);
        await Retrier.InvokeWithRetry(async () => await failer.InvokeFuncAsync(), delay: _ => TimeSpan.Zero);
        failer.InvocationCount.Should().Be(2);
    }

    [Fact]
    public async Task AsyncFuncRetryFailure() {
        Failer     failer  = new(2);
        Func<Task> thrower = async () => await Retrier.InvokeWithRetry(async () => await failer.InvokeFuncAsync(), delay: _ => TimeSpan.FromMilliseconds(1));
        await thrower.Should().ThrowExactlyAsync<Failure>();
        failer.InvocationCount.Should().Be(2);
    }

    [Fact]
    public async Task AsyncFuncRetryNotAllowed() {
        Failer     failer  = new(1);
        Func<Task> thrower = async () => await Retrier.InvokeWithRetry(async () => await failer.InvokeFuncAsync(), isRetryAllowed: _ => false);
        await thrower.Should().ThrowExactlyAsync<Failure>();
        failer.InvocationCount.Should().Be(1);
    }

    [Fact]
    public async Task MaxDelay() {
        Failer                  failer  = new(1);
        CancellationTokenSource cts     = new(200);
        Func<Task>              thrower = async () => await Retrier.InvokeWithRetry(async () => await failer.InvokeActionAsync(), delay: _ => TimeSpan.MaxValue, cancellationToken: cts.Token);
        await thrower.Should().ThrowAsync<TaskCanceledException>();
        failer.InvocationCount.Should().Be(1);
    }

    private class Failure: Exception { }

    private class Failer {

        private static readonly Failure Exception = new();

        private readonly int _timesToFail;

        public int InvocationCount { get; private set; }

        public Failer(int timesToFail) {
            _timesToFail = timesToFail;
        }

        public void InvokeAction() {
            if (++InvocationCount <= _timesToFail) {
                throw Exception;
            }
        }

        public bool InvokeFunc() {
            if (++InvocationCount <= _timesToFail) {
                throw Exception;
            }

            return true;
        }

        public Task InvokeActionAsync() {
            if (++InvocationCount <= _timesToFail) {
                throw Exception;
            }

            return Task.CompletedTask;
        }

        public Task<bool> InvokeFuncAsync() {
            if (++InvocationCount <= _timesToFail) {
                throw Exception;
            }

            return Task.FromResult(true);
        }

    }

}