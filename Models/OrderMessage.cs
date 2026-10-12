namespace CoffeeNChill.Functions.Models;

public class OrderMessage
{
    public string OrderId { get; set; } = default!;
    public string CustomerName { get; set; } = default!;
    public List<string> SelectedItemSKUs { get; set; } = new();
    public double TotalPrice { get; set; }
    public DateTimeOffset OrderTimestamp { get; set; }
}