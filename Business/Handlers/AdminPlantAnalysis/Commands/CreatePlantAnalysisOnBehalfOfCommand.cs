using System;
using System.Threading;
using System.Threading.Tasks;
using Business.BusinessAspects;
using Business.Services.AdminAudit;
using Business.Services.MessageQueue;
using Core.Aspects.Autofac.Logging;
using Core.Configuration;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.Dtos;
using MediatR;
using Microsoft.Extensions.Options;

namespace Business.Handlers.AdminPlantAnalysis.Commands
{
    /// <summary>
    /// Admin command to create plant analysis on behalf of a user
    /// </summary>
    public class CreatePlantAnalysisOnBehalfOfCommand : IRequest<IDataResult<PlantAnalysis>>
    {
        public int TargetUserId { get; set; }
        public string ImageUrl { get; set; }
        public string Notes { get; set; }

        // Admin context
        public int AdminUserId { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        public string RequestPath { get; set; }

        public class CreatePlantAnalysisOnBehalfOfCommandHandler : IRequestHandler<CreatePlantAnalysisOnBehalfOfCommand, IDataResult<PlantAnalysis>>
        {
            private readonly IMessageQueueService _messageQueueService;
            private readonly IPlantAnalysisRepository _analysisRepository;
            private readonly IUserRepository _userRepository;
            private readonly IAdminAuditService _auditService;
            private readonly RabbitMQOptions _rabbitMQOptions;

            public CreatePlantAnalysisOnBehalfOfCommandHandler(
                IMessageQueueService messageQueueService,
                IPlantAnalysisRepository analysisRepository,
                IUserRepository userRepository,
                IAdminAuditService auditService,
                IOptions<RabbitMQOptions> rabbitMQOptions)
            {
                _messageQueueService = messageQueueService;
                _analysisRepository = analysisRepository;
                _userRepository = userRepository;
                _auditService = auditService;
                _rabbitMQOptions = rabbitMQOptions.Value;
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

                // Generate unique analysis ID (async pattern)
                var correlationId = Guid.NewGuid().ToString("N");
                var analysisId = $"async_analysis_{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss}_{correlationId[..8]}";

                // Create initial PlantAnalysis entity with "Processing" status
                var now = DateTime.Now;
                var analysis = new PlantAnalysis
                {
                    AnalysisId = analysisId,
                    UserId = request.TargetUserId,
                    ImageUrl = request.ImageUrl,
                    ImagePath = request.ImageUrl,
                    AnalysisStatus = "Processing",
                    Status = true,
                    CreatedDate = now,
                    Timestamp = now,
                    IsOnBehalfOf = true,
                    CreatedByAdminId = request.AdminUserId,
                    Notes = string.IsNullOrEmpty(request.Notes)
                        ? "[Created by Admin]"
                        : $"[Created by Admin] {request.Notes}"
                };

                // Save to database first
                _analysisRepository.Add(analysis);
                await _analysisRepository.SaveChangesAsync();

                // Create async request payload for RabbitMQ
                var asyncRequest = new PlantAnalysisAsyncRequestDto
                {
                    ImageUrl = request.ImageUrl,
                    Image = null, // URL-based, no base64
                    UserId = request.TargetUserId,
                    Notes = analysis.Notes,
                    ResponseQueue = "plant-analysis-results",
                    CorrelationId = correlationId,
                    AnalysisId = analysisId
                };

                // Publish to RabbitMQ queue
                var queueName = _rabbitMQOptions.Queues.PlantAnalysisRequest;
                var publishResult = await _messageQueueService.PublishAsync(queueName, asyncRequest, correlationId);

                if (!publishResult)
                {
                    // If publish fails, update status
                    analysis.AnalysisStatus = "QueueFailed";
                    _analysisRepository.Update(analysis);
                    await _analysisRepository.SaveChangesAsync();

                    return new ErrorDataResult<PlantAnalysis>("Failed to queue analysis for processing");
                }

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
                    reason: $"Queued async plant analysis for user {targetUser.FullName}",
                    afterState: new
                    {
                        analysis.AnalysisId,
                        analysis.UserId,
                        analysis.AnalysisStatus,
                        analysis.IsOnBehalfOf,
                        analysis.CreatedByAdminId
                    }
                );

                return new SuccessDataResult<PlantAnalysis>(
                    analysis,
                    $"Plant analysis queued successfully for user {targetUser.FullName}. Analysis ID: {analysisId}. Status will be updated when processing completes."
                );
            }
        }
    }
}
