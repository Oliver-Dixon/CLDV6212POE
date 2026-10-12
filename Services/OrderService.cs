using Azure;
using Azure.Data.Tables;
using CoffeeNChill.Functions.Models;
using Microsoft.Extensions.Logging;

namespace CoffeeNChill.Functions.Services;

public class OrderService : IOrderService
{
    private readonly TableClient _table;
    private readonly ILogger<OrderService> _log;

    public OrderService(TableServiceClient svc, ILogger<OrderService> log)
    {
        _log = log;
        var name = Environment.GetEnvironmentVariable("OrdersTableName") ?? "Orders";
        _table = svc.GetTableClient(name);
        _table.CreateIfNotExists();
    }

    public async Task<OrderEntity> CreateAsync(OrderEntity order)
    {
        await _table.UpsertEntityAsync(order, TableUpdateMode.Replace);
        _log.LogInformation("Order {OrderId} written to Orders table with status {Status}",
            order.RowKey, order.Status);
        return order;
    }

    public async Task<bool> UpdateStatusAsync(string orderDate, string orderId, string status)
    {
        try
        {
            var entity = await _table.GetEntityAsync<OrderEntity>(orderDate, orderId);

            entity.Value.Status = status;
            entity.Value.LastStatusChange = DateTimeOffset.UtcNow;

            await _table.UpdateEntityAsync(entity.Value, ETag.All, TableUpdateMode.Replace);

            _log.LogInformation("Order {OrderId} status advanced to {Status}", orderId, status);
            return true;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            _log.LogWarning("Status update failed — order {OrderId} not found", orderId);
            return false;
        }
    }

    public async Task<OrderEntity?> GetAsync(string orderDate, string orderId)
    {
        try
        {
            return await _table.GetEntityAsync<OrderEntity>(orderDate, orderId);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<OrderEntity>> GetAllAsync()
    {
        var results = new List<OrderEntity>();
        await foreach (var e in _table.QueryAsync<OrderEntity>())
            results.Add(e);
        return results;
    }
}