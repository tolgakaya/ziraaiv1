using AutoMapper;
using Business.Services.Configuration;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.Dtos;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.DataAccess;

namespace Business.Services.Configuration
{
    public class ConfigurationService : IConfigurationService
    {
        private readonly IConfigurationRepository _configurationRepository;
        private readonly IMapper _mapper;
        private readonly IMemoryCache _cache;
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(15);

        public ConfigurationService(
            IConfigurationRepository configurationRepository, 
            IMapper mapper,
            IMemoryCache cache)
        {
            _configurationRepository = configurationRepository;
            _mapper = mapper;
            _cache = cache;
        }

        public async Task<IDataResult<List<ConfigurationDto>>> GetAllAsync()
        {
            try
            {
                var configurations = await _configurationRepository.GetListAsync(x => x.IsActive);
                var configDtos = _mapper.Map<List<ConfigurationDto>>(configurations.OrderBy(x => x.Category).ThenBy(x => x.Key));
                
                return new SuccessDataResult<List<ConfigurationDto>>(configDtos);
            }
            catch (Exception ex)
            {
                return new ErrorDataResult<List<ConfigurationDto>>(ex.Message);
            }
        }

        public async Task<IDataResult<List<ConfigurationDto>>> GetByCategoryAsync(string category)
        {
            try
            {
                var configurations = await _configurationRepository.GetListAsync(x => x.Category == category && x.IsActive);
                var configDtos = _mapper.Map<List<ConfigurationDto>>(configurations.OrderBy(x => x.Key));
                
                return new SuccessDataResult<List<ConfigurationDto>>(configDtos);
            }
            catch (Exception ex)
            {
                return new ErrorDataResult<List<ConfigurationDto>>(ex.Message);
            }
        }

        public async Task<IDataResult<ConfigurationDto>> GetByIdAsync(int id)
        {
            try
            {
                var configuration = await _configurationRepository.GetAsync(x => x.Id == id);
                if (configuration == null)
                    return new ErrorDataResult<ConfigurationDto>("Configuration not found");

                var configDto = _mapper.Map<ConfigurationDto>(configuration);
                return new SuccessDataResult<ConfigurationDto>(configDto);
            }
            catch (Exception ex)
            {
                return new ErrorDataResult<ConfigurationDto>(ex.Message);
            }
        }

        public async Task<IDataResult<ConfigurationDto>> GetByKeyAsync(string key)
        {
            try
            {
                var configuration = await _configurationRepository.GetByKeyAsync(key);
                if (configuration == null)
                    return new ErrorDataResult<ConfigurationDto>("Configuration not found");

                var configDto = _mapper.Map<ConfigurationDto>(configuration);
                return new SuccessDataResult<ConfigurationDto>(configDto);
            }
            catch (Exception ex)
            {
                return new ErrorDataResult<ConfigurationDto>(ex.Message);
            }
        }

        public async Task<IResult> CreateAsync(CreateConfigurationDto createDto, int userId)
        {
            try
            {
                // Check if key already exists
                var existingConfig = await _configurationRepository.GetByKeyAsync(createDto.Key);
                if (existingConfig != null)
                    return new ErrorResult("Configuration with this key already exists");

                var configuration = _mapper.Map<Entities.Concrete.Configuration>(createDto);
                configuration.CreatedBy = userId;
                configuration.CreatedDate = DateTime.UtcNow;
                configuration.IsActive = true;

                var addedConfig = _configurationRepository.Add(configuration);
                await _configurationRepository.SaveChangesAsync();
                
                // Clear cache
                ClearCache();
                
                return new SuccessResult("Configuration created successfully");
            }
            catch (Exception ex)
            {
                return new ErrorResult(ex.Message);
            }
        }

        public async Task<IResult> UpdateAsync(UpdateConfigurationDto updateDto, int userId)
        {
            try
            {
                var configuration = await _configurationRepository.GetTrackedAsync(x => x.Id == updateDto.Id);
                if (configuration == null)
                    return new ErrorResult("Configuration not found");

                configuration.Value = updateDto.Value;
                configuration.Description = updateDto.Description;
                configuration.IsActive = updateDto.IsActive;
                configuration.UpdatedBy = userId;
                configuration.UpdatedDate = DateTime.UtcNow;

                var updatedConfig = _configurationRepository.Update(configuration);
                await _configurationRepository.SaveChangesAsync();
                
                // Clear cache
                ClearCache();
                
                return new SuccessResult("Configuration updated successfully");
            }
            catch (Exception ex)
            {
                return new ErrorResult(ex.Message);
            }
        }

        public async Task<IResult> DeleteAsync(int id)
        {
            try
            {
                var configuration = await _configurationRepository.GetTrackedAsync(x => x.Id == id);
                if (configuration == null)
                    return new ErrorResult("Configuration not found");

                configuration.IsActive = false;
                configuration.UpdatedDate = DateTime.UtcNow;

                var updatedConfig = _configurationRepository.Update(configuration);
                await _configurationRepository.SaveChangesAsync();
                
                // Clear cache
                ClearCache();
                
                return new SuccessResult("Configuration deleted successfully");
            }
            catch (Exception ex)
            {
                return new ErrorResult(ex.Message);
            }
        }

        public async Task<T> GetValueAsync<T>(string key, T defaultValue = default)
        {
            var cacheKey = $"config_{key}";
            
            if (_cache.TryGetValue(cacheKey, out T cachedValue))
                return cachedValue;

            try
            {
                var value = await _configurationRepository.GetValueByKeyAsync(key, defaultValue);
                _cache.Set(cacheKey, value, _cacheExpiration);
                return value;
            }
            catch
            {
                return defaultValue;
            }
        }

        public async Task<string> GetValueAsync(string key, string defaultValue = null)
        {
            return await GetValueAsync<string>(key, defaultValue);
        }

        public async Task<int> GetIntValueAsync(string key, int defaultValue = 0)
        {
            return await GetValueAsync<int>(key, defaultValue);
        }

        public async Task<decimal> GetDecimalValueAsync(string key, decimal defaultValue = 0)
        {
            return await GetValueAsync<decimal>(key, defaultValue);
        }

        public async Task<bool> GetBoolValueAsync(string key, bool defaultValue = false)
        {
            return await GetValueAsync<bool>(key, defaultValue);
        }

        private void ClearCache()
        {
            if (_cache is MemoryCache memoryCache)
            {
                memoryCache.Compact(1.0);
            }
        }
    }
}