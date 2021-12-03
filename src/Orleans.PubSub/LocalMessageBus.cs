using System.Collections.Concurrent;
using System.Collections.Immutable;
using Orleans.Runtime;

namespace Orleans.PubSub;

public class LocalMessageBus : ILocalMessageBus
{
    private readonly ILocalSiloDetails _localSiloDetails;
    private readonly ConcurrentDictionary<string, Slot<Func<byte[], Task>>> _subs = new();

    public LocalMessageBus(ILocalSiloDetails localSiloDetails)
    {
        _localSiloDetails = localSiloDetails;
    }

    public Task PublishAsync(string topic, byte[] message)
    {
        topic = _localSiloDetails.SiloAddress.Topic(topic);

        if (_subs.TryGetValue(topic, out var slot))
        {
            var array = slot.Array;

            var list = new List<Task>(array.Length);
            foreach (var cb in array)
            {
                list.Add(cb(message));
            }

            return Task.WhenAll(list);

        }
        return Task.CompletedTask;
    }

    public IDisposable Subscribe(string topic, Func<byte[], Task> onMessage)
    {
        topic = _localSiloDetails.SiloAddress.Topic(topic);

        var slot = _subs.GetOrAdd(topic, static _ => new());

        lock (slot)
        {
            slot.Array = slot.Array.Add(onMessage);
        }

        return new Disposable(() =>
        {
            lock (slot)
            {
                slot.Array = slot.Array.Remove(onMessage);

                // TODO: Address memory leak with empty slots
                //if (slot is { Array.Length: 0 })
                //{
                //    slot.Array = default;

                //    _subs.TryRemove(topic, out _);
                //}
            }
        });
    }

    class Slot<T>
    {
        public ImmutableArray<T> Array = ImmutableArray<T>.Empty;
    }

    private class Disposable : IDisposable
    {
        private readonly Action _action;

        public Disposable(Action action)
        {
            _action = action;
        }

        public void Dispose()
        {
            _action();
        }
    }
}
