using System.ComponentModel.DataAnnotations;

namespace CoffeeNChill.Functions.Models;


public class MenuItemRequest
{
    [Required(ErrorMessage = "Category is required.")]
    public string? Category { get; set; }

    [Required(ErrorMessage = "Sku is required.")]
    [RegularExpression(@"^[A-Z]{3}-\d{3}$",
        ErrorMessage = "Sku must be three capital letters, a hyphen, then three digits (e.g. COF-001).")]
    public string? Sku { get; set; }

    [Required(ErrorMessage = "Name is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be 2 to 100 characters.")]
    public string? Name { get; set; }

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    public string? Description { get; set; }

    [Range(0.01, 10000, ErrorMessage = "Price must be greater than zero.")]
    public double Price { get; set; }

    public bool IsAvailable { get; set; } = true;
}