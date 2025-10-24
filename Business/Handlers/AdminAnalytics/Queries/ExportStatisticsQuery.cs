using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Business.BusinessAspects;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using MediatR;

namespace Business.Handlers.AdminAnalytics.Queries
{
    /// <summary>
    /// Admin query to export statistics as CSV
    /// </summary>
    public class ExportStatisticsQuery : IRequest<IDataResult<byte[]>>
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public class ExportStatisticsQueryHandler : IRequestHandler<ExportStatisticsQuery, IDataResult<byte[]>>
        {
            private readonly IMediator _mediator;

            public ExportStatisticsQueryHandler(IMediator mediator)
            {
                _mediator = mediator;
            }

            [SecuredOperation(Priority = 1)]
            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<byte[]>> Handle(ExportStatisticsQuery request, CancellationToken cancellationToken)
            {
                // Get all statistics
                var userStats = await _mediator.Send(new GetUserStatisticsQuery 
                { 
                    StartDate = request.StartDate, 
                    EndDate = request.EndDate 
                });

                var subscriptionStats = await _mediator.Send(new GetSubscriptionStatisticsQuery 
                { 
                    StartDate = request.StartDate, 
                    EndDate = request.EndDate 
                });

                var sponsorshipStats = await _mediator.Send(new GetSponsorshipStatisticsQuery 
                { 
                    StartDate = request.StartDate, 
                    EndDate = request.EndDate 
                });

                // Build CSV
                var csv = new StringBuilder();
                csv.AppendLine("ZiraAI Admin Statistics Export");
                csv.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                csv.AppendLine($"Date Range: {request.StartDate?.ToString("yyyy-MM-dd") ?? "All"} to {request.EndDate?.ToString("yyyy-MM-dd") ?? "All"}");
                csv.AppendLine();

                // User Statistics
                csv.AppendLine("USER STATISTICS");
                csv.AppendLine("Metric,Value");
                csv.AppendLine($"Total Users,{userStats.Data.TotalUsers}");
                csv.AppendLine($"Active Users,{userStats.Data.ActiveUsers}");
                csv.AppendLine($"Inactive Users,{userStats.Data.InactiveUsers}");
                csv.AppendLine($"Registered Today,{userStats.Data.UsersRegisteredToday}");
                csv.AppendLine($"Registered This Week,{userStats.Data.UsersRegisteredThisWeek}");
                csv.AppendLine($"Registered This Month,{userStats.Data.UsersRegisteredThisMonth}");
                csv.AppendLine();

                // Subscription Statistics
                csv.AppendLine("SUBSCRIPTION STATISTICS");
                csv.AppendLine("Metric,Value");
                csv.AppendLine($"Total Subscriptions,{subscriptionStats.Data.TotalSubscriptions}");
                csv.AppendLine($"Active Subscriptions,{subscriptionStats.Data.ActiveSubscriptions}");
                csv.AppendLine($"Expired Subscriptions,{subscriptionStats.Data.ExpiredSubscriptions}");
                csv.AppendLine($"Trial Subscriptions,{subscriptionStats.Data.TrialSubscriptions}");
                csv.AppendLine($"Sponsored Subscriptions,{subscriptionStats.Data.SponsoredSubscriptions}");
                csv.AppendLine($"Paid Subscriptions,{subscriptionStats.Data.PaidSubscriptions}");
                csv.AppendLine($"Total Revenue,{subscriptionStats.Data.TotalRevenue:C}");
                csv.AppendLine($"Avg Subscription Duration (days),{subscriptionStats.Data.AverageSubscriptionDuration:F1}");
                csv.AppendLine();

                // Sponsorship Statistics
                csv.AppendLine("SPONSORSHIP STATISTICS");
                csv.AppendLine("Metric,Value");
                csv.AppendLine($"Total Purchases,{sponsorshipStats.Data.TotalPurchases}");
                csv.AppendLine($"Completed Purchases,{sponsorshipStats.Data.CompletedPurchases}");
                csv.AppendLine($"Pending Purchases,{sponsorshipStats.Data.PendingPurchases}");
                csv.AppendLine($"Total Revenue,{sponsorshipStats.Data.TotalRevenue:C}");
                csv.AppendLine($"Codes Generated,{sponsorshipStats.Data.TotalCodesGenerated}");
                csv.AppendLine($"Codes Used,{sponsorshipStats.Data.TotalCodesUsed}");
                csv.AppendLine($"Code Redemption Rate,{sponsorshipStats.Data.CodeRedemptionRate:F2}%");
                csv.AppendLine($"Unique Sponsors,{sponsorshipStats.Data.UniqueSponsorCount}");

                var bytes = Encoding.UTF8.GetBytes(csv.ToString());
                return new SuccessDataResult<byte[]>(bytes, "Statistics exported successfully");
            }
        }
    }
}
