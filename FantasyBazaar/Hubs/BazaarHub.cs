using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis;

namespace FantasyBazaar.Api.Hubs;

public class BazaarHub : Hub
{
    private readonly ILogger<BazaarHub> _logger;
    private readonly IConnectionMultiplexer? _redis;
    private readonly IServiceProvider _serviceProvider;

    public BazaarHub(ILogger<BazaarHub> logger, IConnectionMultiplexer? redis, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _redis = redis;
        _serviceProvider = serviceProvider;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await Groups.AddToGroupAsync(Context.ConnectionId, "all-clients");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "all-clients");
        await base.OnDisconnectedAsync(exception);
    }

    public static async Task BroadcastStockUpdate(IHubContext<BazaarHub> hub, int itemId, string itemName, int newStock, decimal newPrice)
    {
        try
        {
            await hub.Clients.Group("all-clients").SendAsync("StockUpdated", new
            {
                itemId,
                itemName,
                newStock,
                newPrice,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SignalR broadcast failed: {ex.Message}");
        }
    }

    public async Task BroadcastNpcActivity(string npcName, string action, string details, string type)
    {
        try
        {
            await Clients.All.SendAsync("NpcActivity", new
            {
                npcName,
                action,
                details,
                type,
                timestamp = DateTime.Now.ToLocalTime().ToString("HH:mm:ss")
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to broadcast NPC activity for {NpcName}", npcName);
        }
    }
}