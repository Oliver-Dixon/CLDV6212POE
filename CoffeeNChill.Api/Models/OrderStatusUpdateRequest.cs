using System.ComponentModel.DataAnnotations;

namespace CoffeeNChill.Api.Models;

/// <summary>Instruction to move an order to a new preparation status.</summary>
public class OrderStatusUpdateRequest
{
    /// <summary>The order's date partition.</summary>
    /// <example>2026-11-10</example>
    [Required]
    public string? OrderDate { get; set; }

    /// <summary>The order identifier.</summary>
    /// <example>ORD-2026-8801</example>
    [Required]
    public string? OrderId { get; set; }

    /// <summary>Received, Preparing, Ready or Collected.</summary>
    /// <example>Ready</example>
    [Required]
    [RegularExpression("^(Received|Preparing|Ready|Collected)$",
        ErrorMessage = "Status must be Received, Preparing, Ready or Collected.")]
    public string? Status { get; set; }
}