using Orleans.Runtime;

namespace Orleans.PubSub;

/// <summary>
/// The <see cref="PubSubGrain"/> is the entry point for interacting with the pub sub system.
/// This grain stores a list of silos that are subscribed to this topic (which is represented by the grain key) and also
/// will fan out to per silo, per topic grains for publishing.
/// </summary>
public class PubSubGrain : Grain, IPubSubGrain
{
    private readonly HashSet<string> _subs = new();
    private string? _topic;
    private readonly IGrainFactory _factory;

    public PubSubGrain(IGrainFactory factory)
    {
        _factory = factory;
    }

    public override Task OnActivateAsync()
    {
        _topic = this.GetPrimaryKeyString();
        return base.OnActivateAsync();
    }

    public Task AddSilo(SiloAddress siloAddress)
    {
        _subs.Add(siloAddress.Topic(_topic!));

        return Task.CompletedTask;
    }

    public Task RemoveSilo(SiloAddress siloAddress)
    {
        _subs.Remove(siloAddress.Topic(_topic!));

        return Task.CompletedTask;
    }

    public Task Publish(byte[] message)
    {
        var tasks = new List<Task>();
        foreach (var sub in _subs)
        {
            tasks.Add(_factory.GetGrain<IPerSiloPubSubGrain>(sub).Publish(message));
        }
        return Task.WhenAll(tasks);
    }
}
