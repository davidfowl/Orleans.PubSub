using Orleans.Placement;

namespace Orleans.Placement;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class FixedPlacementAttribute : PlacementAttribute
{
    public FixedPlacementAttribute() :
        base(FixedPlacement.Singleton)
    {
    }
}
