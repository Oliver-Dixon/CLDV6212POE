using System.Text.Json;
using CoffeeNChill.Functions.Models;
using CoffeeNChill.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace CoffeeNChill.Functions.Functions;

public class ProcessOrderQueue
{
    private readonly IOrderService _orders;
    private readonly ILogger<ProcessOrderQueue> _log;

    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNameCaseInsensitive = true };

    public ProcessOrderQueue(IOrderService orders, ILogger<ProcessOrderQueue> log)
    {
        _orders = orders;
        _log = log;
    }

    [Function("ProcessOrderQueue")]
    public async Task Run(
        [QueueTrigger("order-processing-queue", Connection = "AzureWebJobsStorage")]
        string message)
    {
        _log.LogInformation("Queue message received. {Bytes} bytes", message.Length);

        OrderMessage? order;
        try
        {
            order = JsonSerializer.Deserialize<OrderMessage>(message, JsonOpts);
        }
        catch (JsonException ex)
        {
            // Rethrow so the runtime retries, then moves it to the poison queue.
            _log.LogError(ex,
                "Malformed queue message could not be parsed. It will be retried and " +
                "then routed to order-processing-queue-poison. Payload: {Payload}", message);
            throw;
        }

        if (order is null || string.IsNullOrWhiteSpace(order.OrderId))
        {
            _log.LogError("Queue message parsed but contained no OrderId. Routing to poison queue.");
            throw new InvalidOperationException("Queue message missing OrderId.");
        }

        var orderDate = order.OrderTimestamp.ToString("yyyy-MM-dd");

        try
        {
            // 1. Received
            var entity = new OrderEntity
            {
                PartitionKey = orderDate,
                RowKey = order.OrderId,
                CustomerName = order.CustomerName,
                SelectedItemSKUs = string.Join(",", order.SelectedItemSKUs),
                TotalPrice = order.TotalPrice,
                Status = "Received",
                OrderTimestamp = order.OrderTimestamp,
                LastStatusChange = DateTimeOffset.UtcNow
            };

            await _orders.CreateAsync(entity);
            _log.LogInformation("Order {OrderId} logged as Received", order.OrderId);

            // 2. Simulate the barista lifecycle
            await AdvanceAsync(orderDate, order.OrderId, "Preparing", 1000);
            await AdvanceAsync(orderDate, order.OrderId, "Ready", 1000);
            await AdvanceAsync(orderDate, order.OrderId, "Collected", 1000);

            _log.LogInformation("Order {OrderId} completed the full lifecycle", order.OrderId);
        }
        catch (Exception ex)
        {
            _log.LogError(ex,
                "Processing failed for order {OrderId}. Message will be retried up to " +
                "maxDequeueCount, then moved to the poison queue.", order.OrderId);
            throw;
        }
    }

    private async Task AdvanceAsync(string date, string id, string status, int delayMs)
    {
        await Task.Delay(delayMs);
        await _orders.UpdateStatusAsync(date, id, status);
        _log.LogInformation("Order {OrderId} → {Status}", id, status);
    }
}