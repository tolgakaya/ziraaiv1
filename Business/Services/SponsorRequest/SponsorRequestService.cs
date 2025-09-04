using Business.Constants;
using Core.Utilities.Results;
using IResult = Core.Utilities.Results.IResult;
using DataAccess.Abstract;
using Entities.Concrete;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Microsoft.EntityFrameworkCore;

namespace Business.Services.SponsorRequest
{
    public class SponsorRequestService : ISponsorRequestService
    {
        private readonly IUserRepository _userRepository;
        private readonly ISponsorRequestRepository _sponsorRequestRepository;
        private readonly ISponsorshipCodeRepository _sponsorshipCodeRepository;
        private readonly IConfiguration _configuration;

        public SponsorRequestService(
            IUserRepository userRepository,
            ISponsorRequestRepository sponsorRequestRepository,
            ISponsorshipCodeRepository sponsorshipCodeRepository,
            IConfiguration configuration)
        {
            _userRepository = userRepository;
            _sponsorRequestRepository = sponsorRequestRepository;
            _sponsorshipCodeRepository = sponsorshipCodeRepository;
            _configuration = configuration;
        }

        [LogAspect(typeof(FileLogger))]
        public async Task<IDataResult<string>> CreateRequestAsync(int farmerId, string sponsorPhone, string message, int tierId)
        {
            try
            {
                // Get farmer details
                var farmer = await _userRepository.GetAsync(u => u.UserId == farmerId);
                if (farmer == null)
                {
                    return new ErrorDataResult<string>(Messages.UserNotFound);
                }

                // Find sponsor by phone
                var sponsor = await _userRepository.GetAsync(u => u.MobilePhones == sponsorPhone && u.Status);
                if (sponsor == null)
                {
                    return new ErrorDataResult<string>("Sponsor not found with the provided phone number");
                }

                // Generate unique request token
                var requestToken = GenerateRequestToken(farmer.MobilePhones, sponsorPhone, farmerId);
                
                // Check if request already exists
                var existingRequest = await _sponsorRequestRepository.GetAsync(
                    sr => sr.FarmerId == farmerId && 
                    sr.SponsorId == sponsor.UserId && 
                    sr.Status == "Pending");
                
                if (existingRequest != null)
                {
                    return new ErrorDataResult<string>("A pending request already exists for this sponsor");
                }

                // Create new request
                var sponsorRequest = new Entities.Concrete.SponsorRequest
                {
                    FarmerId = farmerId,
                    SponsorId = sponsor.UserId,
                    FarmerPhone = farmer.MobilePhones,
                    SponsorPhone = sponsorPhone,
                    RequestMessage = message ?? _configuration["SponsorRequest:DefaultRequestMessage"],
                    RequestToken = requestToken,
                    RequestDate = DateTime.Now,
                    Status = "Pending",
                    CreatedDate = DateTime.Now,
                    UpdatedDate = DateTime.Now
                };

                _sponsorRequestRepository.Add(sponsorRequest);
                await _sponsorRequestRepository.SaveChangesAsync();

                // Generate deeplink URL
                var baseUrl = _configuration["SponsorRequest:DeepLinkBaseUrl"] ?? "https://ziraai.com/sponsor-request/";
                var deeplinkUrl = $"{baseUrl}{requestToken}";

                return new SuccessDataResult<string>(deeplinkUrl, "Sponsor request created successfully");
            }
            catch (Exception ex)
            {
                return new ErrorDataResult<string>($"Error creating sponsor request: {ex.Message}");
            }
        }

        [LogAspect(typeof(FileLogger))]
        public async Task<IDataResult<Entities.Concrete.SponsorRequest>> ProcessDeeplinkAsync(string hashedToken)
        {
            try
            {
                // Validate and decode token
                var sponsorRequest = await ValidateRequestTokenAsync(hashedToken);
                if (sponsorRequest == null)
                {
                    return new ErrorDataResult<Entities.Concrete.SponsorRequest>("Invalid or expired request token");
                }

                // Check if request is still pending
                if (sponsorRequest.Status != "Pending")
                {
                    return new ErrorDataResult<Entities.Concrete.SponsorRequest>($"Request has already been {sponsorRequest.Status.ToLower()}");
                }

                // Check token expiry
                var tokenExpiryHours = _configuration.GetValue<int>("SponsorRequest:TokenExpiryHours", 24);
                if (DateTime.Now > sponsorRequest.RequestDate.AddHours(tokenExpiryHours))
                {
                    sponsorRequest.Status = "Expired";
                    sponsorRequest.UpdatedDate = DateTime.Now;
                    _sponsorRequestRepository.Update(sponsorRequest);
                    await _sponsorRequestRepository.SaveChangesAsync();
                    return new ErrorDataResult<Entities.Concrete.SponsorRequest>("Request token has expired");
                }

                return new SuccessDataResult<Entities.Concrete.SponsorRequest>(sponsorRequest, "Request validated successfully");
            }
            catch (Exception ex)
            {
                return new ErrorDataResult<Entities.Concrete.SponsorRequest>($"Error processing deeplink: {ex.Message}");
            }
        }

        [LogAspect(typeof(FileLogger))]
        public async Task<IResult> ApproveRequestsAsync(List<int> requestIds, int sponsorId, int tierId, string notes)
        {
            try
            {
                var approvedCount = 0;
                
                foreach (var requestId in requestIds)
                {
                    var request = await _sponsorRequestRepository.GetAsync(
                        sr => sr.Id == requestId && sr.SponsorId == sponsorId && sr.Status == "Pending");
                    
                    if (request == null)
                        continue;

                    // Generate sponsorship code
                    var sponsorshipCode = await GenerateSponsorshipCodeAsync(sponsorId, request.FarmerId, tierId);
                    
                    // Update request status
                    request.Status = "Approved";
                    request.ApprovalDate = DateTime.Now;
                    request.ApprovedSubscriptionTierId = tierId;
                    request.ApprovalNotes = notes;
                    request.GeneratedSponsorshipCode = sponsorshipCode.Code;
                    request.UpdatedDate = DateTime.Now;
                    
                    _sponsorRequestRepository.Update(request);
                    await _sponsorRequestRepository.SaveChangesAsync();
                    approvedCount++;
                }

                return new SuccessResult($"{approvedCount} requests approved successfully");
            }
            catch (Exception ex)
            {
                return new ErrorResult($"Error approving requests: {ex.Message}");
            }
        }

        [LogAspect(typeof(FileLogger))]
        public async Task<IDataResult<List<Entities.Concrete.SponsorRequest>>> GetPendingRequestsAsync(int sponsorId)
        {
            try
            {
                var requests = await _sponsorRequestRepository.GetListAsync(
                    sr => sr.SponsorId == sponsorId && sr.Status == "Pending");
                
                var requestList = requests.ToList();
                return new SuccessDataResult<List<Entities.Concrete.SponsorRequest>>(
                    requestList, 
                    $"Found {requestList.Count} pending requests");
            }
            catch (Exception ex)
            {
                return new ErrorDataResult<List<Entities.Concrete.SponsorRequest>>(
                    new List<Entities.Concrete.SponsorRequest>(), 
                    $"Error retrieving pending requests: {ex.Message}");
            }
        }

        public string GenerateWhatsAppMessage(Entities.Concrete.SponsorRequest request)
        {
            var baseUrl = _configuration["SponsorRequest:DeepLinkBaseUrl"] ?? "https://ziraai.com/sponsor-request/";
            var deeplinkUrl = $"{baseUrl}{request.RequestToken}";
            var message = request.RequestMessage ?? _configuration["SponsorRequest:DefaultRequestMessage"];
            
            // URL encode the message for WhatsApp
            var encodedMessage = Uri.EscapeDataString($"{message}\n\nOnaylamak için tıklayın: {deeplinkUrl}");
            
            // Generate WhatsApp URL
            return $"https://wa.me/{request.SponsorPhone}?text={encodedMessage}";
        }

        public string GenerateRequestToken(string farmerPhone, string sponsorPhone, int farmerId)
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var payload = $"{farmerId}:{farmerPhone}:{sponsorPhone}:{timestamp}";
            var secret = _configuration["Security:RequestTokenSecret"] ?? "DefaultSecretKey123!@#";
            
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
            {
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
                // Make URL-safe base64
                return Convert.ToBase64String(hash)
                    .Replace('+', '-')
                    .Replace('/', '_')
                    .Replace("=", "");
            }
        }

        public async Task<Entities.Concrete.SponsorRequest> ValidateRequestTokenAsync(string token)
        {
            try
            {
                // Find request by token
                var request = await _sponsorRequestRepository.GetAsync(sr => sr.RequestToken == token);
                return request;
            }
            catch
            {
                return null;
            }
        }

        private async Task<SponsorshipCode> GenerateSponsorshipCodeAsync(int sponsorId, int farmerId, int tierId)
        {
            // Generate unique code
            var random = new Random();
            string code;
            SponsorshipCode existingCode;
            
            do
            {
                code = $"SP-{sponsorId}-{random.Next(100000, 999999)}";
                existingCode = await _sponsorshipCodeRepository.GetAsync(sc => sc.Code == code);
            } while (existingCode != null);

            // Create sponsorship code
            var sponsorshipCode = new SponsorshipCode
            {
                Code = code,
                SponsorId = sponsorId,
                UsedByUserId = farmerId,
                SubscriptionTierId = tierId,
                CreatedDate = DateTime.Now,
                ExpiryDate = DateTime.Now.AddDays(30),
                IsActive = true,
                IsUsed = false
            };

            _sponsorshipCodeRepository.Add(sponsorshipCode);
            await _sponsorshipCodeRepository.SaveChangesAsync();
            return sponsorshipCode;
        }
    }
}