namespace FoodHub.Domain;

/// <summary>
/// Strategy interface for choosing a shipper during automatic order assignment.
/// </summary>
public interface IShipperSelectionStrategy
{
    Shipper? SelectShipper(IEnumerable<Shipper> candidates);
}

/// <summary>
/// Default strategy: chooses the available shipper holding the fewest active orders.
/// </summary>
public sealed class LeastActiveOrdersShipperStrategy : IShipperSelectionStrategy
{
    public Shipper? SelectShipper(IEnumerable<Shipper> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);

        return candidates
            .Where(s => s.CanAcceptOrder())
            .OrderBy(s => s.ActiveOrderCount)
            .ThenBy(s => s.Id)
            .FirstOrDefault();
    }
}
