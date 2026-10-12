using System.ComponentModel.DataAnnotations;

namespace CoffeeNChill.Functions.Models;

public class OrderRequest
{
    [Required(ErrorMessage = "CustomerName is required.")]
    [StringLength(100, MinimumLength = 2)]
    public string? CustomerName { get; set; }

    [Required(ErrorMessage = "At least one item SKU is required.")]
    [MinLength(1, ErrorMessage = "SelectedItemSKUs cannot be empty.")]
    public List<string>? SelectedItemSKUs { get; set; }

    [Range(0.01, 100000, ErrorMessage = "TotalPrice must be greater than zero.")]
    public double TotalPrice { get; set; }
}