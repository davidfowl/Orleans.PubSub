using System.Collections.Concurrent;
using System.Collections.Immutable;
using Orleans.Runtime;

namespace Orleans.PubSub;

public class LocalMessageBus : ILocalMessageBus
{
    private readonly ILocalSiloDetails _localSiloDetails;
    private readonly ConcurrentDictionary<string, ImmutableArray<Func<byte[], Task>>> _subs = new();

    public LocalMessageBus(ILocalSiloDetails localSiloDetails)
    {
        _localSiloDetails = localSiloDetails;
    }

    public async Task PublishAsync(string topic, byte[] message)
    {
        topic = _localSiloDetails.SiloAddress.Topic(topic);

        if (_subs.TryGetValue(topic, out var callbacks))
        {
            foreach (var cb in callbacks)
            {
                await cb(message);
            }
        }
    }

    public IDisposable Subscribe(string topic, Func<byte[], Task> onMessage)
    {
        topic = _localSiloDetails.SiloAddress.Topic(topic);

        var callbacks = _subs.GetOrAdd(topic, static _ => new());

        callbacks = callbacks.Add(onMessage);

        return new Disposable(() =>
        {
            callbacks = callbacks.Remove(onMessage);

            if (callbacks is { Length: 0 })
            {
                _subs.TryRemove(topic, out _);
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
