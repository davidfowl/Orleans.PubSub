namespace Orleans.PubSub;

public interface IPubSub
{
    Task PublishAsync(byte[] message);
    Task<IAsyncDisposable> SubscribeAsync(Func<byte[], Task> onMessage);
}
