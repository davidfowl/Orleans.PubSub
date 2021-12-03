using Orleans.Runtime;

namespace Orleans.PubSub;

public class PubSubProvider : IPubSubProvider
{
    private readonly ILocalMessageBus _messageBus;
    private readonly IGrainFactory _grainFactory;
    private readonly ILocalSiloDetails _localSiloDetails;

    public PubSubProvider(ILocalMessageBus messageBus, IGrainFactory grainFactory, ILocalSiloDetails localSiloDetails)
    {
        _messageBus = messageBus;
        _grainFactory = grainFactory;
        _localSiloDetails = localSiloDetails;
    }

    public IPubSub Create(string topic)
    {
        var grain = _grainFactory.GetGrain<IPubSubGrain>(topic);
        return new PubSub(topic, _messageBus, grain, _localSiloDetails.SiloAddress);
    }

    class PubSub : IPubSub
    {
        private readonly string _topic;
        private readonly IPubSubGrain _grain;
        private readonly ILocalMessageBus _messageBus;
        private readonly SiloAddress _siloAddress;

        public PubSub(string topic, ILocalMessageBus messageBus, IPubSubGrain grain, SiloAddress siloAddress)
        {
            _topic = topic;
            _messageBus = messageBus;
            _grain = grain;
            _siloAddress = siloAddress;
        }

        public Task PublishAsync(byte[] message)
        {
            return _grain.Publish(message);
        }

        public async Task<IAsyncDisposable> SubscribeAsync(Func<byte[], Task> onMessage)
        {

            var disposable = _messageBus.Subscribe(_topic, onMessage);

            // This needs to keep track of the number of subscribers and appropriate add and remove silos
            await _grain.AddSilo(_siloAddress);

            return new Disposable(async () =>
            {
                disposable.Dispose();

                // Remove the grain when there are no more subscribers
                await _grain.RemoveSilo(_siloAddress);
            });
        }

        class Disposable : IAsyncDisposable
        {
            private readonly Func<ValueTask> disposeAsync;

            public Disposable(Func<ValueTask> disposeAsync)
            {
                this.disposeAsync = disposeAsync;
            }

            public ValueTask DisposeAsync()
            {
                return disposeAsync();
            }
        }
    }
}
