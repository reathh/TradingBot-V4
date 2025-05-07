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
    private readonly TradingBotDbContext _dbContext = dbContext;
    private readonly ILogger<UpdateBotOrderCommandHandler> _logger = logger;
    private readonly TimeProvider _timeProvider = timeProvider;

    protected override async Task<Result> HandleCore(UpdateBotOrderCommand request, CancellationToken cancellationToken)
    {
        var orderUpdate = request.OrderUpdate;

        // Find the order in the database
        var order = await _dbContext.Orders.FirstOrDefaultAsync(
            o => o.Id == orderUpdate.Id,
            cancellationToken);

        if (order == null)
        {
            _logger.LogWarning("Order update received for unknown order ID: {OrderId}", orderUpdate.Id);
            return $"Order with ID {orderUpdate.Id} not found";
        }

        // Update order properties
        order.QuantityFilled = orderUpdate.QuantityFilled;
        order.Closed = orderUpdate.Closed;
        order.Canceled = orderUpdate.Canceled;
        order.LastUpdated = _timeProvider.GetUtcNow().DateTime;

        if (orderUpdate.AverageFillPrice.HasValue)
        {
            order.AverageFillPrice = orderUpdate.AverageFillPrice;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated order {OrderId}: Filled {QuantityFilled}/{Quantity}, Closed: {Closed}",
            order.Id, order.QuantityFilled, order.Quantity, order.Closed);

        return Result.Success;
    }
}