using Orleans.Runtime;

namespace Orleans.PubSub;

public class PubSub : IPubSub
{
    private readonly ILocalMessageBus _messageBus;
    private readonly IGrainFactory _grainFactory;
    private readonly ILocalSiloDetails _localSiloDetails;

    public PubSub(ILocalMessageBus messageBus, IGrainFactory grainFactory, ILocalSiloDetails localSiloDetails)
    {
        _messageBus = messageBus;
        _grainFactory = grainFactory;
        _localSiloDetails = localSiloDetails;
    }

    public Task PublishAsync(string topic, byte[] message)
    {
        return _grainFactory.GetGrain<IPubSubGrain>(topic).Publish(message);
    }

    public async Task<IAsyncDisposable> SubscribeAsync(string topic, Func<byte[], Task> onMessage)
    {
        var disposable = _messageBus.Subscribe(topic, onMessage);

        // This needs to keep track of the number of subscribers and appropriate add and remove silos
        await _grainFactory.GetGrain<IPubSubGrain>(topic).AddSilo(_localSiloDetails.SiloAddress);

        return new Disposable(async () =>
        {
            disposable.Dispose();

            // Remove the grain when there are no more subscribers
            await _grainFactory.GetGrain<IPubSubGrain>(topic).RemoveSilo(_localSiloDetails.SiloAddress);
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
