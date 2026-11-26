using System.Collections.Concurrent;
using FoodHub.Domain;

namespace FoodHub.Infrastructure;

/// <summary>
/// In-memory implementation of IReviewStore for development, testing, and seed data.
/// </summary>
public class InMemoryReviewStore : IReviewStore
{
    private readonly ConcurrentDictionary<Guid, Review> _reviews = new();

    public InMemoryReviewStore(IEnumerable<Review>? initialReviews = null)
    {
        if (initialReviews != null)
        {
            foreach (var review in initialReviews)
            {
                _reviews[review.Id] = review;
            }
        }
    }

    public Task<Review?> GetByIdAsync(Guid reviewId, CancellationToken cancellationToken = default)
    {
        _reviews.TryGetValue(reviewId, out var review);
        return Task.FromResult(review);
    }

    public Task<Review?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var review = _reviews.Values.FirstOrDefault(r => r.OrderId == orderId);
        return Task.FromResult(review);
    }

    public Task<IReadOnlyList<Review>> GetByRestaurantIdAsync(Guid restaurantId, CancellationToken cancellationToken = default)
    {
        var list = _reviews.Values.Where(r => r.RestaurantId == restaurantId).ToList();
        return Task.FromResult<IReadOnlyList<Review>>(list);
    }

    public Task<IReadOnlyList<Review>> GetByShipperIdAsync(Guid shipperId, CancellationToken cancellationToken = default)
    {
        var list = _reviews.Values
            .Where(r => r.ShipperId == shipperId && r.ShipperRating.HasValue)
            .ToList();
        return Task.FromResult<IReadOnlyList<Review>>(list);
    }

    public Task AddAsync(Review review, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(review);

        if (_reviews.Values.Any(r => r.OrderId == review.OrderId))
            throw new DomainException("A review already exists for this order.");

        _reviews[review.Id] = review;
        return Task.CompletedTask;
    }
}
