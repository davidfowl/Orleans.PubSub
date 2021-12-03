namespace Orleans.PubSub;

public interface IPubSubProvider
{
    IPubSub Create(string topic);
}

