using Entities.Dtos;
using FluentValidation;
using System.Linq;

namespace Business.Validators
{
    /// <summary>
    /// Validator for BulkCodeDistributionJobDto
    /// Used for API request validation
    /// </summary>
    public class BulkCodeDistributionJobDtoValidator : AbstractValidator<BulkCodeDistributionJobDto>
    {
        public BulkCodeDistributionJobDtoValidator()
        {
            RuleFor(x => x.JobId)
                .GreaterThan(0)
                .WithMessage("JobId must be greater than 0");

            RuleFor(x => x.TotalFarmers)
                .GreaterThan(0)
                .WithMessage("TotalFarmers must be greater than 0")
                .LessThanOrEqualTo(2000)
                .WithMessage("TotalFarmers cannot exceed 2000");

            RuleFor(x => x.TotalCodesRequired)
                .GreaterThan(0)
                .WithMessage("TotalCodesRequired must be greater than 0");

            RuleFor(x => x.AvailableCodes)
                .GreaterThanOrEqualTo(0)
                .WithMessage("AvailableCodes cannot be negative");

            RuleFor(x => x.Status)
                .NotEmpty()
                .WithMessage("Status is required")
                .Must(status => new[] { "Pending", "Processing", "Completed", "PartialSuccess", "Failed" }.Contains(status))
                .WithMessage("Status must be one of: Pending, Processing, Completed, PartialSuccess, Failed");
        }
    }
}
