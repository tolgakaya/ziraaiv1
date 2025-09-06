using System.Threading.Tasks;

namespace Business.Services.DatabaseInitializer
{
    public interface IDatabaseInitializerService
    {
        Task InitializeAsync();
        Task SeedDataAsync();
        Task<bool> CheckIfDataExistsAsync();
    }
}