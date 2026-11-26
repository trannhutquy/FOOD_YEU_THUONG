using FoodHub.Application.Abstractions;
using FoodHub.Application.Common;
using FoodHub.Application.Orders;
using FoodHub.Domain;

namespace FoodHub.Application.Deliveries;

public class DeliveryService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly IDeliveryRepository _deliveryRepository;
    private readonly IShipperRepository _shipperRepository;
    private readonly IDeliveryCodeProtector _codeProtector;
    private readonly AutoAssignService _autoAssignService;
    private readonly IClock _clock;
    private readonly IUnitOfWork _unitOfWork;

    public DeliveryService(
        IOrderRepository orderRepository,
        IRestaurantRepository restaurantRepository,
        IDeliveryRepository deliveryRepository,
        IShipperRepository shipperRepository,
        IDeliveryCodeProtector codeProtector,
        AutoAssignService autoAssignService,
        IClock clock,
        IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _restaurantRepository = restaurantRepository;
        _deliveryRepository = deliveryRepository;
        _shipperRepository = shipperRepository;
        _codeProtector = codeProtector;
        _autoAssignService = autoAssignService;
        _clock = clock;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> UpdateAvailabilityAsync(
        Guid shipperUserId,
        UpdateAvailabilityRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var shipper = await GetShipperByUserOrThrowAsync(shipperUserId, cancellationToken);
        var expectedVersion = shipper.Version;

        shipper.SetAvailability(request.IsAvailable);
        if (shipper.Version != expectedVersion)
        {
            await _shipperRepository.SaveWithConcurrencyAsync(shipper, expectedVersion, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return shipper.IsAvailable;
    }

    public async Task<IReadOnlyList<AvailableOrderDto>> GetAvailableOrdersAsync(
        Guid shipperUserId,
        CancellationToken cancellationToken = default)
    {
        _ = await GetShipperByUserOrThrowAsync(shipperUserId, cancellationToken);
        var orders = await _orderRepository.GetAvailableForDeliveryAsync(cancellationToken);

        return orders.Select(o => new AvailableOrderDto(
            o.Id,
            o.RestaurantId,
            o.Status,
            o.DeliveryAddress is null
                ? null
                : new AddressDto(
                    o.DeliveryAddress.Street,
                    o.DeliveryAddress.Ward,
                    o.DeliveryAddress.District,
                    o.DeliveryAddress.City,
                    o.DeliveryAddress.Note),
            o.Total.Amount,
            o.CreatedAtUtc)).ToList();
    }

    public async Task<DeliveryStatusDto> AcceptDeliveryAsync(
        Guid shipperUserId,
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var shipper = await GetShipperByUserOrThrowAsync(shipperUserId, cancellationToken);
        var order = await GetOrderOrThrowAsync(orderId, cancellationToken);

        var delivery = await _autoAssignService.AcceptOrderAsync(order, shipper.Id, _clock.UtcNow, cancellationToken);
        await _deliveryRepository.AddAsync(delivery, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(delivery, order.Status);
    }

    public async Task<DeliveryStatusDto> PickUpOrderAsync(
        Guid shipperUserId,
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var shipper = await GetShipperByUserOrThrowAsync(shipperUserId, cancellationToken);
        var order = await GetOrderOrThrowAsync(orderId, cancellationToken);
        var delivery = await GetDeliveryOrThrowAsync(orderId, cancellationToken);

        if (delivery.ShipperId != shipper.Id)
            throw new ForbiddenException("Only the assigned shipper can pick up this order.");

        var plainCode = delivery.StartDelivery(order, _clock.UtcNow);
        delivery.Code!.SetProtectedCode(_codeProtector.Protect(plainCode));

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapToDto(delivery, order.Status);
    }

    public async Task<DeliveryStatusDto> ConfirmDeliveryAsync(
        Guid shipperUserId,
        Guid orderId,
        ConfirmDeliveryRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var shipper = await GetShipperByUserOrThrowAsync(shipperUserId, cancellationToken);
        var order = await GetOrderOrThrowAsync(orderId, cancellationToken);
        var delivery = await GetDeliveryOrThrowAsync(orderId, cancellationToken);

        if (delivery.ShipperId != shipper.Id)
            throw new ForbiddenException("Only the assigned shipper can confirm this delivery.");

        var expectedVersion = shipper.Version;

        try
        {
            delivery.ConfirmDelivery(order, shipper, request.Code, _clock.UtcNow);
        }
        catch (DomainException)
        {
            // Persist incremented FailedAttempts / IsLocked state even when verification fails.
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw;
        }

        await _shipperRepository.SaveWithConcurrencyAsync(shipper, expectedVersion, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(delivery, order.Status);
    }

    public async Task<DeliveryStatusDto> RegenerateCodeAsync(
        Guid actorUserId,
        string actorRole,
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await GetOrderOrThrowAsync(orderId, cancellationToken);

        if (actorRole != RoleNames.Admin)
        {
            if (actorRole != RoleNames.Restaurant)
                throw new ForbiddenException("Only restaurant owners or admins can regenerate a delivery code.");

            var restaurant = await _restaurantRepository.GetByIdAsync(order.RestaurantId, cancellationToken)
                ?? throw new NotFoundException("Restaurant not found.");

            if (restaurant.OwnerUserId != actorUserId)
                throw new ForbiddenException("You do not own the restaurant for this order.");
        }

        var delivery = await GetDeliveryOrThrowAsync(orderId, cancellationToken);
        var newPlainCode = delivery.RegenerateCode(_clock.UtcNow);
        delivery.Code!.SetProtectedCode(_codeProtector.Protect(newPlainCode));

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapToDto(delivery, order.Status);
    }

    private async Task<Shipper> GetShipperByUserOrThrowAsync(Guid userId, CancellationToken cancellationToken)
        => await _shipperRepository.GetByUserIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("Shipper profile not found.");

    private async Task<Order> GetOrderOrThrowAsync(Guid orderId, CancellationToken cancellationToken)
        => await _orderRepository.GetByIdAsync(orderId, cancellationToken)
            ?? throw new NotFoundException("Order not found.");

    private async Task<Delivery> GetDeliveryOrThrowAsync(Guid orderId, CancellationToken cancellationToken)
        => await _deliveryRepository.GetByOrderIdAsync(orderId, cancellationToken)
            ?? throw new NotFoundException("Delivery not found for this order.");

    private static DeliveryStatusDto MapToDto(Delivery delivery, OrderStatus orderStatus)
        => new(
            delivery.Id,
            delivery.OrderId,
            delivery.ShipperId,
            orderStatus,
            delivery.AssignedAtUtc,
            delivery.PickedUpAtUtc,
            delivery.DeliveredAtUtc,
            delivery.Code?.IsLocked ?? false,
            delivery.Code?.FailedAttempts ?? 0);
}
