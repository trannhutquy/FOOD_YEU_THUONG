namespace FoodHub.Domain;

/// <summary>
/// Domain persistence port for storing and querying reviews.
/// </summary>
public interface IReviewStore
{
    Task<Review?> GetByIdAsync(Guid reviewId, CancellationToken cancellationToken = default);
    Task<Review?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Review>> GetByRestaurantIdAsync(Guid restaurantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Review>> GetByShipperIdAsync(Guid shipperId, CancellationToken cancellationToken = default);
    Task AddAsync(Review review, CancellationToken cancellationToken = default);
}
