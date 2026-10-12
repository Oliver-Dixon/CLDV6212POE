using System.Text.Json;
using Azure.Storage.Queues;
using CoffeeNChill.Functions.Models;
using Microsoft.Extensions.Logging;

namespace CoffeeNChill.Functions.Services;

public class QueueService : IQueueService
{
    private readonly QueueClient _queue;
    private readonly ILogger<QueueService> _log;

    public QueueService(ILogger<QueueService> log)
    {
        _log = log;

        var conn = Environment.GetEnvironmentVariable("AzureWebJobsStorage")
                   ?? "UseDevelopmentStorage=true";
        var name = Environment.GetEnvironmentVariable("OrderQueueName")
                   ?? "order-processing-queue";

        // Base64 is essential. The SDK sends plain text by default but the
        // [QueueTrigger] attribute expects Base64, so without this the
        // trigger fires and immediately fails to parse the message.
        _queue = new QueueClient(conn, name, new QueueClientOptions
        {
            MessageEncoding = QueueMessageEncoding.Base64
        });

        _queue.CreateIfNotExists();
    }

    public async Task<string> EnqueueOrderAsync(OrderMessage order)
    {
        var json = JsonSerializer.Serialize(order);

        var receipt = await _queue.SendMessageAsync(json);

        _log.LogInformation(
            "Order {OrderId} queued. MessageId {MessageId}, {Bytes} bytes",
            order.OrderId, receipt.Value.MessageId, json.Length);

        return receipt.Value.MessageId;
    }
}