using Business.Services.Configuration;
using Core.Utilities.Results;
using Entities.Dtos;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.Configurations.Commands
{
    public class CreateConfigurationCommand : IRequest<IResult>
    {
        public CreateConfigurationDto CreateDto { get; set; }
        public int UserId { get; set; }
    }

    public class CreateConfigurationCommandHandler : IRequestHandler<CreateConfigurationCommand, IResult>
    {
        private readonly IConfigurationService _configurationService;

        public CreateConfigurationCommandHandler(IConfigurationService configurationService)
        {
            _configurationService = configurationService;
        }

        public async Task<IResult> Handle(CreateConfigurationCommand request, CancellationToken cancellationToken)
        {
            return await _configurationService.CreateAsync(request.CreateDto, request.UserId);
        }
    }
}