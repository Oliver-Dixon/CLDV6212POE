using CoffeeNChill.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeNChill.Api.Controllers;

/// <summary>Order placement and tracking.</summary>
[ApiController]
[Route("api/orders")]
[Produces("application/json")]
public class OrdersController : GatewayControllerBase
{
    public OrdersController(IHttpClientFactory factory) : base(factory) { }

    /// <summary>Places an order onto the processing queue.</summary>
    /// <remarks>Public — a student does not need an account to order.</remarks>
    /// <response code="202">Order accepted and queued.</response>
    /// <response code="400">Validation failed.</response>
    [HttpPost("queue")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> Queue([FromBody] object request)
        => ForwardAsync(HttpMethod.Post, "api/orders/queue", request);

    /// <summary>Lists every order. Staff only.</summary>
    [HttpGet]
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public Task<IActionResult> GetAll()
        => ForwardAsync(HttpMethod.Get, "api/orders");

    /// <summary>Advances an order's preparation status. Staff only.</summary>
    /// <response code="200">Status updated.</response>
    /// <response code="404">No such order.</response>
    [HttpPut("status")]
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> UpdateStatus([FromBody] OrderStatusUpdateRequest request)
    {
        if (!ModelState.IsValid)
            return Task.FromResult<IActionResult>(
                BadRequest(new { error = "OrderDate, OrderId and Status are required." }));

        return ForwardAsync(HttpMethod.Put, "api/orders/status", request);
    }
}