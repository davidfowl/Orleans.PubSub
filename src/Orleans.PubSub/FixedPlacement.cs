using Orleans.Core;
using Orleans.Runtime;

namespace Orleans.Placement;

[Serializable]
public class FixedPlacement : PlacementStrategy
{
    private const char SegmentSeparator = '/';
    internal static FixedPlacement Singleton { get; } = new FixedPlacement();

    public override bool IsUsingGrainDirectory => false;

    internal static SiloAddress ParseSiloAddress(IGrainIdentity grainIdentity)
    {
        grainIdentity.GetPrimaryKey(out var key);
        if (key.IndexOf(SegmentSeparator) is int index && index >= 0)
        {
            key = key[..index];
        }

        return SiloAddress.FromParsableString(key);
    }
}
