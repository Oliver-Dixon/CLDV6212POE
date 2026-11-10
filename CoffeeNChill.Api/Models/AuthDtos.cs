using System.ComponentModel.DataAnnotations;

namespace CoffeeNChill.Api.Models;

/// <summary>Payload for creating a new user account.</summary>
public class RegisterRequest
{
    /// <summary>Email address, used as the unique account identifier.</summary>
    /// <example>barista@coffeenchill.ac.za</example>
    [Required, EmailAddress]
    public string? Email { get; set; }

    /// <summary>The user's display name.</summary>
    /// <example>Jane Smith</example>
    [Required, StringLength(100, MinimumLength = 2)]
    public string? FullName { get; set; }

    /// <summary>Plaintext password. Hashed with BCrypt before storage.</summary>
    [Required, StringLength(100, MinimumLength = 8,
        ErrorMessage = "Password must be at least 8 characters.")]
    public string? Password { get; set; }

    /// <summary>One of Admin, Staff or Customer.</summary>
    /// <example>Staff</example>
    [Required]
    [RegularExpression("^(Admin|Staff|Customer)$",
        ErrorMessage = "Role must be Admin, Staff or Customer.")]
    public string? Role { get; set; }
}

/// <summary>Credentials for obtaining a JWT.</summary>
public class LoginRequest
{
    [Required, EmailAddress]
    public string? Email { get; set; }

    [Required]
    public string? Password { get; set; }
}

/// <summary>A successful authentication result.</summary>
public class AuthResponse
{
    public string Token { get; set; } = default!;
    public DateTime ExpiresAt { get; set; }
    public string Email { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public string Role { get; set; } = default!;
}

/// <summary>A user account, without any password material.</summary>
public class UserResponse
{
    public string Email { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public string Role { get; set; } = default!;
    public DateTimeOffset CreatedDate { get; set; }
}