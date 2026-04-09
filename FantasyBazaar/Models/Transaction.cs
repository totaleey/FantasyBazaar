namespace FantasyBazaar.Api.Models;

public class Transaction
{
    public int Id { get; set; }
    public int ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal PriceAtPurchase { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime Timestamp { get; set; }
    public string BuyerType { get; set; } = "User";
    public string? TransactionId { get; set; }
}