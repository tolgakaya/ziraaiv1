using DataAccess.Concrete.EntityFramework.Contexts;
using Microsoft.EntityFrameworkCore;

var connectionString = "Host=localhost;Port=5432;Database=ziraai_dev;Username=ziraai;Password=devpass";
var optionsBuilder = new DbContextOptionsBuilder<ProjectDbContext>();
optionsBuilder.UseNpgsql(connectionString);

using var context = new ProjectDbContext(optionsBuilder.Options);

var recentPlantAnalyses = await context.PlantAnalyses
    .OrderByDescending(p => p.Id)
    .Take(5)
    .Select(p => new {
        p.Id,
        p.UserId,
        p.FarmerId,
        p.CropType,
        p.PlantSpecies,
        p.PrimaryConcern,
        p.OverallHealthScore,
        p.AnalysisId,
        p.CreatedDate
    })
    .ToListAsync();

Console.WriteLine("=== SON 5 PLANT ANALYSIS KAYDI ===");
Console.WriteLine($"{"Id",-5} {"UserId",-8} {"FarmerId",-15} {"CropType",-10} {"PlantSpecies",-20} {"PrimaryConcern",-20} {"HealthScore",-12} {"AnalysisId",-30}");
Console.WriteLine(new string('-', 130));

foreach (var analysis in recentPlantAnalyses)
{
    Console.WriteLine($"{analysis.Id,-5} {analysis.UserId?.ToString() ?? "NULL",-8} {analysis.FarmerId ?? "NULL",-15} {analysis.CropType ?? "NULL",-10} {(analysis.PlantSpecies ?? "NULL").Substring(0, Math.Min(20, (analysis.PlantSpecies ?? "NULL").Length)),-20} {(analysis.PrimaryConcern ?? "NULL").Substring(0, Math.Min(20, (analysis.PrimaryConcern ?? "NULL").Length)),-20} {analysis.OverallHealthScore?.ToString() ?? "NULL",-12} {analysis.AnalysisId ?? "NULL",-30}");
}