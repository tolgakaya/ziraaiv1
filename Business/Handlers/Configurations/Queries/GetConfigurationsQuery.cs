using Business.Services.Configuration;
using Core.Utilities.Results;
using Entities.Dtos;
using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.Configurations.Queries
{
    public class GetConfigurationsQuery : IRequest<IDataResult<List<ConfigurationDto>>>
    {
        public string Category { get; set; }
    }

    public class GetConfigurationsQueryHandler : IRequestHandler<GetConfigurationsQuery, IDataResult<List<ConfigurationDto>>>
    {
        private readonly IConfigurationService _configurationService;

        public GetConfigurationsQueryHandler(IConfigurationService configurationService)
        {
            _configurationService = configurationService;
        }

        public async Task<IDataResult<List<ConfigurationDto>>> Handle(GetConfigurationsQuery request, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrEmpty(request.Category))
                return await _configurationService.GetByCategoryAsync(request.Category);
                
            return await _configurationService.GetAllAsync();
        }
    }
}