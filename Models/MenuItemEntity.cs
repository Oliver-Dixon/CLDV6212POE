using Azure;
using Azure.Data.Tables;

namespace CoffeeNChill.Functions.Models;


public class MenuItemEntity : ITableEntity
{
    public string PartitionKey { get; set; } = default!;   // Category
    public string RowKey { get; set; } = default!;         // SKU

    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public double Price { get; set; }
    public bool IsAvailable { get; set; }

    // Managed by Azure — never set these yourself
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}