#!/usr/bin/env dotnet-script
#r "nuget: Microsoft.EntityFrameworkCore, 9.0.0"
#r "nuget: Npgsql.EntityFrameworkCore.PostgreSQL, 9.0.0"
#r "nuget: Microsoft.EntityFrameworkCore.Design, 9.0.0"
#r "nuget: MediatR, 12.4.1"
#r "nuget: Microsoft.Extensions.Configuration, 9.0.0"
#r "nuget: Microsoft.Extensions.Configuration.Json, 9.0.0"

using System;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

// Set PostgreSQL timezone compatibility switches first
System.AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
System.AppContext.SetSwitch("Npgsql.DisableDateTimeInfinityConversions", true);

// Simplified SponsorProfile entity for testing
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

// Simplified repository interface
public interface ISponsorProfileRepository
{
    void Add(TestSponsorProfile entity);
    Task SaveChangesAsync();
    Task<TestSponsorProfile> GetBySponsorIdAsync(int sponsorId);
}

// Simple repository implementation
public class TestSponsorProfileRepository : ISponsorProfileRepository
{
    private readonly TestDbContext _context;

    public TestSponsorProfileRepository(TestDbContext context)
    {
        _context = context;
    }

    public void Add(TestSponsorProfile entity)
    {
        _context.SponsorProfiles.Add(entity);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task<TestSponsorProfile> GetBySponsorIdAsync(int sponsorId)
    {
        return await _context.SponsorProfiles
            .FirstOrDefaultAsync(x => x.SponsorId == sponsorId);
    }
}

// Test DbContext
public class TestDbContext : DbContext
{
    public DbSet<TestSponsorProfile> SponsorProfiles { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=ziraai_dev;Username=ziraai;Password=devpass");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestSponsorProfile>().ToTable("SponsorProfiles");
    }
}

// Simulate the CreateSponsorProfileCommand logic
public class CreateSponsorProfileCommand
{
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
}

public class CreateSponsorProfileCommandHandler
{
    private readonly ISponsorProfileRepository _sponsorProfileRepository;

    public CreateSponsorProfileCommandHandler(ISponsorProfileRepository sponsorProfileRepository)
    {
        _sponsorProfileRepository = sponsorProfileRepository;
    }

    public async Task<bool> Handle(CreateSponsorProfileCommand request, CancellationToken cancellationToken)
    {
        // Check if sponsor profile already exists
        var existingProfile = await _sponsorProfileRepository.GetBySponsorIdAsync(request.SponsorId);
        if (existingProfile != null)
        {
            Console.WriteLine($"⚠️  Sponsor profile already exists for SponsorId {request.SponsorId}");
            return false;
        }

        var sponsorProfile = new TestSponsorProfile
        {
            SponsorId = request.SponsorId,
            CompanyName = request.CompanyName,
            CompanyDescription = request.CompanyDescription,
            SponsorLogoUrl = request.SponsorLogoUrl,
            WebsiteUrl = request.WebsiteUrl,
            ContactEmail = request.ContactEmail,
            ContactPhone = request.ContactPhone,
            ContactPerson = request.ContactPerson,
            CompanyType = request.CompanyType ?? "Agriculture",
            BusinessModel = request.BusinessModel ?? "B2B",
            IsVerifiedCompany = false,
            IsActive = true,
            TotalPurchases = 0,
            TotalCodesGenerated = 0,
            TotalCodesRedeemed = 0,
            TotalInvestment = 0,
            CreatedDate = DateTime.Now
        };

        _sponsorProfileRepository.Add(sponsorProfile);
        await _sponsorProfileRepository.SaveChangesAsync();
        return true;
    }
}

// Test the command handler
try
{
    Console.WriteLine("✅ Testing CreateSponsorProfileCommand handler...");
    
    using var context = new TestDbContext();
    var repository = new TestSponsorProfileRepository(context);
    var handler = new CreateSponsorProfileCommandHandler(repository);
    
    // Clean up any existing test profile
    var existing = await repository.GetBySponsorIdAsync(1001);
    if (existing != null)
    {
        context.SponsorProfiles.Remove(existing);
        await context.SaveChangesAsync();
        Console.WriteLine("🧹 Cleaned up existing test profile");
    }
    
    // Test the corrected payload structure from Postman collection
    var command = new CreateSponsorProfileCommand
    {
        SponsorId = 1001,
        CompanyName = "ZiraTech Agriculture Solutions",
        CompanyDescription = "Leading provider of agricultural technology and sustainable farming solutions in Turkey.",
        SponsorLogoUrl = "https://example.com/logos/ziratech.png",
        WebsiteUrl = "https://ziratech.com.tr",
        ContactEmail = "sponsor@ziratech.com.tr",
        ContactPhone = "+90 212 555 0123",
        ContactPerson = "Ahmet Yılmaz",
        CompanyType = "Agriculture Technology",
        BusinessModel = "B2B"
    };
    
    var result = await handler.Handle(command, CancellationToken.None);
    
    if (result)
    {
        Console.WriteLine("✅ Successfully created sponsor profile!");
        Console.WriteLine($"   SponsorId: {command.SponsorId}");
        Console.WriteLine($"   CompanyName: {command.CompanyName}");
        Console.WriteLine($"   CompanyType: {command.CompanyType}");
        Console.WriteLine($"   BusinessModel: {command.BusinessModel}");
        
        // Verify the profile was saved
        var savedProfile = await repository.GetBySponsorIdAsync(command.SponsorId);
        if (savedProfile != null)
        {
            Console.WriteLine($"✅ Profile verified in database with ID: {savedProfile.Id}");
            Console.WriteLine($"   CreatedDate: {savedProfile.CreatedDate}");
            Console.WriteLine($"   TotalPurchases: {savedProfile.TotalPurchases}");
            Console.WriteLine($"   IsActive: {savedProfile.IsActive}");
        }
        
        // Clean up test profile
        context.SponsorProfiles.Remove(savedProfile);
        await context.SaveChangesAsync();
        Console.WriteLine("🧹 Cleaned up test profile");
    }
    else
    {
        Console.WriteLine("❌ Failed to create sponsor profile");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
    if (ex.InnerException != null)
    {
        Console.WriteLine($"   Inner Exception: {ex.InnerException.Message}");
    }
}

Console.WriteLine("\n🚀 Test completed!");