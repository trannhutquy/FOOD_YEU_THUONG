using FoodHub.Domain;
using Xunit;

namespace FoodHub.Domain.Tests;

public class ReviewTests
{
    private static readonly DateTime Now = new(2026, 11, 26, 12, 0, 0, DateTimeKind.Utc);

    private static (Order Order, Guid CustomerId, Restaurant Restaurant, Shipper Shipper) CreateDeliveredOrderWithShipper()
    {
        var customerId = Guid.NewGuid();
        var restaurant = new Restaurant("Pho 24", Guid.NewGuid());
        var item = new MenuItem(restaurant.Id, "Pho Dac Biet", new Money(65000));
        var order = Order.Create(customerId, restaurant, new Address("12 Nguyen Trai", "P1", "Q5", "TPHCM"));
        order.AddItem(item, 1);
        order.Accept();
        order.StartPreparing();
        order.MarkReady();

        var shipper = new Shipper(Guid.NewGuid());
        var delivery = Delivery.Assign(order, shipper, Now);
        var code = delivery.StartDelivery(order, Now.AddMinutes(5));
        delivery.ConfirmDelivery(order, shipper, code, Now.AddMinutes(20));

        return (order, customerId, restaurant, shipper);
    }

    [Fact]
    public void Create_DeliveredOrderWithValidRatings_SucceedsAndMarksOrderReviewed()
    {
        var (order, customerId, restaurant, shipper) = CreateDeliveredOrderWithShipper();

        var review = Review.Create(
            order,
            customerId,
            restaurantRating: 5,
            restaurantComment: "Mon an rat ngon va dong goi can than!",
            shipperRating: 5,
            shipperComment: "Shipper giao hang rat nhanh va than thien.",
            utcNow: Now.AddMinutes(30));

        Assert.NotNull(review);
        Assert.NotEqual(Guid.Empty, review.Id);
        Assert.Equal(order.Id, review.OrderId);
        Assert.Equal(customerId, review.CustomerId);
        Assert.Equal(restaurant.Id, review.RestaurantId);
        Assert.Equal(shipper.Id, review.ShipperId);
        Assert.Equal(5, review.RestaurantRating);
        Assert.Equal("Mon an rat ngon va dong goi can than!", review.RestaurantComment);
        Assert.Equal(5, review.ShipperRating);
        Assert.Equal("Shipper giao hang rat nhanh va than thien.", review.ShipperComment);
        Assert.Equal(Now.AddMinutes(30), review.CreatedAtUtc);
        Assert.True(order.IsReviewed);
    }

    [Fact]
    public void Create_WithoutShipperReview_SucceedsWhenShipperAssigned()
    {
        var (order, customerId, restaurant, shipper) = CreateDeliveredOrderWithShipper();

        var review = Review.Create(
            order,
            customerId,
            restaurantRating: 4,
            restaurantComment: "Mon an tam on.");

        Assert.Equal(4, review.RestaurantRating);
        Assert.Equal("Mon an tam on.", review.RestaurantComment);
        Assert.Null(review.ShipperRating);
        Assert.Null(review.ShipperComment);
        Assert.True(order.IsReviewed);
    }

    [Fact]
    public void Create_ViaOrderAddReview_Succeeds()
    {
        var (order, customerId, _, _) = CreateDeliveredOrderWithShipper();

        var review = order.AddReview(
            customerId,
            restaurantRating: 5,
            restaurantComment: "Tuyet voi!");

        Assert.NotNull(review);
        Assert.Equal(order.Id, review.OrderId);
        Assert.True(order.IsReviewed);
    }

    [Theory]
    [InlineData(OrderStatus.Pending)]
    [InlineData(OrderStatus.Accepted)]
    [InlineData(OrderStatus.Preparing)]
    [InlineData(OrderStatus.ReadyForPickup)]
    [InlineData(OrderStatus.Delivering)]
    public void Create_WhenOrderNotDelivered_ThrowsDomainException(OrderStatus nonDeliveredStatus)
    {
        var customerId = Guid.NewGuid();
        var restaurant = new Restaurant("Quan An Ngon", Guid.NewGuid());
        var item = new MenuItem(restaurant.Id, "Com Ga", new Money(40000));
        var order = Order.Create(customerId, restaurant);
        order.AddItem(item, 1);

        if (nonDeliveredStatus != OrderStatus.Pending)
        {
            order.Accept();
            if (nonDeliveredStatus != OrderStatus.Accepted)
            {
                order.StartPreparing();
                if (nonDeliveredStatus != OrderStatus.Preparing)
                {
                    order.MarkReady();
                    if (nonDeliveredStatus == OrderStatus.Delivering)
                    {
                        var shipper = new Shipper(Guid.NewGuid());
                        var delivery = Delivery.Assign(order, shipper, Now);
                        delivery.StartDelivery(order, Now.AddMinutes(5));
                    }
                }
            }
        }

        Assert.Equal(nonDeliveredStatus, order.Status);

        var ex = Assert.Throws<DomainException>(() =>
            Review.Create(order, customerId, restaurantRating: 5));

        Assert.Equal("Only delivered orders can be reviewed.", ex.Message);
        Assert.False(order.IsReviewed);
    }

    [Fact]
    public void Create_WhenOrderCancelled_ThrowsDomainException()
    {
        var customerId = Guid.NewGuid();
        var restaurant = new Restaurant("Quan An Ngon", Guid.NewGuid());
        var item = new MenuItem(restaurant.Id, "Com Ga", new Money(40000));
        var order = Order.Create(customerId, restaurant);
        order.AddItem(item, 1);
        order.Cancel("Customer changed mind");

        var ex = Assert.Throws<DomainException>(() =>
            Review.Create(order, customerId, restaurantRating: 5));

        Assert.Equal("Only delivered orders can be reviewed.", ex.Message);
        Assert.False(order.IsReviewed);
    }

    [Fact]
    public void Create_WhenOrderAlreadyReviewed_ThrowsDomainException()
    {
        var (order, customerId, _, _) = CreateDeliveredOrderWithShipper();

        Review.Create(order, customerId, restaurantRating: 5, restaurantComment: "Lan 1");

        var ex = Assert.Throws<DomainException>(() =>
            Review.Create(order, customerId, restaurantRating: 4, restaurantComment: "Lan 2"));

        Assert.Equal("Order has already been reviewed.", ex.Message);
    }

    [Fact]
    public void Create_WhenCustomerIdDoesNotMatchOrder_ThrowsDomainException()
    {
        var (order, _, _, _) = CreateDeliveredOrderWithShipper();
        var anotherCustomerId = Guid.NewGuid();

        var ex = Assert.Throws<DomainException>(() =>
            Review.Create(order, anotherCustomerId, restaurantRating: 5));

        Assert.Equal("Only the customer who placed the order can review it.", ex.Message);
        Assert.False(order.IsReviewed);
    }

    [Fact]
    public void Create_WhenCustomerIdIsEmpty_ThrowsDomainException()
    {
        var (order, _, _, _) = CreateDeliveredOrderWithShipper();

        var ex = Assert.Throws<DomainException>(() =>
            Review.Create(order, Guid.Empty, restaurantRating: 5));

        Assert.Equal("Customer is required.", ex.Message);
    }

    [Fact]
    public void Create_WhenOrderIsNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            Review.Create(null!, Guid.NewGuid(), restaurantRating: 5));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(6)]
    public void Create_WithInvalidRestaurantRating_ThrowsDomainException(int invalidRating)
    {
        var (order, customerId, _, _) = CreateDeliveredOrderWithShipper();

        var ex = Assert.Throws<DomainException>(() =>
            Review.Create(order, customerId, restaurantRating: invalidRating));

        Assert.Equal("Restaurant rating must be between 1 and 5.", ex.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Create_WithInvalidShipperRating_ThrowsDomainException(int invalidRating)
    {
        var (order, customerId, _, _) = CreateDeliveredOrderWithShipper();

        var ex = Assert.Throws<DomainException>(() =>
            Review.Create(order, customerId, restaurantRating: 5, shipperRating: invalidRating));

        Assert.Equal("Shipper rating must be between 1 and 5.", ex.Message);
    }

    [Fact]
    public void Create_WithShipperComment_WithoutShipperRating_ThrowsDomainException()
    {
        var (order, customerId, _, _) = CreateDeliveredOrderWithShipper();

        var ex = Assert.Throws<DomainException>(() =>
            Review.Create(
                order,
                customerId,
                restaurantRating: 5,
                shipperRating: null,
                shipperComment: "Giao hang nhanh nhung khong danh gia sao"));

        Assert.Equal("Shipper rating is required when providing shipper feedback.", ex.Message);
    }

    [Fact]
    public void Create_WithShipperRating_WhenNoShipperAssigned_ThrowsDomainException()
    {
        var customerId = Guid.NewGuid();
        var restaurant = new Restaurant("Quan An", Guid.NewGuid());
        var order = Order.Restore(
            Guid.NewGuid(),
            customerId,
            restaurant.Id,
            OrderStatus.Delivered,
            shipperId: null);

        var ex = Assert.Throws<DomainException>(() =>
            Review.Create(order, customerId, restaurantRating: 5, shipperRating: 5));

        Assert.Equal("Cannot review a shipper when no shipper was assigned to the order.", ex.Message);
    }

    [Fact]
    public void Create_WithCommentExceedingMaxLength_ThrowsDomainException()
    {
        var (order, customerId, _, _) = CreateDeliveredOrderWithShipper();
        var longComment = new string('A', Review.MaxCommentLength + 1);

        var ex = Assert.Throws<DomainException>(() =>
            Review.Create(order, customerId, restaurantRating: 5, restaurantComment: longComment));

        Assert.Contains("comment cannot exceed", ex.Message);
    }

    [Fact]
    public void Restore_ReconstructsReviewProperly()
    {
        var id = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var restaurantId = Guid.NewGuid();
        var shipperId = Guid.NewGuid();

        var review = Review.Restore(
            id,
            orderId,
            customerId,
            restaurantId,
            shipperId,
            restaurantRating: 5,
            restaurantComment: "Com ga gion da",
            shipperRating: 4,
            shipperComment: "Giao nhanh",
            createdAtUtc: Now);

        Assert.Equal(id, review.Id);
        Assert.Equal(orderId, review.OrderId);
        Assert.Equal(customerId, review.CustomerId);
        Assert.Equal(restaurantId, review.RestaurantId);
        Assert.Equal(shipperId, review.ShipperId);
        Assert.Equal(5, review.RestaurantRating);
        Assert.Equal("Com ga gion da", review.RestaurantComment);
        Assert.Equal(4, review.ShipperRating);
        Assert.Equal("Giao nhanh", review.ShipperComment);
        Assert.Equal(Now, review.CreatedAtUtc);
    }

    [Fact]
    public void UpdateComments_UpdatesCommentsSuccessfully()
    {
        var (order, customerId, _, _) = CreateDeliveredOrderWithShipper();
        var review = Review.Create(
            order,
            customerId,
            restaurantRating: 5,
            restaurantComment: "Ngon",
            shipperRating: 5,
            shipperComment: "Tot");

        review.UpdateComments("Mon an cuc ky ngon", "Shipper 10 diem");

        Assert.Equal("Mon an cuc ky ngon", review.RestaurantComment);
        Assert.Equal("Shipper 10 diem", review.ShipperComment);
    }

    [Fact]
    public void UpdateComments_ThrowsWhenUpdatingShipperCommentWithoutShipperRating()
    {
        var (order, customerId, _, _) = CreateDeliveredOrderWithShipper();
        var review = Review.Create(
            order,
            customerId,
            restaurantRating: 5,
            restaurantComment: "Ngon");

        var ex = Assert.Throws<DomainException>(() =>
            review.UpdateComments("Ngon", "Shipper tot"));

        Assert.Equal("Cannot update shipper comment because shipper was not reviewed.", ex.Message);
    }
}
