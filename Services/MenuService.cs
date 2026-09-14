using Azure;
using Azure.Data.Tables;
using CoffeeNChill.Functions.Models;
using Microsoft.Extensions.Logging;

namespace CoffeeNChill.Functions.Services;

public class MenuService : IMenuService
{
    private readonly TableClient _table;
    private readonly ILogger<MenuService> _log;

    public MenuService(TableServiceClient svc, ILogger<MenuService> log)
    {
        _log = log;
        var tableName = Environment.GetEnvironmentVariable("MenuTableName") ?? "MenuItems";
        _table = svc.GetTableClient(tableName);
        _table.CreateIfNotExists();
    }

    public async Task<MenuItemEntity> CreateAsync(MenuItemEntity item)
    {
        await _table.AddEntityAsync(item);
        _log.LogInformation("Created menu item {Sku} in category {Category}",
            item.RowKey, item.PartitionKey);
        return item;
    }

    public async Task<IReadOnlyList<MenuItemEntity>> GetAllAsync()
    {
        var results = new List<MenuItemEntity>();
        await foreach (var entity in _table.QueryAsync<MenuItemEntity>())
            results.Add(entity);

        _log.LogInformation("Retrieved {Count} menu items", results.Count);
        return results;
    }

    public async Task<IReadOnlyList<MenuItemEntity>> GetByCategoryAsync(string category)
    {
        // Single-partition query — the fastest read Table Storage offers.
        // CreateQueryFilter escapes the input, preventing filter injection.
        var filter = TableClient.CreateQueryFilter($"PartitionKey eq {category}");

        var results = new List<MenuItemEntity>();
        await foreach (var entity in _table.QueryAsync<MenuItemEntity>(filter))
            results.Add(entity);

        _log.LogInformation("Retrieved {Count} items from category {Category}",
            results.Count, category);
        return results;
    }

    public async Task<MenuItemEntity?> GetAsync(string category, string sku)
    {
        try
        {
            return await _table.GetEntityAsync<MenuItemEntity>(category, sku);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task<bool> UpdateAsync(MenuItemEntity item)
    {
        try
        {
            await _table.UpdateEntityAsync(item, ETag.All, TableUpdateMode.Replace);
            _log.LogInformation("Updated menu item {Sku}", item.RowKey);
            return true;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            _log.LogWarning("Update failed — {Sku} not found in {Category}",
                item.RowKey, item.PartitionKey);
            return false;
        }
    }

    public async Task<bool> DeleteAsync(string category, string sku)
    {
        var response = await _table.DeleteEntityAsync(category, sku);
        var deleted = response.Status is >= 200 and < 300;

        if (deleted) _log.LogInformation("Deleted menu item {Sku}", sku);
        return deleted;
    }
}