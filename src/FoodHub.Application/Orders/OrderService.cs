using FoodHub.Application.Abstractions;
using FoodHub.Domain;

namespace FoodHub.Application.Orders;

public class OrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly IDeliveryRepository _deliveryRepository;
    private readonly IShipperRepository _shipperRepository;
    private readonly IDeliveryCodeProtector _codeProtector;
    private readonly AutoAssignService _autoAssignService;
    private readonly IClock _clock;
    private readonly IUnitOfWork _unitOfWork;

    public OrderService(
        IOrderRepository orderRepository,
        IRestaurantRepository restaurantRepository,
        IMenuItemRepository menuItemRepository,
        IDeliveryRepository deliveryRepository,
        IShipperRepository shipperRepository,
        IDeliveryCodeProtector codeProtector,
        AutoAssignService autoAssignService,
        IClock clock,
        IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _restaurantRepository = restaurantRepository;
        _menuItemRepository = menuItemRepository;
        _deliveryRepository = deliveryRepository;
        _shipperRepository = shipperRepository;
        _codeProtector = codeProtector;
        _autoAssignService = autoAssignService;
        _clock = clock;
        _unitOfWork = unitOfWork;
    }

    public async Task<OrderDetailsDto> CreateOrderAsync(
        Guid customerId,
        CreateOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Items is null || request.Items.Count == 0)
            throw new DomainException("Order must contain at least one item.");

        var restaurant = await _restaurantRepository.GetByIdAsync(request.RestaurantId, cancellationToken)
            ?? throw new NotFoundException("Restaurant not found.");

        var address = new Address(
            request.DeliveryAddress.Street,
            request.DeliveryAddress.Ward,
            request.DeliveryAddress.District,
            request.DeliveryAddress.City,
            request.DeliveryAddress.Note);

        var order = Order.Create(customerId, restaurant, address, new Money(request.ShippingFee));

        var menuIds = request.Items.Select(i => i.MenuItemId).Distinct().ToList();
        var menuItems = (await _menuItemRepository.GetByIdsAsync(menuIds, cancellationToken))
            .ToDictionary(m => m.Id);

        foreach (var itemReq in request.Items)
        {
            if (!menuItems.TryGetValue(itemReq.MenuItemId, out var menuItem))
                throw new NotFoundException($"Menu item '{itemReq.MenuItemId}' not found.");

            order.AddItem(menuItem, itemReq.Quantity);
        }

        await _orderRepository.AddAsync(order, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(order, deliveryConfirmationCode: null);
    }

    public async Task<OrderDetailsDto> GetOrderByIdAsync(
        Guid actorUserId,
        string actorRole,
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await GetOrderOrThrowAsync(orderId, cancellationToken);
        await EnsureCanViewOrderAsync(actorUserId, actorRole, order, cancellationToken);

        string? deliveryCode = null;
        if (string.Equals(actorRole, UserRoles.Customer, StringComparison.OrdinalIgnoreCase) &&
            order.CustomerId == actorUserId &&
            order.Status == OrderStatus.Delivering)
        {
            var delivery = await _deliveryRepository.GetByOrderIdAsync(order.Id, cancellationToken);
            if (!string.IsNullOrEmpty(delivery?.Code?.ProtectedCode))
                deliveryCode = _codeProtector.Unprotect(delivery.Code.ProtectedCode);
        }

        return MapToDto(order, deliveryCode);
    }

    public async Task<OrderDetailsDto> CancelOrderAsync(
        Guid customerId,
        Guid orderId,
        CancelOrderRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var order = await GetOrderOrThrowAsync(orderId, cancellationToken);
        if (order.CustomerId != customerId)
            throw new ForbiddenException("Only the customer who placed this order can cancel it.");

        order.Cancel(request?.Reason);

        if (order.ShipperId.HasValue)
        {
            var shipper = await _shipperRepository.GetByIdAsync(order.ShipperId.Value, cancellationToken);
            if (shipper is not null)
            {
                var expectedVersion = shipper.Version;
                shipper.ReleaseOrder(order);
                await _shipperRepository.SaveWithConcurrencyAsync(shipper, expectedVersion, cancellationToken);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapToDto(order, deliveryConfirmationCode: null);
    }

    public async Task<OrderDetailsDto> AcceptOrderAsync(
        Guid ownerUserId,
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await GetOrderOwnedByRestaurantAsync(ownerUserId, orderId, cancellationToken);
        order.Accept();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapToDto(order, deliveryConfirmationCode: null);
    }

    public async Task<OrderDetailsDto> RejectOrderAsync(
        Guid ownerUserId,
        Guid orderId,
        RejectOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var order = await GetOrderOwnedByRestaurantAsync(ownerUserId, orderId, cancellationToken);
        order.Reject(request.Reason);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapToDto(order, deliveryConfirmationCode: null);
    }

    public async Task<OrderDetailsDto> StartPreparingAsync(
        Guid ownerUserId,
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await GetOrderOwnedByRestaurantAsync(ownerUserId, orderId, cancellationToken);
        order.StartPreparing();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapToDto(order, deliveryConfirmationCode: null);
    }

    public async Task<OrderDetailsDto> MarkReadyAsync(
        Guid ownerUserId,
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await GetOrderOwnedByRestaurantAsync(ownerUserId, orderId, cancellationToken);
        var delivery = await _autoAssignService.MarkReadyWithAutoAssignAsync(order, _clock.UtcNow, cancellationToken);

        if (delivery is not null)
            await _deliveryRepository.AddAsync(delivery, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapToDto(order, deliveryConfirmationCode: null);
    }

    private async Task<Order> GetOrderOrThrowAsync(Guid orderId, CancellationToken cancellationToken)
        => await _orderRepository.GetByIdAsync(orderId, cancellationToken)
            ?? throw new NotFoundException("Order not found.");

    private async Task<Order> GetOrderOwnedByRestaurantAsync(
        Guid ownerUserId,
        Guid orderId,
        CancellationToken cancellationToken)
    {
        var order = await GetOrderOrThrowAsync(orderId, cancellationToken);
        var restaurant = await _restaurantRepository.GetByIdAsync(order.RestaurantId, cancellationToken)
            ?? throw new NotFoundException("Restaurant not found.");

        if (restaurant.OwnerUserId != ownerUserId)
            throw new ForbiddenException("You do not own the restaurant for this order.");

        return order;
    }

    private async Task EnsureCanViewOrderAsync(
        Guid actorUserId,
        string actorRole,
        Order order,
        CancellationToken cancellationToken)
    {
        if (string.Equals(actorRole, UserRoles.Admin, StringComparison.OrdinalIgnoreCase))
            return;

        if (string.Equals(actorRole, UserRoles.Customer, StringComparison.OrdinalIgnoreCase) && order.CustomerId == actorUserId)
            return;

        if (string.Equals(actorRole, UserRoles.Restaurant, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(actorRole, UserRoles.RestaurantOwner, StringComparison.OrdinalIgnoreCase))
        {
            var restaurant = await _restaurantRepository.GetByIdAsync(order.RestaurantId, cancellationToken);
            if (restaurant is not null && restaurant.OwnerUserId == actorUserId)
                return;
        }

        if (string.Equals(actorRole, UserRoles.Shipper, StringComparison.OrdinalIgnoreCase))
        {
            var shipper = await _shipperRepository.GetByUserIdAsync(actorUserId, cancellationToken);
            if (shipper is not null && order.ShipperId == shipper.Id)
                return;
        }

        throw new ForbiddenException("You do not have permission to view this order.");
    }

    private static OrderDetailsDto MapToDto(Order order, string? deliveryConfirmationCode)
    {
        var addressDto = order.DeliveryAddress is null
            ? null
            : new AddressDto(
                order.DeliveryAddress.Street,
                order.DeliveryAddress.Ward,
                order.DeliveryAddress.District,
                order.DeliveryAddress.City,
                order.DeliveryAddress.Note);

        var items = order.Items
            .Select(i => new OrderItemDto(i.MenuItemId, i.Name, i.UnitPrice.Amount, i.Quantity, i.LineTotal.Amount))
            .ToList();

        return new OrderDetailsDto(
            order.Id,
            order.CustomerId,
            order.RestaurantId,
            order.Status,
            order.ShipperId,
            addressDto,
            items,
            order.Subtotal.Amount,
            order.ShippingFee.Amount,
            order.DiscountAmount.Amount,
            order.Total.Amount,
            order.AppliedVoucherCode,
            order.RejectionReason,
            order.CancellationReason,
            deliveryConfirmationCode,
            order.CreatedAtUtc);
    }
}
