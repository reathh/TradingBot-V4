using MediatR;
using TradingBot.Data;

namespace TradingBot.Application.Commands.UpdateBotOrder;

public record UpdateBotOrderCommand(OrderUpdate OrderUpdate) : IRequest<bool>;