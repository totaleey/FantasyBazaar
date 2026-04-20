using Microsoft.EntityFrameworkCore;
using FantasyBazaar.Api.Data;

namespace FantasyBazaar.Api.BackgroundServices;

public class ReplenishmentService : BackgroundService
{
    private readonly ILogger<ReplenishmentService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly Random _random = new();

    // config
    private readonly int _replenishIntervalMinutes = 1;

    public ReplenishmentService(
        ILogger<ReplenishmentService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Replenishment Service started - running every {Interval} minutes", _replenishIntervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(_replenishIntervalMinutes), stoppingToken);
                await ReplenishInventory(stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Replenishment cycle failed");
            }
        }

        _logger.LogInformation("Replenishment Service stopped");
    }

    private async Task ReplenishInventory(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var items = await db.Items.ToListAsync(stoppingToken);
        var replenishedCount = 0;

        foreach (var item in items)
        {
            var originalStock = GetOriginalStock(item.Id);
            var currentStock = item.Stock;

            if (currentStock >= originalStock)
                continue;

            // Calculate restock amount based on popularity
            var restockAmount = CalculateRestockAmount(item.PopularityScore, originalStock);

            if (restockAmount > 0)
            {
                var newStock = Math.Min(currentStock + restockAmount, originalStock);
                var actualRestock = newStock - currentStock;

                item.Stock = newStock;
                replenishedCount++;

                _logger.LogInformation("📦 Replenished {ItemName}: +{Amount} stock (Popularity: {Popularity}) → New stock: {NewStock}",
                    item.Name, actualRestock, item.PopularityScore, newStock);
            }
        }

        await db.SaveChangesAsync(stoppingToken);

        if (replenishedCount > 0)
            _logger.LogInformation("Replenishment complete - Restocked {Count} items", replenishedCount);
        else
            _logger.LogDebug("Replenishment cycle - No items needed restocking");
    }

    private int CalculateRestockAmount(int popularityScore, int originalStock)
    {
        double multiplier;

        if (popularityScore >= 80)
            multiplier = _random.NextDouble() * 0.2 + 0.8;
        else if (popularityScore >= 50)
            multiplier = _random.NextDouble() * 0.3 + 0.5;
        else
            multiplier = _random.NextDouble() * 0.3 + 0.2;

        var restockAmount = (int)Math.Ceiling(originalStock * multiplier);

        return Math.Min(restockAmount, originalStock / 2);
    }

    private int GetOriginalStock(int itemId)
    {
        // Original stock values from seed data
        return itemId switch
        {
            1 => 25,   // Healing Potion
            2 => 20,   // Mana Elixir
            3 => 5,    // Invisibility Brew
            4 => 12,   // Strength Tonic
            5 => 8,    // Fire Resistance Draught
            6 => 3,    // Dragon Scale Shield
            7 => 7,    // Elven Dagger
            8 => 10,   // Frost Enchantment
            9 => 2,    // Phoenix Feather
            10 => 50,  // Goblin Ale
            11 => 6,   // Shadow Cloak
            12 => 4,   // Thunder Hammer
            13 => 3,   // Wisdom Scroll
            14 => 15,  // Speed Potion
            15 => 10,  // Mystery Box
            _ => 10    // Default
        };
    }
}