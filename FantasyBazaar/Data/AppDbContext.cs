using Microsoft.EntityFrameworkCore;
using FantasyBazaar.Api.Models;

namespace FantasyBazaar.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Item> Items => Set<Item>();
    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Item>().HasData(
            new Item { Id = 1, Name = "Healing Potion", Description = "Restores 50 HP", Category = "Potion", BasePrice = 50m, CurrentPrice = 50m, Stock = 25, PopularityScore = 85, LastPriceUpdate = DateTime.UtcNow, IsSpecial = false },
            new Item { Id = 2, Name = "Mana Elixir", Description = "Restores 30 MP", Category = "Potion", BasePrice = 45m, CurrentPrice = 45m, Stock = 20, PopularityScore = 70, LastPriceUpdate = DateTime.UtcNow, IsSpecial = false },
            new Item { Id = 3, Name = "Invisibility Brew", Description = "Turn invisible for 30s", Category = "Potion", BasePrice = 120m, CurrentPrice = 120m, Stock = 5, PopularityScore = 95, LastPriceUpdate = DateTime.UtcNow, IsSpecial = false },
            new Item { Id = 4, Name = "Strength Tonic", Description = "+20 Strength for 1hr", Category = "Potion", BasePrice = 80m, CurrentPrice = 80m, Stock = 12, PopularityScore = 60, LastPriceUpdate = DateTime.UtcNow, IsSpecial = false },
            new Item { Id = 5, Name = "Fire Resistance Draught", Description = "50% fire resist for 2hrs", Category = "Potion", BasePrice = 90m, CurrentPrice = 90m, Stock = 8, PopularityScore = 75, LastPriceUpdate = DateTime.UtcNow, IsSpecial = false },
            new Item { Id = 6, Name = "Dragon Scale Shield", Description = "Defense +25", Category = "Armor", BasePrice = 200m, CurrentPrice = 200m, Stock = 3, PopularityScore = 90, LastPriceUpdate = DateTime.UtcNow, IsSpecial = true },
            new Item { Id = 7, Name = "Elven Dagger", Description = "+15% critical hit", Category = "Weapon", BasePrice = 150m, CurrentPrice = 150m, Stock = 7, PopularityScore = 80, LastPriceUpdate = DateTime.UtcNow, IsSpecial = false },
            new Item { Id = 8, Name = "Frost Enchantment", Description = "Adds ice damage", Category = "Enchantment", BasePrice = 75m, CurrentPrice = 75m, Stock = 10, PopularityScore = 65, LastPriceUpdate = DateTime.UtcNow, IsSpecial = false },
            new Item { Id = 9, Name = "Phoenix Feather", Description = "Auto-revive once", Category = "Rare", BasePrice = 300m, CurrentPrice = 300m, Stock = 2, PopularityScore = 100, LastPriceUpdate = DateTime.UtcNow, IsSpecial = true },
            new Item { Id = 10, Name = "Goblin Ale", Description = "Questionable effects", Category = "Food", BasePrice = 15m, CurrentPrice = 15m, Stock = 50, PopularityScore = 40, LastPriceUpdate = DateTime.UtcNow, IsSpecial = false },
            new Item { Id = 11, Name = "Shadow Cloak", Description = "+10 stealth", Category = "Armor", BasePrice = 110m, CurrentPrice = 110m, Stock = 6, PopularityScore = 70, LastPriceUpdate = DateTime.UtcNow, IsSpecial = false },
            new Item { Id = 12, Name = "Thunder Hammer", Description = "Chance to stun", Category = "Weapon", BasePrice = 250m, CurrentPrice = 250m, Stock = 4, PopularityScore = 85, LastPriceUpdate = DateTime.UtcNow, IsSpecial = false },
            new Item { Id = 13, Name = "Wisdom Scroll", Description = "+5 intellect permanently", Category = "Enchantment", BasePrice = 180m, CurrentPrice = 180m, Stock = 3, PopularityScore = 88, LastPriceUpdate = DateTime.UtcNow, IsSpecial = true },
            new Item { Id = 14, Name = "Speed Potion", Description = "Double movement speed", Category = "Potion", BasePrice = 65m, CurrentPrice = 65m, Stock = 15, PopularityScore = 78, LastPriceUpdate = DateTime.UtcNow, IsSpecial = false },
            new Item { Id = 15, Name = "Mystery Box", Description = "Random item inside!", Category = "Special", BasePrice = 50m, CurrentPrice = 50m, Stock = 10, PopularityScore = 95, LastPriceUpdate = DateTime.UtcNow, IsSpecial = true }
        );
    }
}