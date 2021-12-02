using System.Collections.Concurrent;
using Orleans.Runtime;

namespace Orleans.PubSub;

public class SiloAwareMessageBus : ILocalMessageBus
{
    private readonly ILocalSiloDetails _localSiloDetails;
    private readonly ConcurrentDictionary<string, Func<byte[], Task>> _subs = new();

    public SiloAwareMessageBus(ILocalSiloDetails localSiloDetails)
    {
        _localSiloDetails = localSiloDetails;
    }

    public Task PublishAsync(string topic, byte[] message)
    {
        topic = _localSiloDetails.SiloAddress.Topic(topic);

        if (_subs.TryGetValue(topic, out var cb))
        {
            return cb(message);
        }
        return Task.CompletedTask;
    }

    public IDisposable Subscribe(string topic, Func<byte[], Task> onMessage)
    {
        topic = _localSiloDetails.SiloAddress.Topic(topic);

        var cb = _subs.AddOrUpdate(topic, key => onMessage, (key, existing) => existing += onMessage);

        return new Disposable(() =>
        {
            cb -= onMessage;

            if (cb is null)
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
