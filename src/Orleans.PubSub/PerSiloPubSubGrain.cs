using Orleans.Placement;

namespace Orleans.PubSub;

/// <summary>
/// The <see cref="PerSiloPubSubGrain"/> broadcasts to local subscriptions on the silo.
/// </summary>
[FixedPlacement]
public class PerSiloPubSubGrain : Grain, IPerSiloPubSubGrain
{
    private readonly ILocalMessageBus _bus;
    private string? _topic;

    public PerSiloPubSubGrain(ILocalMessageBus bus)
    {
        _bus = bus;
    }

    public override Task OnActivateAsync()
    {
        _topic = this.GetPrimaryKeyString();
        return base.OnActivateAsync();
    }

    public Task Publish(byte[] message)
    {
        return _bus.PublishAsync(_topic!, message);
    }
}
