using Core.Utilities.Results;
using Entities.Dtos;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Business.Services.Configuration
{
    public interface IConfigurationService
    {
        Task<IDataResult<List<ConfigurationDto>>> GetAllAsync();
        Task<IDataResult<List<ConfigurationDto>>> GetByCategoryAsync(string category);
        Task<IDataResult<ConfigurationDto>> GetByIdAsync(int id);
        Task<IDataResult<ConfigurationDto>> GetByKeyAsync(string key);
        Task<IResult> CreateAsync(CreateConfigurationDto createDto, int userId);
        Task<IResult> UpdateAsync(UpdateConfigurationDto updateDto, int userId);
        Task<IResult> DeleteAsync(int id);
        
        // Strongly typed value getters
        Task<T> GetValueAsync<T>(string key, T defaultValue = default);
        Task<string> GetValueAsync(string key, string defaultValue = null);
        Task<int> GetIntValueAsync(string key, int defaultValue = 0);
        Task<decimal> GetDecimalValueAsync(string key, decimal defaultValue = 0);
        Task<bool> GetBoolValueAsync(string key, bool defaultValue = false);
    }
}