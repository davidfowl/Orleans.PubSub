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

        Slot<Func<byte[], Task>> slot;

        do
        {
            slot = _subs.GetOrAdd(topic, static _ => new());

            lock (slot)
            {
                slot.Array = slot.Array.Add(onMessage);
            }

        } while (slot.Valid);

        return new Disposable(() =>
        {
            lock (slot)
            {
                slot.Array = slot.Array.Remove(onMessage);

                if (slot is { Array.IsEmpty: true })
                {
                    if (_subs.TryRemove(topic, out var s))
                    {
                        // Mark this slot as invalid since we're removing it
                        s.Valid = false;
                    }
                }
            }
        });
    }

    class Slot<T>
    {
        public ImmutableArray<T> Array = ImmutableArray<T>.Empty;

        public bool Valid;
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
