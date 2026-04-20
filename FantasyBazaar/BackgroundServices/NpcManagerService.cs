namespace FantasyBazaar.Api.BackgroundServices;

public class NpcManagerService : BackgroundService
{
    private readonly ILogger<NpcManagerService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly List<NpcWorker> _workers = new();
    private readonly List<Task> _workerTasks = new();

    private int _targetNpcCount = 10;
    private int _purchasesPerMinute = 5;
    private bool _isEnabled = true;

    public NpcManagerService(
        ILogger<NpcManagerService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public void SetNpcCount(int count)
    {
        _targetNpcCount = Math.Clamp(count, 1, 50);
        _logger.LogInformation("NPC target count set to {Count}", _targetNpcCount);
    }

    public void SetIntensity(int purchasesPerMinute)
    {
        _purchasesPerMinute = Math.Clamp(purchasesPerMinute, 1, 100);
        _logger.LogInformation("NPC intensity set to {Intensity} purchases/minute per NPC", _purchasesPerMinute);

        // Update all existing workers
        foreach (var worker in _workers)
        {
            worker.SetIntensity(_purchasesPerMinute);
        }
    }

    public void ToggleEnabled(bool enabled)
    {
        _isEnabled = enabled;
        _logger.LogInformation("NPC system {State}", enabled ? "enabled" : "disabled");

        foreach (var worker in _workers)
        {
            worker.ToggleEnabled(enabled);
        }
    }

    public int GetActiveNpcCount() => _workers.Count;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("NPC Manager Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            // Adjust number of NPCs to match target
            while (_workers.Count < _targetNpcCount && !stoppingToken.IsCancellationRequested)
            {
                await AddNpc(stoppingToken);
            }

            while (_workers.Count > _targetNpcCount && _workers.Count > 0)
            {
                await RemoveNpc(stoppingToken);
            }

            await Task.Delay(5000, stoppingToken);  // Check every 5 seconds
        }

        // Stop all workers on shutdown
        _logger.LogInformation("NPC Manager Service stopping, shutting down {Count} NPCs", _workers.Count);
    }

    private async Task AddNpc(CancellationToken stoppingToken)
    {
        var workerIndex = _workers.Count + 1;

        using var scope = _serviceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<NpcWorker>>();

        var worker = new NpcWorker(logger, _serviceProvider, workerIndex);
        worker.SetIntensity(_purchasesPerMinute);
        worker.ToggleEnabled(_isEnabled);

        _workers.Add(worker);

        // Start the worker task
        var workerTask = worker.StartAsync(stoppingToken);
        _workerTasks.Add(workerTask);

        _logger.LogInformation("Added NPC #{WorkerIndex} - Total NPCs: {Count}", workerIndex, _workers.Count);

        await Task.CompletedTask;
    }

    private async Task RemoveNpc(CancellationToken stoppingToken)
    {
        if (_workers.Count == 0) return;

        var lastWorker = _workers.Last();
        _workers.Remove(lastWorker);

        await lastWorker.StopAsync(stoppingToken);

        _logger.LogInformation("Removed NPC - Total NPCs: {Count}", _workers.Count);
    }
}