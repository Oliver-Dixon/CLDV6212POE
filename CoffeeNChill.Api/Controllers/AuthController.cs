using CoffeeNChill.Api.Models;
using CoffeeNChill.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeNChill.Api.Controllers;

/// <summary>Registration and login.</summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IUserService _users;
    private readonly ITokenService _tokens;
    private readonly ILogger<AuthController> _log;

    public AuthController(IUserService users, ITokenService tokens, ILogger<AuthController> log)
    {
        _users = users;
        _tokens = tokens;
        _log = log;
    }

    /// <summary>Creates a new user account.</summary>
    /// <remarks>
    /// The password is hashed with BCrypt before it reaches storage.
    /// Role must be Admin, Staff or Customer.
    /// </remarks>
    /// <response code="201">Account created.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="409">An account with that email already exists.</response>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new
            {
                error = "Validation failed.",
                details = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
            });

        if (await _users.FindByEmailAsync(request.Email!) is not null)
            return Conflict(new { error = $"An account already exists for {request.Email}." });

        var user = await _users.CreateAsync(
            request.Email!, request.FullName!, request.Password!, request.Role!);

        return CreatedAtAction(nameof(Register), new UserResponse
        {
            Email = user.RowKey,
            FullName = user.FullName,
            Role = user.PartitionKey,
            CreatedDate = user.CreatedDate
        });
    }

    /// <summary>Exchanges credentials for a signed JWT.</summary>
    /// <remarks>
    /// Copy the returned token into the Authorize box at the top of this page
    /// to call the protected endpoints.
    /// </remarks>
    /// <response code="200">Authenticated. Token valid for two hours.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Email or password incorrect.</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { error = "Email and password are required." });

        var user = await _users.FindByEmailAsync(request.Email!);

        // Same message whether the email is unknown or the password is wrong —
        // otherwise this endpoint tells an attacker which accounts exist.
        if (user is null || !_users.VerifyPassword(request.Password!, user.PasswordHash))
        {
            _log.LogWarning("Failed login attempt for {Email}", request.Email);
            return Unauthorized(new { error = "Invalid email or password." });
        }

        var (token, expiresAt) = _tokens.CreateToken(
            user.RowKey, user.FullName, user.PartitionKey);

        _log.LogInformation("{Email} logged in as {Role}", user.RowKey, user.PartitionKey);

        return Ok(new AuthResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            Email = user.RowKey,
            FullName = user.FullName,
            Role = user.PartitionKey
        });
    }

    /// <summary>Returns the identity attached to the current token.</summary>
    /// <response code="200">Token is valid.</response>
    /// <response code="401">No token, or token expired.</response>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Me() => Ok(new
    {
        email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value,
        name = User.Identity?.Name,
        role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value
    });
}