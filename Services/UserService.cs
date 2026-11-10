using Azure.Data.Tables;
using CoffeeNChill.Api.Models;

namespace CoffeeNChill.Api.Services;

public interface IUserService
{
    Task<UserEntity?> FindByEmailAsync(string email);
    Task<UserEntity> CreateAsync(string email, string fullName, string password, string role);
    bool VerifyPassword(string password, string hash);
}

public class UserService : IUserService
{
    private readonly TableClient _table;
    private readonly ILogger<UserService> _log;

    public UserService(TableServiceClient svc, ILogger<UserService> log)
    {
        _log = log;
        var name = Environment.GetEnvironmentVariable("UsersTableName") ?? "Users";
        _table = svc.GetTableClient(name);
        _table.CreateIfNotExists();
    }

    public async Task<UserEntity?> FindByEmailAsync(string email)
    {
        // Email is the RowKey, but the role is the PartitionKey — and at login
        // we don't know the role yet, so this searches across all partitions.
        var filter = TableClient.CreateQueryFilter($"RowKey eq {email.ToLowerInvariant()}");

        await foreach (var user in _table.QueryAsync<UserEntity>(filter, maxPerPage: 1))
            return user;

        return null;
    }

    public async Task<UserEntity> CreateAsync(
        string email, string fullName, string password, string role)
    {
        var user = new UserEntity
        {
            
            PartitionKey = role,
            RowKey = email.ToLowerInvariant(),
            FullName = fullName,
            // Work factor 12 — slow enough to resist brute force, fast enough to log in.
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12),
            CreatedDate = DateTimeOffset.UtcNow
        };

        await _table.AddEntityAsync(user);
        _log.LogInformation("Registered {Email} with role {Role}", user.RowKey, role);
        return user;
    }

    public bool VerifyPassword(string password, string hash)
        => BCrypt.Net.BCrypt.Verify(password, hash);
}