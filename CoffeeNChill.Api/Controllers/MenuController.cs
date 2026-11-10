using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeNChill.Api.Controllers;

/// <summary>Menu items. Reading is public; changing requires Admin or Staff.</summary>
[ApiController]
[Route("api/menu")]
[Produces("application/json")]
public class MenuController : GatewayControllerBase
{
    public MenuController(IHttpClientFactory factory) : base(factory) { }

    /// <summary>Returns every menu item.</summary>
    /// <response code="200">The full menu.</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<IActionResult> GetAll()
        => ForwardAsync(HttpMethod.Get, "api/menu");

    /// <summary>Returns the items in one category.</summary>
    /// <param name="category">Category name, e.g. Hot Drinks.</param>
    /// <response code="200">Items in that category.</response>
    /// <response code="404">No items found in that category.</response>
    [HttpGet("category/{category}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetByCategory(string category)
        => ForwardAsync(HttpMethod.Get, $"api/menu/category/{Uri.EscapeDataString(category)}");

    /// <summary>Adds a menu item. Staff only.</summary>
    /// <response code="201">Item created.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">No token supplied.</response>
    /// <response code="403">Token does not carry the Admin or Staff role.</response>
    /// <response code="409">An item with that SKU already exists.</response>
    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public Task<IActionResult> Create([FromBody] object request)
        => ForwardAsync(HttpMethod.Post, "api/menu", request);

    /// <summary>Updates price or availability. Staff only.</summary>
    [HttpPut("{category}/{id}")]
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Update(string category, string id, [FromBody] object request)
        => ForwardAsync(HttpMethod.Put,
            $"api/menu/{Uri.EscapeDataString(category)}/{Uri.EscapeDataString(id)}", request);

    /// <summary>Removes a menu item. Staff only.</summary>
    [HttpDelete("{category}/{id}")]
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Delete(string category, string id)
        => ForwardAsync(HttpMethod.Delete,
            $"api/menu/{Uri.EscapeDataString(category)}/{Uri.EscapeDataString(id)}");
}