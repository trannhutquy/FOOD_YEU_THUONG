namespace FoodHub.Domain;

/// <summary>
/// Represents a customer's review for a restaurant and/or shipper upon order delivery.
/// Rule: Reviews can only be submitted once per order and only after the order is Delivered.
/// </summary>
public class Review
{
    public const int MinRating = 1;
    public const int MaxRating = 5;
    public const int MaxCommentLength = 1000;

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid OrderId { get; private set; }
    public Guid CustomerId { get; private set; }
    public Guid RestaurantId { get; private set; }
    public Guid? ShipperId { get; private set; }

    public int RestaurantRating { get; private set; }
    public string? RestaurantComment { get; private set; }

    public int? ShipperRating { get; private set; }
    public string? ShipperComment { get; private set; }

    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;

    private Review() { }

    /// <summary>
    /// Creates a new review for a completed (Delivered) order.
    /// Marks the order as reviewed to enforce "mỗi đơn một lần" (one review per order).
    /// </summary>
    public static Review Create(
        Order order,
        Guid customerId,
        int restaurantRating,
        string? restaurantComment = null,
        int? shipperRating = null,
        string? shipperComment = null,
        DateTime? utcNow = null)
    {
        ArgumentNullException.ThrowIfNull(order);

        if (customerId == Guid.Empty)
            throw new DomainException("Customer is required.");

        if (order.CustomerId != customerId)
            throw new DomainException("Only the customer who placed the order can review it.");

        if (order.Status != OrderStatus.Delivered)
            throw new DomainException("Only delivered orders can be reviewed.");

        if (order.IsReviewed)
            throw new DomainException("Order has already been reviewed.");

        ValidateRating(restaurantRating, "Restaurant");
        var cleanRestaurantComment = SanitizeComment(restaurantComment, "Restaurant");

        int? validShipperRating = null;
        string? cleanShipperComment = null;

        if (shipperRating.HasValue)
        {
            if (order.ShipperId is null)
                throw new DomainException("Cannot review a shipper when no shipper was assigned to the order.");

            ValidateRating(shipperRating.Value, "Shipper");
            validShipperRating = shipperRating.Value;
            cleanShipperComment = SanitizeComment(shipperComment, "Shipper");
        }
        else if (!string.IsNullOrWhiteSpace(shipperComment))
        {
            throw new DomainException("Shipper rating is required when providing shipper feedback.");
        }

        order.MarkReviewed();

        return new Review
        {
            OrderId = order.Id,
            CustomerId = customerId,
            RestaurantId = order.RestaurantId,
            ShipperId = order.ShipperId,
            RestaurantRating = restaurantRating,
            RestaurantComment = cleanRestaurantComment,
            ShipperRating = validShipperRating,
            ShipperComment = cleanShipperComment,
            CreatedAtUtc = utcNow ?? DateTime.UtcNow
        };
    }

    /// <summary>
    /// Restores a review instance from storage without modifying the order.
    /// </summary>
    public static Review Restore(
        Guid id,
        Guid orderId,
        Guid customerId,
        Guid restaurantId,
        Guid? shipperId,
        int restaurantRating,
        string? restaurantComment,
        int? shipperRating,
        string? shipperComment,
        DateTime createdAtUtc)
    {
        if (id == Guid.Empty) throw new DomainException("Review id is required.");
        if (orderId == Guid.Empty) throw new DomainException("Order id is required.");
        if (customerId == Guid.Empty) throw new DomainException("Customer id is required.");
        if (restaurantId == Guid.Empty) throw new DomainException("Restaurant id is required.");
        ValidateRating(restaurantRating, "Restaurant");

        if (shipperRating.HasValue)
        {
            ValidateRating(shipperRating.Value, "Shipper");
        }

        return new Review
        {
            Id = id,
            OrderId = orderId,
            CustomerId = customerId,
            RestaurantId = restaurantId,
            ShipperId = shipperId,
            RestaurantRating = restaurantRating,
            RestaurantComment = SanitizeComment(restaurantComment, "Restaurant"),
            ShipperRating = shipperRating,
            ShipperComment = SanitizeComment(shipperComment, "Shipper"),
            CreatedAtUtc = createdAtUtc
        };
    }

    public void UpdateComments(string? newRestaurantComment, string? newShipperComment = null)
    {
        RestaurantComment = SanitizeComment(newRestaurantComment, "Restaurant");
        if (newShipperComment is not null)
        {
            if (!ShipperRating.HasValue)
                throw new DomainException("Cannot update shipper comment because shipper was not reviewed.");
            ShipperComment = SanitizeComment(newShipperComment, "Shipper");
        }
    }

    private static void ValidateRating(int rating, string target)
    {
        if (rating < MinRating || rating > MaxRating)
            throw new DomainException($"{target} rating must be between {MinRating} and {MaxRating}.");
    }

    private static string? SanitizeComment(string? comment, string target)
    {
        if (string.IsNullOrWhiteSpace(comment))
            return null;

        var trimmed = comment.Trim();
        if (trimmed.Length > MaxCommentLength)
            throw new DomainException($"{target} comment cannot exceed {MaxCommentLength} characters.");

        return trimmed;
    }
}
