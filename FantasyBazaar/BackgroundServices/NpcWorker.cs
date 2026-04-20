using FantasyBazaar.Api.Data;
using FantasyBazaar.Api.Endpoints;

namespace FantasyBazaar.Api.BackgroundServices;

public class NpcWorker : BackgroundService
{
    private readonly ILogger<NpcWorker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly Random _random = new();
    private readonly string _npcId;
    private readonly string _npcName;
    private readonly int _workerIndex;

    // Configuration
    private int _purchasesPerMinute = 10;

    private bool _isEnabled = true;

    private static readonly string[] NpcNames = new[]
    {
    "Geggolo of the Golden Spittoon",
    "Y'mhitra Rhul",
    "Haurchefant Greystone",
    "Alisaie Leveilleur",
    "Thancred Waters",
    "Tataru Taru",
    "Yotsuyu goe Brutus",
    "Curious Gorge",
    "Kan-E-Senna",
    "Aymeric de Borel",
    "Sadr Albeaq",
    "Merlwyb Bloefhiswyn",
    "Lyse Hext",
    "Godbert Manderville",
    "Y'shtola Rhul"
};

    public NpcWorker(
        ILogger<NpcWorker> logger,
        IServiceProvider serviceProvider,
        int workerIndex)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _workerIndex = workerIndex;

        _npcId = Guid.NewGuid().ToString()[..8];
        var randomName = NpcNames[new Random().Next(NpcNames.Length)];
        _npcName = $"{randomName} (ID:{_npcId})";
    }

    public void SetIntensity(int purchasesPerMinute)
    {
        _purchasesPerMinute = Math.Clamp(purchasesPerMinute, 1, 100);
    }

    public void ToggleEnabled(bool enabled)
    {
        _isEnabled = enabled;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("NPC #{WorkerIndex} - {NpcName} started shopping", _workerIndex, _npcName);

        while (!stoppingToken.IsCancellationRequested)
        {
            if (_isEnabled)
            {
                var delayMs = 60000 / _purchasesPerMinute;
                var actualDelay = delayMs + _random.Next(-(delayMs / 4), delayMs / 4);
                actualDelay = Math.Max(500, actualDelay);

                try
                {
                    await Task.Delay(actualDelay, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }

                await TryNpcPurchase(stoppingToken);
            }
            else
            {
                await Task.Delay(1000, stoppingToken);
            }
        }

        _logger.LogInformation("NPC #{WorkerIndex} - {NpcName} stopped shopping", _workerIndex, _npcName);
    }

    private async Task TryNpcPurchase(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<NpcWorker>>();
            var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();

            var items = db.Items.ToList();
            if (!items.Any())
                return;

            var randomItem = items[new Random().Next(items.Count)];

            if (randomItem.Stock <= 0)
            {
                logger.LogDebug("NPC #{WorkerIndex} - {NpcName} skipped {ItemName} - out of stock",
                    _workerIndex, _npcName, randomItem.Name);
                return;
            }

            var quantity = new Random().Next(1, Math.Min(4, randomItem.Stock + 1));

            logger.LogInformation("NPC #{WorkerIndex} - {NpcName} attempting to buy {Quantity}x {ItemName} (Stock: {Stock})",
                _workerIndex, _npcName, quantity, randomItem.Name, randomItem.Stock);

            // Call the purchase endpoint - SAME pipeline as real users!
            var httpClient = httpClientFactory.CreateClient();
            var purchaseRequest = new PurchaseRequest(randomItem.Id, quantity, $"NPC-{_npcName}");

            var response = await httpClient.PostAsJsonAsync("http://fantasybazaar.api:8080/api/purchase", purchaseRequest, stoppingToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<PurchaseResult>(stoppingToken);
                logger.LogInformation("✅ NPC #{WorkerIndex} - {NpcName} SUCCESS: Bought {Quantity}x {ItemName} for {TotalPaid} gold. Remaining stock: {RemainingStock}",
                    _workerIndex, _npcName, quantity, randomItem.Name, result?.TotalPaid, result?.RemainingStock);
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync(stoppingToken);
                logger.LogWarning("❌ NPC #{WorkerIndex} - {NpcName} FAILED: Could not buy {Quantity}x {ItemName}. Reason: {Error}",
                    _workerIndex, _npcName, quantity, randomItem.Name, error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "NPC #{WorkerIndex} - {NpcName} purchase attempt failed", _workerIndex, _npcName);
        }
    }
}