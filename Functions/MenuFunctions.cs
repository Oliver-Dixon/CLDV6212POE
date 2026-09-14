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

public class MenuFunctions
{
    private readonly IMenuService _menu;
    private readonly ILogger<MenuFunctions> _log;

    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNameCaseInsensitive = true };

    public MenuFunctions(IMenuService menu, ILogger<MenuFunctions> log)
    {
        _menu = menu;
        _log = log;
    }

    // POST /api/menu
    [Function("CreateMenuItem")]
    public async Task<IActionResult> Create(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "menu")] HttpRequest req)
    {
        try
        {
            var body = await new StreamReader(req.Body).ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(body))
                return new BadRequestObjectResult(new { error = "Request body is empty." });

            MenuItemRequest? dto;
            try
            {
                dto = JsonSerializer.Deserialize<MenuItemRequest>(body, JsonOpts);
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
                return new BadRequestObjectResult(new { error = "Request body could not be parsed." });

            var validationErrors = Validate(dto);
            if (validationErrors.Count > 0)
                return new BadRequestObjectResult(new
                {
                    error = "Validation failed.",
                    details = validationErrors
                });

            if (await _menu.GetAsync(dto.Category!, dto.Sku!) is not null)
                return new ConflictObjectResult(new
                {
                    error = $"Menu item '{dto.Sku}' already exists in category '{dto.Category}'."
                });

            var entity = new MenuItemEntity
            {
                PartitionKey = dto.Category!,
                RowKey = dto.Sku!,
                Name = dto.Name!,
                Description = dto.Description ?? string.Empty,
                Price = dto.Price,
                IsAvailable = dto.IsAvailable
            };

            await _menu.CreateAsync(entity);

            return new CreatedResult($"/api/menu/{entity.PartitionKey}/{entity.RowKey}", entity);
        }
        catch (RequestFailedException ex)
        {
            _log.LogError(ex, "Storage failure while creating a menu item");
            return new ObjectResult(new { error = "Storage service unavailable." })
            { StatusCode = 503 };
        }
    }

    // GET /api/menu
    [Function("GetAllMenuItems")]
    public async Task<IActionResult> GetAll(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "menu")] HttpRequest req)
    {
        try
        {
            var items = await _menu.GetAllAsync();
            return new OkObjectResult(items);
        }
        catch (RequestFailedException ex)
        {
            _log.LogError(ex, "Storage failure while listing menu items");
            return new ObjectResult(new { error = "Storage service unavailable." })
            { StatusCode = 503 };
        }
    }

    // GET /api/menu/category/{category}
    [Function("GetMenuItemsByCategory")]
    public async Task<IActionResult> GetByCategory(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "menu/category/{category}")]
        HttpRequest req,
        string category)
    {
        if (string.IsNullOrWhiteSpace(category))
            return new BadRequestObjectResult(new { error = "Category is required." });

        try
        {
            var items = await _menu.GetByCategoryAsync(category);

            if (items.Count == 0)
                return new NotFoundObjectResult(new
                {
                    error = $"No menu items found in category '{category}'."
                });

            return new OkObjectResult(items);
        }
        catch (RequestFailedException ex)
        {
            _log.LogError(ex, "Storage failure while filtering by category {Category}", category);
            return new ObjectResult(new { error = "Storage service unavailable." })
            { StatusCode = 503 };
        }
    }

    private static List<string> Validate(object dto)
    {
        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(dto, context, results, validateAllProperties: true);
        return results.Select(r => r.ErrorMessage ?? "Invalid value.").ToList();
    }
}