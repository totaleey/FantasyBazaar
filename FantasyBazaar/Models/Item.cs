namespace FantasyBazaar.Api.Models;

public class Item
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public int Stock { get; set; }
    public int PopularityScore { get; set; }
    public DateTime LastPriceUpdate { get; set; }
    public bool IsSpecial { get; set; }
}