using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TradingBot.Application.Common;
using TradingBot.Data;
using TradingBot.Services;

namespace TradingBot.Application.Commands.UpdateBotOrder;

public record UpdateBotOrderCommand(OrderUpdate OrderUpdate) : IRequest<Result>
{
}

public class UpdateBotOrderCommandHandler(
    TradingBotDbContext dbContext, 
    ILogger<UpdateBotOrderCommandHandler> logger, 
    TimeProvider timeProvider,
    TradingNotificationService notificationService)
    : BaseCommandHandler<UpdateBotOrderCommand>(logger)
{
    // Convenience constructor for unit tests that don't supply a TradingNotificationService
    internal UpdateBotOrderCommandHandler(
        TradingBotDbContext dbContext,
        ILogger<UpdateBotOrderCommandHandler> logger,
        TimeProvider timeProvider)
        : this(dbContext, logger, timeProvider, new NullTradingNotificationService())
    {
    }

    private sealed class NullTradingNotificationService : TradingNotificationService
    {
        public NullTradingNotificationService() : base(null!, NullLogger<TradingNotificationService>.Instance) {}

        public new Task NotifyOrderUpdated(string orderId) => Task.CompletedTask;
    }

    protected override async Task<Result> HandleCore(UpdateBotOrderCommand request, CancellationToken cancellationToken)
    {
        var orderUpdate = request.OrderUpdate;

        // Find the order in the database
        var order = await dbContext.Orders.FirstOrDefaultAsync(o => o.Id == orderUpdate.Id, cancellationToken);

        if (order == null)
        {
            logger.LogWarning("Order update received for unknown order ID: {OrderId}", orderUpdate.Id);

            return Result.Failure($"Order with ID {orderUpdate.Id} not found", ErrorCode.OrderNotFound);
        }

        // Check if the order is already filled or canceled
        if (order.Status is OrderStatus.Filled or OrderStatus.Canceled || request.OrderUpdate.QuantityFilled < order.QuantityFilled)
        {
            logger.LogDebug("Order update ignored: Order {OrderId} is {Status} with filled quantity {QuantityFilled}",
                order.Id,
                order.Status,
                order.QuantityFilled);

            return Result.Success;
        }

        // Update order properties
        order.QuantityFilled = orderUpdate.QuantityFilled;
        order.Status = orderUpdate.Status;

        order.LastUpdated = timeProvider.GetUtcNow()
            .DateTime;

        if (orderUpdate.AverageFillPrice.HasValue)
        {
            order.AverageFillPrice = orderUpdate.AverageFillPrice;
        }

        if (orderUpdate.Fee.HasValue)
        {
            // accumulate commission per fill
            order.Fee += orderUpdate.Fee.Value;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Updated order {OrderId}: Filled {QuantityFilled}/{Quantity}, Status: {Status}",
            order.Id,
            order.QuantityFilled,
            order.Quantity,
            order.Status);
            
        // Notify clients about the order update
        await notificationService.NotifyOrderUpdated(order.Id);

        return Result.Success;
    }
}