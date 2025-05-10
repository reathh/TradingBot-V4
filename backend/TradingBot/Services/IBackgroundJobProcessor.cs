using MediatR;
using TradingBot.Application.Common;

namespace TradingBot.Services;

public interface IBackgroundJobProcessor
{
    void Enqueue<TRequest>(TRequest request) where TRequest : IRequest<Result>;
}
