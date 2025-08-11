using Business.Services.Configuration;
using Core.Utilities.Results;
using Entities.Dtos;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.Configurations.Queries
{
    public class GetConfigurationQuery : IRequest<IDataResult<ConfigurationDto>>
    {
        public int? Id { get; set; }
        public string Key { get; set; }
    }

    public class GetConfigurationQueryHandler : IRequestHandler<GetConfigurationQuery, IDataResult<ConfigurationDto>>
    {
        private readonly IConfigurationService _configurationService;

        public GetConfigurationQueryHandler(IConfigurationService configurationService)
        {
            _configurationService = configurationService;
        }

        public async Task<IDataResult<ConfigurationDto>> Handle(GetConfigurationQuery request, CancellationToken cancellationToken)
        {
            if (request.Id.HasValue)
                return await _configurationService.GetByIdAsync(request.Id.Value);
                
            if (!string.IsNullOrEmpty(request.Key))
                return await _configurationService.GetByKeyAsync(request.Key);
                
            return new ErrorDataResult<ConfigurationDto>("Either Id or Key must be provided");
        }
    }
}