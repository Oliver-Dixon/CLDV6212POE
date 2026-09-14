using System.ComponentModel.DataAnnotations;

namespace CoffeeNChill.Functions.Models;

public class MenuItemUpdateRequest
{
    [Range(0.01, 10000, ErrorMessage = "Price must be greater than zero.")]
    public double? Price { get; set; }

    public bool? IsAvailable { get; set; }

    [StringLength(100, MinimumLength = 2)]
    public string? Name { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }
}