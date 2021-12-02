
namespace Orleans.Runtime;

internal static class SiloAddressExtensions
{
    public static string Topic(this SiloAddress address, string topic)
    {
        // Is ToParsableString cached?
        var addressString = address.ToParsableString();
        if (topic.StartsWith(addressString))
        {
            return topic;
        }

        return $"{addressString}/{topic}";
    }
}