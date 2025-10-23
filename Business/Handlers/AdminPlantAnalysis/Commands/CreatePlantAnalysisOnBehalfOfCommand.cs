using System;
using System.Threading;
using System.Threading.Tasks;
using Business.BusinessAspects;
using Business.Services.AdminAudit;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using MediatR;

namespace Business.Handlers.AdminPlantAnalysis.Commands
{
    /// <summary>
    /// Admin command to create plant analysis on behalf of a user
    /// </summary>
    public class CreatePlantAnalysisOnBehalfOfCommand : IRequest<IDataResult<PlantAnalysis>>
    {
        public int TargetUserId { get; set; }
        public string ImageUrl { get; set; }
        public string AnalysisResult { get; set; }
        public string Notes { get; set; }

        // Admin context
        public int AdminUserId { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        public string RequestPath { get; set; }

        public class CreatePlantAnalysisOnBehalfOfCommandHandler : IRequestHandler<CreatePlantAnalysisOnBehalfOfCommand, IDataResult<PlantAnalysis>>
        {
            private readonly IPlantAnalysisRepository _analysisRepository;
            private readonly IUserRepository _userRepository;
            private readonly IAdminAuditService _auditService;

            public CreatePlantAnalysisOnBehalfOfCommandHandler(
                IPlantAnalysisRepository analysisRepository,
                IUserRepository userRepository,
                IAdminAuditService auditService)
            {
                _analysisRepository = analysisRepository;
                _userRepository = userRepository;
                _auditService = auditService;
            }

            [SecuredOperation(Priority = 1)]
            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<PlantAnalysis>> Handle(CreatePlantAnalysisOnBehalfOfCommand request, CancellationToken cancellationToken)
            {
                // Verify target user exists
                var targetUser = await _userRepository.GetAsync(u => u.UserId == request.TargetUserId);
                if (targetUser == null)
                {
                    return new ErrorDataResult<PlantAnalysis>("Target user not found");
                }

                var now = DateTime.Now;
                var analysis = new PlantAnalysis
                {
                    UserId = request.TargetUserId,
                    ImageUrl = request.ImageUrl,
                    AnalysisResult = request.AnalysisResult,
                    AnalysisStatus = "completed",
                    Status = true,
                    CreatedDate = now,
                    AnalysisDate = now,
                    IsOnBehalfOf = true,
                    CreatedByAdminId = request.AdminUserId,
                    Notes = string.IsNullOrEmpty(request.Notes) 
                        ? "[Created by Admin]" 
                        : $"[Created by Admin] {request.Notes}"
                };

                _analysisRepository.Add(analysis);
                await _analysisRepository.SaveChangesAsync();

                // Audit log
                await _auditService.LogAsync(
                    action: "CreatePlantAnalysisOnBehalfOf",
                    adminUserId: request.AdminUserId,
                    targetUserId: request.TargetUserId,
                    entityType: "PlantAnalysis",
                    entityId: analysis.Id,
                    isOnBehalfOf: true,
                    ipAddress: request.IpAddress,
                    userAgent: request.UserAgent,
                    requestPath: request.RequestPath,
                    reason: $"Created plant analysis for user {targetUser.FullName}",
                    afterState: new
                    {
                        analysis.Id,
                        analysis.UserId,
                        analysis.Status,
                        analysis.IsOnBehalfOf,
                        analysis.CreatedByAdminId
                    }
                );

                return new SuccessDataResult<PlantAnalysis>(analysis, $"Plant analysis created successfully for user {targetUser.FullName}");
            }
        }
    }
}
