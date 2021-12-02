namespace Orleans.PubSub;

public interface IPubSub
{
    Task PublishAsync(string topic, byte[] message);
    Task<IAsyncDisposable> SubscribeAsync(string topic, Func<byte[], Task> onMessage);
}
