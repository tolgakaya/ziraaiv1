using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using DataAccess.Concrete.EntityFramework.Contexts;
using Entities.Concrete;

class Program
{
    static async Task Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("WebAPI/appsettings.Development.json")
            .Build();

        var options = new DbContextOptionsBuilder<ProjectDbContext>()
            .UseNpgsql(configuration.GetConnectionString("DArchPgContext"))
            .Options;

        using var context = new ProjectDbContext(options, configuration);
        
        Console.WriteLine("=== Checking Trial Tier ===");
        var trialTier = await context.SubscriptionTiers
            .Where(t => t.TierName == "Trial" && t.IsActive)
            .FirstOrDefaultAsync();
            
        if (trialTier != null)
        {
            Console.WriteLine($"✅ Trial tier found: ID={trialTier.Id}, Name={trialTier.TierName}");
            Console.WriteLine($"   Daily Limit: {trialTier.DailyRequestLimit}");
            Console.WriteLine($"   Monthly Limit: {trialTier.MonthlyRequestLimit}");
            Console.WriteLine($"   Active: {trialTier.IsActive}");
        }
        else
        {
            Console.WriteLine("❌ No Trial tier found!");
            Console.WriteLine("Available tiers:");
            var allTiers = await context.SubscriptionTiers.ToListAsync();
            foreach (var tier in allTiers)
            {
                Console.WriteLine($"   - {tier.TierName} (ID: {tier.Id}, Active: {tier.IsActive})");
            }
        }

        Console.WriteLine("\n=== Checking Recent User Subscriptions ===");
        var recentSubscriptions = await context.UserSubscriptions
            .Include(s => s.SubscriptionTier)
            .OrderByDescending(s => s.CreatedDate)
            .Take(5)
            .ToListAsync();
            
        foreach (var sub in recentSubscriptions)
        {
            Console.WriteLine($"User {sub.UserId}: {sub.SubscriptionTier?.TierName ?? "NULL"} " +
                            $"(Active: {sub.IsActive}, Created: {sub.CreatedDate:yyyy-MM-dd HH:mm})");
        }
    }
}