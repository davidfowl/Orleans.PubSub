using System.Collections.Concurrent;
using Orleans.Runtime;

namespace Orleans.PubSub;

public class LocalMessageBus : ILocalMessageBus
{
    private readonly ILocalSiloDetails _localSiloDetails;
    private readonly ConcurrentDictionary<string, List<Func<byte[], Task>>> _subs = new();

    public LocalMessageBus(ILocalSiloDetails localSiloDetails)
    {
        _localSiloDetails = localSiloDetails;
    }

    public Task PublishAsync(string topic, byte[] message)
    {
        topic = _localSiloDetails.SiloAddress.Topic(topic);

        if (_subs.TryGetValue(topic, out var callbacks))
        {
            lock (callbacks)
            {
                var list = new List<Task>(callbacks.Count);
                foreach (var cb in callbacks)
                {
                    list.Add(cb(message));
                }

                return Task.WhenAll(list);
            }
        }
        return Task.CompletedTask;
    }

    public IDisposable Subscribe(string topic, Func<byte[], Task> onMessage)
    {
        topic = _localSiloDetails.SiloAddress.Topic(topic);

        var callbacks = _subs.GetOrAdd(topic, static _ => new());

        lock (callbacks)
        {
            callbacks.Add(onMessage);
        }

        return new Disposable(() =>
        {
            lock (callbacks)
            {
                callbacks.Remove(onMessage);

                if (callbacks is { Count: 0 })
                {
                    _subs.TryRemove(topic, out _);
                }
            }
        });
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
