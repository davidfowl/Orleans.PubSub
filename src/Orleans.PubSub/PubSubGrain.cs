using Orleans.Runtime;

namespace Orleans.PubSub;

/// <summary>
/// The <see cref="PubSubGrain"/> is the entry point for interacting with the pub sub system.
/// This grain stores a list of silos that are subscribed to this topic (which is represented by the grain key) and also
/// will fan out to per silo, per topic grains for publishing.
/// </summary>
public class PubSubGrain : Grain, IPubSubGrain
{
    private readonly Dictionary<string, IPerSiloPubSubGrain> _subs = new(StringComparer.OrdinalIgnoreCase);
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
        var key = siloAddress.Topic(_topic!);

        if (_subs.ContainsKey(key))
        {
            return Task.CompletedTask;
        }

        _subs.Add(key, _factory.GetGrain<IPerSiloPubSubGrain>(key));

        return Task.CompletedTask;
    }

    public Task RemoveSilo(SiloAddress siloAddress)
    {
        _subs.Remove(siloAddress.Topic(_topic!));

        return Task.CompletedTask;
    }

    public Task Publish(byte[] message)
    {
        if (_subs.Count == 0)
        {
            return Task.CompletedTask;
        }

        var tasks = new List<Task>();
        foreach (var grain in _subs.Values)
        {
            tasks.Add(grain.Publish(message));
        }
        return Task.WhenAll(tasks);
    }
}
