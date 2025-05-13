using MediatR;
using Microsoft.EntityFrameworkCore;
using TradingBot.Application.Common;
using TradingBot.Data;

namespace TradingBot.Application.Commands.UpdateBotOrder;

public record UpdateBotOrderCommand(OrderUpdate OrderUpdate) : IRequest<Result>
{
}

public class UpdateBotOrderCommandHandler(
    TradingBotDbContext dbContext,
    ILogger<UpdateBotOrderCommandHandler> logger,
    TimeProvider timeProvider) : BaseCommandHandler<UpdateBotOrderCommand>(logger)
{
    protected override async Task<Result> HandleCore(UpdateBotOrderCommand request, CancellationToken cancellationToken)
    {
        var orderUpdate = request.OrderUpdate;

        // Find the order in the database
        var order = await dbContext.Orders.FirstOrDefaultAsync(
            o => o.Id == orderUpdate.Id,
            cancellationToken);

        if (order == null)
        {
            logger.LogWarning("Order update received for unknown order ID: {OrderId}", orderUpdate.Id);
            return Result.Failure($"Order with ID {orderUpdate.Id} not found", ErrorCode.OrderNotFound);
        }

        // Update order properties
        order.QuantityFilled = orderUpdate.QuantityFilled;
        order.Status = orderUpdate.Status;
        order.LastUpdated = timeProvider.GetUtcNow().DateTime;

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
            order.Id, order.QuantityFilled, order.Quantity, order.Status);

        return Result.Success;
    }
}