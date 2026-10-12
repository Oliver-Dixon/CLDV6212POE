using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Azure;
using CoffeeNChill.Functions.Models;
using CoffeeNChill.Functions.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace CoffeeNChill.Functions.Functions;

public class OrderFunctions
{
    private readonly IQueueService _queue;
    private readonly ILogger<OrderFunctions> _log;

    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNameCaseInsensitive = true };

    public OrderFunctions(IQueueService queue, ILogger<OrderFunctions> log)
    {
        _queue = queue;
        _log = log;
    }

    // POST /api/orders/queue
    [Function("QueueOrder")]
    public async Task<IActionResult> QueueOrder(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "orders/queue")]
        HttpRequest req)
    {
        try
        {
            var body = await new StreamReader(req.Body).ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(body))
                return new BadRequestObjectResult(new { error = "Request body is empty." });

            OrderRequest? dto;
            try
            {
                dto = JsonSerializer.Deserialize<OrderRequest>(body, JsonOpts);
            }
            catch (JsonException ex)
            {
                return new BadRequestObjectResult(new
                {
                    error = "Malformed JSON.",
                    detail = ex.Message
                });
            }

            if (dto is null)
                return new BadRequestObjectResult(new { error = "Body could not be parsed." });

            var context = new ValidationContext(dto);
            var results = new List<ValidationResult>();

            if (!Validator.TryValidateObject(dto, context, results, true))
                return new BadRequestObjectResult(new
                {
                    error = "Validation failed.",
                    details = results.Select(r => r.ErrorMessage)
                });

            var order = new OrderMessage
            {
                OrderId = $"ORD-{DateTime.UtcNow:yyyy}-{Random.Shared.Next(1000, 9999)}",
                CustomerName = dto.CustomerName!,
                SelectedItemSKUs = dto.SelectedItemSKUs!,
                TotalPrice = dto.TotalPrice,
                OrderTimestamp = DateTimeOffset.UtcNow
            };

            var messageId = await _queue.EnqueueOrderAsync(order);

            // 202 Accepted — the work has not been done yet, only accepted.
            return new AcceptedResult($"/api/orders/{order.OrderId}", new
            {
                message = "Order accepted and queued for processing.",
                orderId = order.OrderId,
                messageId,
                status = "Queued",
                queuedAt = order.OrderTimestamp
            });
        }
        catch (RequestFailedException ex)
        {
            _log.LogError(ex, "Queue storage failure while placing an order");
            return new ObjectResult(new { error = "Queue service unavailable." })
            { StatusCode = 503 };
        }
    }
}