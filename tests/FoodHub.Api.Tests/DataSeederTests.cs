using FoodHub.Domain;
using FoodHub.Infrastructure;
using Xunit;

namespace FoodHub.Api.Tests;

public class DataSeederTests
{
    [Fact]
    public void SampleAccounts_ContainsRequiredRoles_WithUniqueCredentials()
    {
        var accounts = SampleAccounts.All;

        Assert.NotEmpty(accounts);
        Assert.Contains(accounts, a => a.Role == UserRoles.Admin);
        Assert.Contains(accounts, a => a.Role == UserRoles.RestaurantOwner);
        Assert.Contains(accounts, a => a.Role == UserRoles.Shipper);
        Assert.Contains(accounts, a => a.Role == UserRoles.Customer);

        // All IDs must be unique
        var uniqueIds = accounts.Select(a => a.Id).Distinct();
        Assert.Equal(accounts.Count, uniqueIds.Count());

        // All emails must be unique
        var uniqueEmails = accounts.Select(a => a.Email.ToLowerInvariant()).Distinct();
        Assert.Equal(accounts.Count, uniqueEmails.Count());

        // Passwords and hashes must not be empty
        foreach (var account in accounts)
        {
            Assert.False(string.IsNullOrWhiteSpace(account.RawPassword));
            Assert.False(string.IsNullOrWhiteSpace(account.PasswordHash));
            Assert.False(string.IsNullOrWhiteSpace(account.FullName));
            Assert.False(string.IsNullOrWhiteSpace(account.PhoneNumber));
        }
    }

    [Fact]
    public void DataSeeder_HasReferentialIntegrity()
    {
        // 1. All restaurants have an owner that exists in Users
        foreach (var restaurant in DataSeeder.Restaurants)
        {
            Assert.Contains(DataSeeder.Users, u => u.Id == restaurant.OwnerUserId);
            Assert.False(string.IsNullOrWhiteSpace(restaurant.Name));
            Assert.True(restaurant.IsOpen);
        }

        // 2. All menu items point to existing restaurants
        foreach (var item in DataSeeder.MenuItems)
        {
            Assert.Contains(DataSeeder.Restaurants, r => r.Id == item.RestaurantId);
            Assert.True(item.Price.Amount > 0);
            Assert.True(item.IsAvailable);
        }

        // 3. All shippers point to existing users
        foreach (var shipper in DataSeeder.Shippers)
        {
            Assert.Contains(DataSeeder.Users, u => u.Id == shipper.UserId);
        }

        // 4. Sample orders cover all lifecycle statuses
        var statuses = DataSeeder.Orders.Select(o => o.Status).Distinct().ToList();
        Assert.Contains(OrderStatus.Delivered, statuses);
        Assert.Contains(OrderStatus.Delivering, statuses);
        Assert.Contains(OrderStatus.Preparing, statuses);
        Assert.Contains(OrderStatus.Pending, statuses);

        // 5. Sample review matches delivered order
        var review = Assert.Single(DataSeeder.Reviews);
        Assert.Equal(DataSeeder.OrderDeliveredReviewed.Id, review.OrderId);
        Assert.Equal(DataSeeder.OrderDeliveredReviewed.RestaurantId, review.RestaurantId);
        Assert.Equal(DataSeeder.OrderDeliveredReviewed.ShipperId, review.ShipperId);
        Assert.Equal(5, review.RestaurantRating);
        Assert.Equal(5, review.ShipperRating);
    }

    [Fact]
    public async Task InMemoryReviewStore_QueriesAndInsertsSuccessfully()
    {
        var store = DataSeeder.CreateSeededReviewStore();

        // Query existing review by order ID
        var existing = await store.GetByOrderIdAsync(DataSeeder.OrderDeliveredReviewed.Id);
        Assert.NotNull(existing);
        Assert.Equal(DataSeeder.SampleReview.Id, existing.Id);

        // Query reviews by restaurant
        var restaurantReviews = await store.GetByRestaurantIdAsync(DataSeeder.Restaurant1Id);
        Assert.NotEmpty(restaurantReviews);

        // Query reviews by shipper
        var shipperReviews = await store.GetByShipperIdAsync(DataSeeder.Shipper1Id);
        Assert.NotEmpty(shipperReviews);

        // Create a new review for the pending delivered order
        var newReview = DataSeeder.OrderDeliveredPendingReview.AddReview(
            DataSeeder.OrderDeliveredPendingReview.CustomerId,
            restaurantRating: 4,
            restaurantComment: "Bun bo dam da, topping day dan",
            shipperRating: 4,
            shipperComment: "Giao hang nhanh");

        await store.AddAsync(newReview);

        var retrieved = await store.GetByOrderIdAsync(DataSeeder.OrderDeliveredPendingReview.Id);
        Assert.NotNull(retrieved);
        Assert.Equal(4, retrieved.RestaurantRating);

        // Duplicate review throws domain exception
        await Assert.ThrowsAsync<DomainException>(() => store.AddAsync(newReview));
    }

    [Fact]
    public async Task InMemoryShipperStore_PreservesOptimisticConcurrency()
    {
        var store = DataSeeder.CreateSeededShipperStore();
        var shipper = await store.GetByIdAsync(DataSeeder.Shipper1Id);
        Assert.NotNull(shipper);

        var currentVersion = shipper.Version;

        // Change availability updates version
        shipper.SetAvailability(false);
        Assert.Equal(currentVersion + 1, shipper.Version);

        // Saving with expected version succeeds
        await store.SaveWithConcurrencyAsync(shipper, currentVersion + 1);

        // Saving with wrong version throws ConcurrencyException
        await Assert.ThrowsAsync<ConcurrencyException>(() =>
            store.SaveWithConcurrencyAsync(shipper, currentVersion));
    }
}
