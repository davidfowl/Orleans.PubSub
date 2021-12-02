using Orleans.Runtime;

namespace Orleans.PubSub;

public interface IPubSubGrain : IGrainWithStringKey
{
    Task AddSilo(SiloAddress siloAddress);
    Task RemoveSilo(SiloAddress siloAddress);
    Task Publish(byte[] message);
}
