using FantasyBazaar.Api.Data;
using FantasyBazaar.Api.Hubs;
using FantasyBazaar.Api.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace FantasyBazaar.Api.BackgroundServices
{
    public class PriceEngineService : BackgroundService
    {
        private readonly ILogger<PriceEngineService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IHubContext<BazaarHub> _hubContext;

        // config
        private readonly int _priceUpdateIntervalMinutes = 1;

        public PriceEngineService(
            ILogger<PriceEngineService> logger,
            IServiceProvider serviceProvider,
            IHubContext<BazaarHub> hubContext)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _hubContext = hubContext;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Price engine Service started - running every {Interval} minutes", _priceUpdateIntervalMinutes);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(_priceUpdateIntervalMinutes), stoppingToken);
                    await UpdatePrices(stoppingToken);
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

            _logger.LogInformation("Price engine Service stopped");
        }

        private async Task UpdatePrices(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var random = new Random();

            var items = await db.Items.ToListAsync(stoppingToken);
            var oldPrices = items.ToDictionary(i => i.Id, i => i.CurrentPrice);
            var updatedCount = 0;

            foreach (var item in items)
            {
                var originalStock = GetOriginalStock(item.Id);
                var oldPrice = item.CurrentPrice;

                // Calculate new price
                var newPrice = CalculateNewPrice(item, originalStock, random);

                // Round to 2 decimal places
                newPrice = Math.Round(newPrice, 2);

                if (newPrice != oldPrice)
                {
                    item.CurrentPrice = newPrice;
                    item.LastPriceUpdate = DateTime.UtcNow;
                    updatedCount++;

                    _logger.LogInformation("💰 Price changed for {ItemName}: {OldPrice} → {NewPrice} gold (Stock: {Stock}/{MaxStock}, Popularity: {Popularity})",
                        item.Name, oldPrice, newPrice, item.Stock, originalStock, item.PopularityScore);
                }
            }

            await db.SaveChangesAsync(stoppingToken);

            // Broadcast price updates via SignalR
            foreach (var item in items.Where(i => i.CurrentPrice != oldPrices[i.Id]))
            {
                try
                {
                    await _hubContext.Clients.All.SendAsync("PriceUpdated", new
                    {
                        itemId = item.Id,
                        itemName = item.Name,
                        newPrice = item.CurrentPrice,
                        oldPrice = oldPrices[item.Id],
                        timestamp = DateTime.UtcNow
                    });
                    _logger.LogDebug("Broadcasted price update for {ItemName}", item.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "SignalR broadcast failed for {ItemName}", item.Name);
                }
            }

            if (updatedCount > 0)
                _logger.LogInformation("Price update complete - Changed {Count} items", updatedCount);
            else
                _logger.LogDebug("Price update cycle - No price changes needed");
        }

        private decimal CalculateNewPrice(Item item, int originalStock, Random random)
        {
            var basePrice = item.BasePrice;
            var stockPercent = (double)item.Stock / originalStock;
            var popularity = item.PopularityScore;

            // 1. Stock Factor (inverse relationship)
            double stockFactor;
            if (stockPercent >= 0.7)
                stockFactor = random.NextDouble() * -0.2;
            else if (stockPercent >= 0.3)
                stockFactor = random.NextDouble() * 0.1;
            else
                stockFactor = random.NextDouble() * 0.3 + 0.1; // +10% to +40%

            // 2. Popularity Factor
            double popularityFactor;
            if (popularity >= 80)
                popularityFactor = random.NextDouble() * 0.1 + 0.05;
            else if (popularity >= 50)
                popularityFactor = random.NextDouble() * 0.05;
            else
                popularityFactor = random.NextDouble() * -0.05;

            // 3. Random Market Events
            double randomFactor = 0;
            var eventRoll = random.Next(100);

            if (eventRoll < 5)
            {
                randomFactor = random.NextDouble() * 0.2 + 0.2; // +20% to +40%
                _logger.LogInformation("⚡ Market Event: Supply Shortage! Prices increasing");
            }
            else if (eventRoll < 10)
            {
                randomFactor = random.NextDouble() * -0.3;
                _logger.LogInformation("📉 Market Event: Market Crash! Prices dropping");
            }
            else if (eventRoll < 15)
            {
                randomFactor = random.NextDouble() * -0.1 - 0.1;
                _logger.LogInformation("🎉 Market Event: Daily Special! Selected items discounted");
            }

            // Calculate final multiplier
            var multiplier = 1 + stockFactor + popularityFactor + randomFactor;

            // Cap at reasonable bounds
            multiplier = Math.Clamp(multiplier, 0.5, 3.0);

            return basePrice * (decimal)multiplier;
        }

        private int GetOriginalStock(int itemId)
        {
            return itemId switch
            {
                1 => 25,
                2 => 20,
                3 => 5,
                4 => 12,
                5 => 8,
                6 => 3,
                7 => 7,
                8 => 10,
                9 => 2,
                10 => 50,
                11 => 6,
                12 => 4,
                13 => 3,
                14 => 15,
                15 => 10,
                _ => 10
            };
        }
    }
}