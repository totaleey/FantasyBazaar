using FantasyBazaar.Api.BackgroundServices;
using FantasyBazaar.Api.Data;
using FantasyBazaar.Api.Endpoints;
using FantasyBazaar.Api.Hubs;
using FantasyBazaar.Api.Models;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSignalR();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"), npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorCodesToAdd: null);
    });
});

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    try
    {
        var configuration = builder.Configuration["Redis:ConnectionString"] ?? "redis:6379";
        var logger = sp.GetRequiredService<ILogger<Program>>();

        var options = ConfigurationOptions.Parse(configuration);
        options.ConnectTimeout = 3000;
        options.AbortOnConnectFail = false;

        var redis = ConnectionMultiplexer.Connect(options);
        logger.LogInformation("Redis connected successfully");
        return redis;
    }
    catch (Exception ex)
    {
        var logger = sp.GetRequiredService<ILogger<Program>>();
        logger.LogWarning(ex, "Redis connection failed - continuing without cache");
        return null;  
    }
});

builder.Services.AddHostedService<NpcManagerService>();
builder.Services.AddHostedService<ReplenishmentService>();
builder.Services.AddDirectoryBrowser();
builder.Services.AddHttpClient();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseDefaultFiles();
app.UseStaticFiles();

// Health check - reports dependency status
app.MapGet("/health", async (AppDbContext db, IConnectionMultiplexer? redis) =>
{
    var status = new
    {
        status = "alive",
        timestamp = DateTime.UtcNow,
        database = await db.Database.CanConnectAsync(),
        redis = redis?.IsConnected ?? false,
        signalR = true  // SignalR always "up" from health perspective
    };
    return Results.Ok(status);
});

// Inventory endpoint - uses cache if Redis is up
app.MapGet("/api/items", async (AppDbContext db, IConnectionMultiplexer? redis, ILogger<Program> logger) =>
{
    // Try cache first, but don't fail if Redis is down
    if (redis?.IsConnected == true)
    {
        try
        {
            var db_cache = redis.GetDatabase();
            var cached = await db_cache.StringGetAsync("all_items");
            if (cached.HasValue)
            {
                logger.LogDebug("Returning cached inventory");
                var cachedString = cached.ToString();
                if (!string.IsNullOrEmpty(cachedString))
                {
                    var cachedItems = JsonSerializer.Deserialize<List<Item>>(cachedString);
                    return Results.Ok(cachedItems);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis cache read failed - falling back to DB");
        }
    }

    var dbItems = await db.Items.ToListAsync();

    if (redis?.IsConnected == true)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                var db_cache = redis.GetDatabase();
                var serialized = JsonSerializer.Serialize(dbItems);
                await db_cache.StringSetAsync("all_items", serialized, TimeSpan.FromSeconds(30));
            }
            catch {}
        });
    }

    return Results.Ok(dbItems);
});

app.MapPurchaseEndpoints();

app.MapHub<BazaarHub>("/bazaarHub");

// Ensure database is created with retry logic
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var retryCount = 0;
    var maxRetries = 10;

    while (retryCount < maxRetries)
    {
        try
        {
            await db.Database.EnsureCreatedAsync();
            logger.LogInformation("Database connection successful!");
            break;
        }
        catch (Exception ex)
        {
            retryCount++;
            logger.LogWarning(ex, "Database connection attempt {RetryCount} failed", retryCount);
            if (retryCount >= maxRetries) throw;
            await Task.Delay(TimeSpan.FromSeconds(3));
        }
    }
}

app.Run();