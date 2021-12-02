using System.Runtime.CompilerServices;
using Orleans.Runtime;
using Orleans.Runtime.Placement;

namespace Orleans.Placement;

/// <summary>
/// The grain identity is prefixed with the silo address so we place that grain on the target silo
/// </summary>
public class FixedPlacementDirector : IPlacementDirector
{
    public virtual Task<SiloAddress> OnAddActivation(
        PlacementStrategy strategy, PlacementTarget target, IPlacementContext context)
    {
        var targetSilo = FixedPlacement.ParseSiloAddress(target.GrainIdentity);
        var allSilos = context.GetCompatibleSilos(target);
        var found = false;
        foreach (var silo in allSilos)
        {
            if (silo.Equals(targetSilo))
            {
                found = true;
                break;
            }
        }

        if (!found)
        {
            ThrowSiloUnavailable(target, targetSilo);
        }

        return Task.FromResult(targetSilo);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ThrowSiloUnavailable(PlacementTarget target, SiloAddress targetSilo) => throw new SiloUnavailableException($"The silo {targetSilo} for grain {target.GrainIdentity} is not available");
    }
}
