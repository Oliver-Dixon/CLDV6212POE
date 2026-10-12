using CoffeeNChill.Functions.Models;

namespace CoffeeNChill.Functions.Services;

public interface IOrderService
{
    Task<OrderEntity> CreateAsync(OrderEntity order);
    Task<bool> UpdateStatusAsync(string orderDate, string orderId, string status);
    Task<OrderEntity?> GetAsync(string orderDate, string orderId);
    Task<IReadOnlyList<OrderEntity>> GetAllAsync();
}