#!/usr/bin/env dotnet-script
#r "nuget: Microsoft.EntityFrameworkCore, 9.0.0"
#r "nuget: Npgsql.EntityFrameworkCore.PostgreSQL, 9.0.0"
#r "nuget: Microsoft.EntityFrameworkCore.Design, 9.0.0"

using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

// Create a minimal DbContext to test the SponsorProfile entity
public class TestSponsorProfile
{
    public int Id { get; set; }
    public int SponsorId { get; set; }
    public string CompanyName { get; set; }
    public string CompanyDescription { get; set; }
    public string SponsorLogoUrl { get; set; }
    public string WebsiteUrl { get; set; }
    public string ContactEmail { get; set; }
    public string ContactPhone { get; set; }
    public string ContactPerson { get; set; }
    public string CompanyType { get; set; }
    public string BusinessModel { get; set; }
    public bool IsVerifiedCompany { get; set; }
    public bool IsActive { get; set; }
    public int TotalPurchases { get; set; }
    public int TotalCodesGenerated { get; set; }
    public int TotalCodesRedeemed { get; set; }
    public decimal TotalInvestment { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class TestDbContext : DbContext
{
    public DbSet<TestSponsorProfile> SponsorProfiles { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Set the PostgreSQL timezone compatibility switches
        System.AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        System.AppContext.SetSwitch("Npgsql.DisableDateTimeInfinityConversions", true);
        
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=ziraai_dev;Username=ziraai;Password=devpass");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestSponsorProfile>().ToTable("SponsorProfiles");
    }
}

// Test the actual scenario
try
{
    using var context = new TestDbContext();
    
    Console.WriteLine("✅ Testing SponsorProfile entity creation...");
    
    // Test querying existing profiles
    var existingProfiles = await context.SponsorProfiles.Take(5).ToListAsync();
    Console.WriteLine($"📋 Found {existingProfiles.Count} existing sponsor profiles.");
    
    // Test creating a new profile (similar to the command handler)
    var testProfile = new TestSponsorProfile
    {
        SponsorId = 999,
        CompanyName = "Test Company",
        CompanyDescription = "Test Description",
        CompanyType = "Agriculture",
        BusinessModel = "B2B",
        IsVerifiedCompany = false,
        IsActive = true,
        TotalPurchases = 0,
        TotalCodesGenerated = 0,
        TotalCodesRedeemed = 0,
        TotalInvestment = 0,
        CreatedDate = DateTime.Now
    };
    
    // Check if profile already exists (simulating the command handler logic)
    var existingProfile = await context.SponsorProfiles
        .FirstOrDefaultAsync(x => x.SponsorId == testProfile.SponsorId);
    
    if (existingProfile != null)
    {
        Console.WriteLine($"⚠️  Profile already exists for SponsorId {testProfile.SponsorId}");
        // Remove existing test profile
        context.SponsorProfiles.Remove(existingProfile);
        await context.SaveChangesAsync();
        Console.WriteLine("🗑️ Removed existing test profile");
    }
    
    // Add new profile
    context.SponsorProfiles.Add(testProfile);
    await context.SaveChangesAsync();
    
    Console.WriteLine("✅ Successfully created sponsor profile!");
    Console.WriteLine($"   SponsorId: {testProfile.SponsorId}");
    Console.WriteLine($"   CompanyName: {testProfile.CompanyName}");
    Console.WriteLine($"   CreatedDate: {testProfile.CreatedDate}");
    
    // Clean up - remove test profile
    context.SponsorProfiles.Remove(testProfile);
    await context.SaveChangesAsync();
    Console.WriteLine("🧹 Cleaned up test profile");
    
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
    if (ex.InnerException != null)
    {
        Console.WriteLine($"   Inner Exception: {ex.InnerException.Message}");
    }
    Console.WriteLine($"   Stack Trace: {ex.StackTrace}");
}

Console.WriteLine("\n🚀 Test completed!");