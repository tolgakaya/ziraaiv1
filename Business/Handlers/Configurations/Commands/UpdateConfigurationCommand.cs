using Business.Services.Configuration;
using Core.Utilities.Results;
using Entities.Dtos;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.Configurations.Commands
{
    public class UpdateConfigurationCommand : IRequest<IResult>
    {
        public UpdateConfigurationDto UpdateDto { get; set; }
        public int UserId { get; set; }
    }

    public class UpdateConfigurationCommandHandler : IRequestHandler<UpdateConfigurationCommand, IResult>
    {
        private readonly IConfigurationService _configurationService;

        public UpdateConfigurationCommandHandler(IConfigurationService configurationService)
        {
            _configurationService = configurationService;
        }

        public async Task<IResult> Handle(UpdateConfigurationCommand request, CancellationToken cancellationToken)
        {
            return await _configurationService.UpdateAsync(request.UpdateDto, request.UserId);
        }
    }
}