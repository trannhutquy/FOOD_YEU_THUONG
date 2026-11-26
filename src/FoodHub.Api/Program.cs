using FoodHub.Domain;
using FoodHub.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();

// Register domain stores seeded with DataSeeder
builder.Services.AddSingleton<IReviewStore>(DataSeeder.CreateSeededReviewStore());
builder.Services.AddSingleton<IShipperStore>(DataSeeder.CreateSeededShipperStore());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// --- Seed Data & Sample Accounts Endpoints ---
app.MapGet("/api/sample-accounts", () => Results.Ok(SampleAccounts.All))
    .WithName("GetSampleAccounts");

app.MapGet("/api/seed/summary", () => Results.Ok(DataSeeder.GetSummary()))
    .WithName("GetSeedSummary");

app.MapGet("/api/seed/restaurants", () => Results.Ok(DataSeeder.Restaurants))
    .WithName("GetSeedRestaurants");

app.MapGet("/api/seed/orders", () => Results.Ok(DataSeeder.Orders))
    .WithName("GetSeedOrders");

// --- Review Query Endpoints ---
app.MapGet("/api/reviews/order/{orderId:guid}", async (Guid orderId, IReviewStore store) =>
{
    var review = await store.GetByOrderIdAsync(orderId);
    return review is not null ? Results.Ok(review) : Results.NotFound(new { message = "Review not found for this order." });
})
.WithName("GetReviewByOrderId");

app.MapGet("/api/reviews/restaurant/{restaurantId:guid}", async (Guid restaurantId, IReviewStore store) =>
{
    var reviews = await store.GetByRestaurantIdAsync(restaurantId);
    return Results.Ok(reviews);
})
.WithName("GetReviewsByRestaurantId");

app.MapGet("/api/reviews/shipper/{shipperId:guid}", async (Guid shipperId, IReviewStore store) =>
{
    var reviews = await store.GetByShipperIdAsync(shipperId);
    return Results.Ok(reviews);
})
.WithName("GetReviewsByShipperId");

app.Run();
