namespace Orleans.PubSub;

public interface IPerSiloPubSubGrain : IGrainWithStringKey
{
    Task Publish(byte[] message);
}
