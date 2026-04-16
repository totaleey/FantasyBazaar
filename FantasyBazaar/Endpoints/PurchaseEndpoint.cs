using Microsoft.EntityFrameworkCore;
using FantasyBazaar.Api.Data;
using FantasyBazaar.Api.Models;
using FantasyBazaar.Api.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace FantasyBazaar.Api.Endpoints;

public static class PurchaseEndpoints
{
    public static void MapPurchaseEndpoints(this WebApplication app)
    {
        app.MapPost("/api/purchase", async (PurchaseRequest request, AppDbContext db, IHubContext<BazaarHub>? hubContext, ILogger<Program> logger) =>
        {
            // Validate request
            if (request.Quantity <= 0)
                return Results.BadRequest("Quantity must be positive");

            // Use database transaction with row lock - THIS IS YOUR CORRECTNESS GUARANTEE
            await using var transaction = await db.Database.BeginTransactionAsync();

            try
            {
                // SELECT ... FOR UPDATE - locks the row exclusively
                var item = await db.Items
                    .FromSqlRaw("SELECT * FROM \"Items\" WHERE \"Id\" = {0} FOR UPDATE", request.ItemId)
                    .FirstOrDefaultAsync();

                if (item == null)
                    return Results.NotFound($"Item {request.ItemId} not found");

                // Check stock
                if (item.Stock < request.Quantity)
                {
                    logger.LogWarning("Purchase failed: Item {ItemName} has only {Stock} left, requested {Quantity}",
                        item.Name, item.Stock, request.Quantity);
                    return Results.BadRequest($"Only {item.Stock} left in stock");
                }

                // Update stock
                var originalPrice = item.CurrentPrice;
                item.Stock -= request.Quantity;
                await db.SaveChangesAsync();

                // Record transaction
                var transactionRecord = new Transaction
                {
                    ItemId = item.Id,
                    ItemName = item.Name,
                    Quantity = request.Quantity,
                    PriceAtPurchase = originalPrice,
                    TotalAmount = originalPrice * request.Quantity,
                    Timestamp = DateTime.UtcNow,
                    BuyerType = request.BuyerType ?? "User",
                    TransactionId = Guid.NewGuid().ToString()
                };
                db.Transactions.Add(transactionRecord);
                await db.SaveChangesAsync();

                // Commit the transaction - this is where it becomes permanent
                await transaction.CommitAsync();

                logger.LogInformation("Purchase successful: {Quantity}x {ItemName} for {BuyerType}",
                    request.Quantity, item.Name, request.BuyerType ?? "User");

                // Broadcast real-time update via SignalR (if available - graceful degradation)
                if (hubContext != null)
                {
                    try
                    {
                        await BazaarHub.BroadcastStockUpdate(hubContext, item.Id, item.Name, item.Stock, item.CurrentPrice);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "SignalR broadcast failed - continuing anyway");
                    }
                }

                // Return success - using positional constructor for record
                return Results.Ok(new PurchaseResult(
                    Success: true,
                    ItemId: item.Id,
                    ItemName: item.Name,
                    QuantityPurchased: request.Quantity,
                    PricePaid: originalPrice,
                    TotalPaid: originalPrice * request.Quantity,
                    RemainingStock: item.Stock
                ));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                logger.LogError(ex, "Purchase failed for item {ItemId}", request.ItemId);
                return Results.StatusCode(500);
            }
        });
    }
}

// DTOs - defined as records with positional parameters
public record PurchaseRequest(int ItemId, int Quantity, string? BuyerType = "User");

public record PurchaseResult(
    bool Success,
    int ItemId,
    string ItemName,
    int QuantityPurchased,
    decimal PricePaid,
    decimal TotalPaid,
    int RemainingStock
);