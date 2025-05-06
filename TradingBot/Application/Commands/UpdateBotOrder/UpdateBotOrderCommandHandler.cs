using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TradingBot.Data;

namespace TradingBot.Application.Commands.UpdateBotOrder;

public class UpdateBotOrderCommandHandler : IRequestHandler<UpdateBotOrderCommand, bool>
{
    private readonly TradingBotDbContext _dbContext;
    private readonly ILogger<UpdateBotOrderCommandHandler> _logger;

    public UpdateBotOrderCommandHandler(
        TradingBotDbContext dbContext,
        ILogger<UpdateBotOrderCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<bool> Handle(UpdateBotOrderCommand request, CancellationToken cancellationToken)
    {
        var orderUpdate = request.OrderUpdate;

        try
        {
            // Find the order in the database
            var order = await _dbContext.Orders.FirstOrDefaultAsync(
                o => o.Id == orderUpdate.Id || o.ExchangeOrderId == orderUpdate.Id,
                cancellationToken);

            if (order == null)
            {
                _logger.LogWarning("Order update received for unknown order ID: {OrderId}", orderUpdate.Id);
                return false;
            }

            // Update order properties
            order.QuantityFilled = orderUpdate.QuantityFilled;
            order.Closed = orderUpdate.Closed;
            order.Canceled = orderUpdate.Canceled;

            if (orderUpdate.AverageFillPrice.HasValue)
            {
                order.AverageFillPrice = orderUpdate.AverageFillPrice;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Updated order {OrderId}: Filled {QuantityFilled}/{Quantity}, Closed: {Closed}",
                order.Id, order.QuantityFilled, order.Quantity, order.Closed);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating order {OrderId}", orderUpdate.Id);
            return false;
        }
    }
}