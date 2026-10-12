using Azure;
using Azure.Data.Tables;

namespace CoffeeNChill.Functions.Models;

public class OrderEntity : ITableEntity
{
    public string PartitionKey { get; set; } = default!;   // OrderDate, e.g. 2026-10-12
    public string RowKey { get; set; } = default!;         // OrderId

    public string CustomerName { get; set; } = default!;
    public string SelectedItemSKUs { get; set; } = default!;  // comma separated
    public double TotalPrice { get; set; }
    public string Status { get; set; } = "Received";
    public DateTimeOffset OrderTimestamp { get; set; }
    public DateTimeOffset? LastStatusChange { get; set; }

    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}