using CoffeeNChill.Functions.Models;

namespace CoffeeNChill.Functions.Services;

public interface IQueueService
{
    Task<string> EnqueueOrderAsync(OrderMessage order);
}