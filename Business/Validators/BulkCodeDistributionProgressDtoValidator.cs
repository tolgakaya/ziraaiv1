using Entities.Dtos;
using FluentValidation;
using System.Linq;

namespace Business.Validators
{
    /// <summary>
    /// Validator for BulkCodeDistributionProgressDto
    /// Used for progress update validation
    /// </summary>
    public class BulkCodeDistributionProgressDtoValidator : AbstractValidator<BulkCodeDistributionProgressDto>
    {
        public BulkCodeDistributionProgressDtoValidator()
        {
            RuleFor(x => x.JobId)
                .GreaterThan(0)
                .WithMessage("JobId must be greater than 0");

            RuleFor(x => x.Status)
                .NotEmpty()
                .WithMessage("Status is required")
                .Must(status => new[] { "Pending", "Processing", "Completed", "PartialSuccess", "Failed" }.Contains(status))
                .WithMessage("Status must be one of: Pending, Processing, Completed, PartialSuccess, Failed");

            RuleFor(x => x.TotalFarmers)
                .GreaterThan(0)
                .WithMessage("TotalFarmers must be greater than 0");

            RuleFor(x => x.ProcessedFarmers)
                .GreaterThanOrEqualTo(0)
                .WithMessage("ProcessedFarmers cannot be negative")
                .LessThanOrEqualTo(x => x.TotalFarmers)
                .WithMessage("ProcessedFarmers cannot exceed TotalFarmers");

            RuleFor(x => x.SuccessfulDistributions)
                .GreaterThanOrEqualTo(0)
                .WithMessage("SuccessfulDistributions cannot be negative");

            RuleFor(x => x.FailedDistributions)
                .GreaterThanOrEqualTo(0)
                .WithMessage("FailedDistributions cannot be negative");

            RuleFor(x => x.ProgressPercentage)
                .GreaterThanOrEqualTo(0)
                .WithMessage("ProgressPercentage cannot be negative")
                .LessThanOrEqualTo(100)
                .WithMessage("ProgressPercentage cannot exceed 100");

            RuleFor(x => x.TotalCodesDistributed)
                .GreaterThanOrEqualTo(0)
                .WithMessage("TotalCodesDistributed cannot be negative");

            RuleFor(x => x.TotalSmsSent)
                .GreaterThanOrEqualTo(0)
                .WithMessage("TotalSmsSent cannot be negative");

            RuleFor(x => x)
                .Must(dto => dto.SuccessfulDistributions + dto.FailedDistributions == dto.ProcessedFarmers)
                .WithMessage("SuccessfulDistributions + FailedDistributions must equal ProcessedFarmers")
                .When(x => x.ProcessedFarmers > 0);
        }
    }
}
