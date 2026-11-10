using Azure;
using Azure.Data.Tables;

namespace CoffeeNChill.Api.Models;

/// <summary>A registered user. PartitionKey is the role, RowKey is the email.</summary>
public class UserEntity : ITableEntity
{
    public string PartitionKey { get; set; } = default!;   // Role
    public string RowKey { get; set; } = default!;         // Email

    public string PasswordHash { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public DateTimeOffset CreatedDate { get; set; }

    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}