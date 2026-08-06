using System.Collections.Concurrent;

namespace Axent.Extensions.Caching;

internal sealed class CacheLockProvider
{
    private readonly ConcurrentDictionary<string, LockState> _locks = new();

    public async ValueTask<IDisposable> AcquireAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        LockState state;

        while (true)
        {
            state = _locks.GetOrAdd(key, static _ => new LockState());

            lock (state)
            {
                if (state.IsRemoved)
                {
                    continue;
                }

                state.ReferenceCount++;
                break;
            }
        }

        try
        {
            await state.Semaphore.WaitAsync(cancellationToken);
        }
        catch
        {
            ReleaseReference(key, state);
            throw;
        }

        return new Releaser(this, key, state);
    }

    private void Release(string key, LockState state)
    {
        state.Semaphore.Release();
        ReleaseReference(key, state);
    }

    private void ReleaseReference(string key, LockState state)
    {
        lock (state)
        {
            state.ReferenceCount--;
            if (state.ReferenceCount != 0)
            {
                return;
            }

            state.IsRemoved = true;
            _locks.TryRemove(KeyValuePair.Create(key, state));
        }

        state.Semaphore.Dispose();
    }

    private sealed class LockState
    {
        public SemaphoreSlim Semaphore { get; } = new(1, 1);
        public int ReferenceCount { get; set; }
        public bool IsRemoved { get; set; }
    }

    private sealed class Releaser(
        CacheLockProvider provider,
        string key,
        LockState state) : IDisposable
    {
        private LockState? _state = state;

        public void Dispose()
        {
            var stateToRelease = Interlocked.Exchange(ref _state, null);
            if (stateToRelease is not null)
            {
                provider.Release(key, stateToRelease);
            }
        }
    }
}
