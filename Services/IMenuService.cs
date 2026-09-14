using CoffeeNChill.Functions.Models;

namespace CoffeeNChill.Functions.Services;

public interface IMenuService
{
    Task<MenuItemEntity> CreateAsync(MenuItemEntity item);
    Task<IReadOnlyList<MenuItemEntity>> GetAllAsync();
    Task<IReadOnlyList<MenuItemEntity>> GetByCategoryAsync(string category);
    Task<MenuItemEntity?> GetAsync(string category, string sku);
    Task<bool> UpdateAsync(MenuItemEntity item);
    Task<bool> DeleteAsync(string category, string sku);
}