using Business.Services.Payment;
using Core.Configuration;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.Dtos.Payment;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Business.Services.Payment
{
    /// <summary>
    /// iyzico payment gateway service implementation
    /// Handles PWI (Pay With iyzico) integration with HMACSHA256 authentication
    /// </summary>
    public class IyzicoPaymentService : IIyzicoPaymentService
    {
        private readonly IPaymentTransactionRepository _paymentTransactionRepository;
        private readonly ISponsorshipPurchaseRepository _sponsorshipPurchaseRepository;
        private readonly IUserSubscriptionRepository _userSubscriptionRepository;
        private readonly ISubscriptionTierRepository _subscriptionTierRepository;
        private readonly IUserRepository _userRepository;
        private readonly IyzicoOptions _iyzicoOptions;
        private readonly ILogger<IyzicoPaymentService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public IyzicoPaymentService(
            IPaymentTransactionRepository paymentTransactionRepository,
            ISponsorshipPurchaseRepository sponsorshipPurchaseRepository,
            IUserSubscriptionRepository userSubscriptionRepository,
            ISubscriptionTierRepository subscriptionTierRepository,
            IUserRepository userRepository,
            IOptions<IyzicoOptions> iyzicoOptions,
            ILogger<IyzicoPaymentService> logger,
            IHttpClientFactory httpClientFactory)
        {
            _paymentTransactionRepository = paymentTransactionRepository;
            _sponsorshipPurchaseRepository = sponsorshipPurchaseRepository;
            _userSubscriptionRepository = userSubscriptionRepository;
            _subscriptionTierRepository = subscriptionTierRepository;
            _userRepository = userRepository;
            _iyzicoOptions = iyzicoOptions.Value;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IDataResult<PaymentInitializeResponseDto>> InitializePaymentAsync(
            int userId,
            PaymentInitializeRequestDto request)
        {
            try
            {
                _logger.LogInformation($"[iyzico] Initializing payment for User {userId}, FlowType: {request.FlowType}");

                // Validate flow type
                if (request.FlowType != PaymentFlowType.SponsorBulkPurchase &&
                    request.FlowType != PaymentFlowType.FarmerSubscription)
                {
                    return new ErrorDataResult<PaymentInitializeResponseDto>("Invalid flow type");
                }

                // Get user details
                var user = await _userRepository.GetAsync(u => u.UserId == userId);
                if (user == null)
                {
                    return new ErrorDataResult<PaymentInitializeResponseDto>("User not found");
                }

                // Calculate amount based on flow type
                decimal amount;
                string currency = request.Currency ?? _iyzicoOptions.Currency;
                int? subscriptionTierId = null;

                if (request.FlowType == PaymentFlowType.SponsorBulkPurchase)
                {
                    var flowData = JsonSerializer.Deserialize<SponsorBulkPurchaseFlowData>(
                        JsonSerializer.Serialize(request.FlowData));

                    var tier = await _subscriptionTierRepository.GetAsync(t => t.Id == flowData.SubscriptionTierId);
                    if (tier == null)
                    {
                        return new ErrorDataResult<PaymentInitializeResponseDto>("Subscription tier not found");
                    }

                    amount = tier.MonthlyPrice * flowData.Quantity;
                    subscriptionTierId = flowData.SubscriptionTierId;
                }
                else // FarmerSubscription
                {
                    var flowData = JsonSerializer.Deserialize<FarmerSubscriptionFlowData>(
                        JsonSerializer.Serialize(request.FlowData));

                    var tier = await _subscriptionTierRepository.GetAsync(t => t.Id == flowData.SubscriptionTierId);
                    if (tier == null)
                    {
                        return new ErrorDataResult<PaymentInitializeResponseDto>("Subscription tier not found");
                    }

                    amount = tier.MonthlyPrice * flowData.DurationMonths;
                    subscriptionTierId = flowData.SubscriptionTierId;
                }

                // Generate unique conversation ID
                var conversationId = $"{request.FlowType}_{userId}_{DateTime.Now.Ticks}";

                // Create payment transaction record
                var transaction = new PaymentTransaction
                {
                    UserId = userId,
                    FlowType = request.FlowType,
                    FlowDataJson = JsonSerializer.Serialize(request.FlowData),
                    Amount = amount,
                    Currency = currency,
                    Status = PaymentStatus.Initialized,
                    InitializedAt = DateTime.Now,
                    ExpiresAt = DateTime.Now.AddMinutes(_iyzicoOptions.TokenExpirationMinutes),
                    ConversationId = conversationId,
                    IyzicoToken = string.Empty, // Will be set after API call
                    CreatedDate = DateTime.Now
                };

                // Prepare iyzico PWI initialize request
                var iyzicoRequest = new
                {
                    locale = "tr",
                    conversationId = conversationId,
                    price = amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
                    paidPrice = amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
                    currency = currency,
                    paymentChannel = _iyzicoOptions.PaymentChannel,
                    paymentGroup = _iyzicoOptions.PaymentGroup,
                    callbackUrl = _iyzicoOptions.Callback.DeepLinkScheme,
                    enabledInstallments = new[] { 1 },
                    buyer = new
                    {
                        id = userId.ToString(),
                        name = "User",
                    surname = user.FullName ?? "User",
                        email = user.Email,
                        identityNumber = "11111111111", // Required by iyzico, dummy for now
                        registrationAddress = "N/A",
                        city = "Istanbul",
                        country = "Turkey",
                        ip = "127.0.0.1"
                    },
                    shippingAddress = new
                    {
                        address = "N/A",
                        contactName = user.FullName ?? "User",
                        city = "Istanbul",
                        country = "Turkey"
                    },
                    billingAddress = new
                    {
                        address = "N/A",
                        contactName = user.FullName ?? "User",
                        city = "Istanbul",
                        country = "Turkey"
                    },
                    basketItems = new[]
                    {
                        new
                        {
                            id = subscriptionTierId?.ToString() ?? "1",
                            price = amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
                            name = request.FlowType == PaymentFlowType.SponsorBulkPurchase
                                ? "Sponsorship Package"
                                : "Subscription",
                            category1 = "Subscription",
                            itemType = "VIRTUAL"
                        }
                    }
                };

                // Call iyzico PWI initialize endpoint
                var iyzicoResponse = await CallIyzicoApiAsync(
                    "/payment/iyzipos/checkoutform/initialize/auth/ecom",
                    iyzicoRequest);

                if (!iyzicoResponse.Success)
                {
                    _logger.LogError($"[iyzico] Initialize failed: {iyzicoResponse.Message}");
                    return new ErrorDataResult<PaymentInitializeResponseDto>(
                        $"Payment initialization failed: {iyzicoResponse.Message}");
                }

                var iyzicoData = JsonSerializer.Deserialize<JsonElement>(iyzicoResponse.Data);

                // Check iyzico response status
                if (iyzicoData.GetProperty("status").GetString() != "success")
                {
                    var errorMessage = iyzicoData.GetProperty("errorMessage").GetString();
                    _logger.LogError($"[iyzico] API returned error: {errorMessage}");
                    return new ErrorDataResult<PaymentInitializeResponseDto>($"iyzico error: {errorMessage}");
                }

                // Extract payment token and URL
                var token = iyzicoData.GetProperty("token").GetString();
                var paymentPageUrl = iyzicoData.GetProperty("paymentPageUrl").GetString();

                // Update transaction with token
                transaction.IyzicoToken = token;
                transaction.InitializeResponse = iyzicoResponse.Data;

                _paymentTransactionRepository.Add(transaction);
                await _paymentTransactionRepository.SaveChangesAsync();

                _logger.LogInformation($"[iyzico] Payment initialized successfully. TransactionId: {transaction.Id}, Token: {token}");

                // Build response
                var response = new PaymentInitializeResponseDto
                {
                    TransactionId = transaction.Id,
                    PaymentToken = token,
                    PaymentPageUrl = paymentPageUrl,
                    CallbackUrl = $"{_iyzicoOptions.Callback.DeepLinkScheme}?token={token}",
                    Amount = amount,
                    Currency = currency,
                    ExpiresAt = transaction.ExpiresAt.ToString("o"),
                    Status = PaymentStatus.Initialized,
                    ConversationId = conversationId
                };

                return new SuccessDataResult<PaymentInitializeResponseDto>(response, "Payment initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[iyzico] Exception during payment initialization");
                return new ErrorDataResult<PaymentInitializeResponseDto>($"Payment initialization failed: {ex.Message}");
            }
        }

        public async Task<IDataResult<PaymentVerifyResponseDto>> VerifyPaymentAsync(PaymentVerifyRequestDto request)
        {
            try
            {
                _logger.LogInformation($"[iyzico] Verifying payment token: {request.PaymentToken}");

                // Get transaction by token
                var transaction = await _paymentTransactionRepository.GetByIyzicoTokenAsync(request.PaymentToken);
                if (transaction == null)
                {
                    return new ErrorDataResult<PaymentVerifyResponseDto>("Payment transaction not found");
                }

                // Check if already verified
                if (transaction.Status == PaymentStatus.Success)
                {
                    _logger.LogInformation($"[iyzico] Payment already verified. TransactionId: {transaction.Id}");
                    return await BuildVerifyResponseAsync(transaction);
                }

                // Check if expired
                if (transaction.ExpiresAt < DateTime.Now)
                {
                    await _paymentTransactionRepository.UpdateStatusAsync(
                        transaction.Id,
                        PaymentStatus.Expired,
                        "Payment token expired");

                    return new ErrorDataResult<PaymentVerifyResponseDto>("Payment token has expired");
                }

                // Call iyzico retrieve payment details endpoint
                var iyzicoRequest = new
                {
                    locale = "tr",
                    conversationId = transaction.ConversationId,
                    token = request.PaymentToken
                };

                var iyzicoResponse = await CallIyzicoApiAsync(
                    "/payment/iyzipos/checkoutform/auth/ecom/detail",
                    iyzicoRequest);

                if (!iyzicoResponse.Success)
                {
                    _logger.LogError($"[iyzico] Verify failed: {iyzicoResponse.Message}");
                    await _paymentTransactionRepository.UpdateStatusAsync(
                        transaction.Id,
                        PaymentStatus.Failed,
                        iyzicoResponse.Message);

                    return new ErrorDataResult<PaymentVerifyResponseDto>(
                        $"Payment verification failed: {iyzicoResponse.Message}");
                }

                var iyzicoData = JsonSerializer.Deserialize<JsonElement>(iyzicoResponse.Data);

                // Check payment status
                var paymentStatus = iyzicoData.GetProperty("paymentStatus").GetString();
                var iyzicoPaymentId = iyzicoData.TryGetProperty("paymentId", out var paymentIdProp)
                    ? paymentIdProp.GetString()
                    : null;

                if (paymentStatus == "SUCCESS")
                {
                    // Mark transaction as completed
                    await _paymentTransactionRepository.MarkAsCompletedAsync(
                        transaction.Id,
                        iyzicoPaymentId,
                        iyzicoResponse.Data);

                    // Process based on flow type
                    await ProcessSuccessfulPaymentAsync(transaction);

                    _logger.LogInformation($"[iyzico] Payment verified successfully. TransactionId: {transaction.Id}, PaymentId: {iyzicoPaymentId}");

                    // Reload transaction to get updated data
                    transaction = await _paymentTransactionRepository.GetWithRelationsAsync(transaction.Id);
                    return await BuildVerifyResponseAsync(transaction);
                }
                else
                {
                    var errorMessage = iyzicoData.TryGetProperty("errorMessage", out var errorProp)
                        ? errorProp.GetString()
                        : "Payment failed";

                    await _paymentTransactionRepository.UpdateStatusAsync(
                        transaction.Id,
                        PaymentStatus.Failed,
                        errorMessage);

                    _logger.LogWarning($"[iyzico] Payment failed. TransactionId: {transaction.Id}, Reason: {errorMessage}");

                    return new ErrorDataResult<PaymentVerifyResponseDto>($"Payment failed: {errorMessage}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[iyzico] Exception during payment verification");
                return new ErrorDataResult<PaymentVerifyResponseDto>($"Payment verification failed: {ex.Message}");
            }
        }

        public async Task<IResult> ProcessWebhookAsync(PaymentWebhookDto webhook)
        {
            try
            {
                _logger.LogInformation($"[iyzico] Processing webhook for token: {webhook.Token}");

                // Get transaction
                var transaction = await _paymentTransactionRepository.GetByIyzicoTokenAsync(webhook.Token);
                if (transaction == null)
                {
                    _logger.LogWarning($"[iyzico] Webhook transaction not found: {webhook.Token}");
                    return new ErrorResult("Transaction not found");
                }

                // Update status based on webhook
                if (webhook.Status == "SUCCESS" && transaction.Status != PaymentStatus.Success)
                {
                    await _paymentTransactionRepository.MarkAsCompletedAsync(
                        transaction.Id,
                        webhook.PaymentId,
                        JsonSerializer.Serialize(webhook));

                    await ProcessSuccessfulPaymentAsync(transaction);

                    _logger.LogInformation($"[iyzico] Webhook processed successfully. TransactionId: {transaction.Id}");
                }
                else if (webhook.Status == "FAILURE")
                {
                    await _paymentTransactionRepository.UpdateStatusAsync(
                        transaction.Id,
                        PaymentStatus.Failed,
                        webhook.ErrorMessage);
                }

                return new SuccessResult("Webhook processed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[iyzico] Exception during webhook processing");
                return new ErrorResult($"Webhook processing failed: {ex.Message}");
            }
        }

        public async Task<IDataResult<int>> MarkExpiredTransactionsAsync()
        {
            try
            {
                var expiredTransactions = await _paymentTransactionRepository.GetExpiredTransactionsAsync();

                foreach (var transaction in expiredTransactions)
                {
                    await _paymentTransactionRepository.UpdateStatusAsync(
                        transaction.Id,
                        PaymentStatus.Expired,
                        "Payment token expired - marked by background job");
                }

                _logger.LogInformation($"[iyzico] Marked {expiredTransactions.Count} transactions as expired");

                return new SuccessDataResult<int>(expiredTransactions.Count,
                    $"{expiredTransactions.Count} transactions marked as expired");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[iyzico] Exception during expired transactions cleanup");
                return new ErrorDataResult<int>(0, $"Cleanup failed: {ex.Message}");
            }
        }

        public async Task<IDataResult<PaymentVerifyResponseDto>> GetPaymentStatusAsync(string token)
        {
            try
            {
                var transaction = await _paymentTransactionRepository.GetByIyzicoTokenAsync(token);
                if (transaction == null)
                {
                    return new ErrorDataResult<PaymentVerifyResponseDto>("Payment transaction not found");
                }

                return await BuildVerifyResponseAsync(transaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[iyzico] Exception during get payment status");
                return new ErrorDataResult<PaymentVerifyResponseDto>($"Get status failed: {ex.Message}");
            }
        }

        #region Private Helper Methods

        /// <summary>
        /// Call iyzico API with HMACSHA256 authentication
        /// </summary>
        private async Task<IDataResult<string>> CallIyzicoApiAsync(string endpoint, object requestBody)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.BaseAddress = new Uri(_iyzicoOptions.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(_iyzicoOptions.Timeout.InitializeTimeoutSeconds);

                var requestJson = JsonSerializer.Serialize(requestBody);
                var randomString = Guid.NewGuid().ToString();

                // Generate authorization header with HMACSHA256
                var authString = GenerateAuthorizationHeader(randomString, requestJson);

                var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                request.Headers.Add("Authorization", authString);
                request.Headers.Add("x-iyzi-rnd", randomString);
                request.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");

                _logger.LogDebug($"[iyzico] Calling {endpoint}");

                var response = await client.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"[iyzico] API call failed. Status: {response.StatusCode}, Response: {responseContent}");
                    return new ErrorDataResult<string>($"API call failed: {response.StatusCode}");
                }

                return new SuccessDataResult<string>(responseContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[iyzico] Exception calling API endpoint: {endpoint}");
                return new ErrorDataResult<string>($"API call exception: {ex.Message}");
            }
        }

        /// <summary>
        /// Generate HMACSHA256 authorization header for iyzico API
        /// Format: IYZWS {ApiKey}:{Base64(HMACSHA256({ApiKey}+{RandomString}+{SecretKey}))}
        /// </summary>
        private string GenerateAuthorizationHeader(string randomString, string requestBody)
        {
            var dataToEncrypt = randomString + requestBody;

            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_iyzicoOptions.SecretKey)))
            {
                var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(dataToEncrypt));
                var hashBase64 = Convert.ToBase64String(hashBytes);

                return $"IYZWS {_iyzicoOptions.ApiKey}:{hashBase64}";
            }
        }

        /// <summary>
        /// Process successful payment based on flow type
        /// </summary>
        private async Task ProcessSuccessfulPaymentAsync(PaymentTransaction transaction)
        {
            // This will be implemented in Phase 8 when we update sponsor purchase flow
            // For now, just log
            _logger.LogInformation($"[iyzico] Processing successful payment. TransactionId: {transaction.Id}, FlowType: {transaction.FlowType}");

            // TODO Phase 8:
            // - If SponsorBulkPurchase: Create SponsorshipPurchase and generate codes
            // - If FarmerSubscription: Create/update UserSubscription
        }

        /// <summary>
        /// Build PaymentVerifyResponseDto from transaction
        /// </summary>
        private async Task<IDataResult<PaymentVerifyResponseDto>> BuildVerifyResponseAsync(PaymentTransaction transaction)
        {
            var response = new PaymentVerifyResponseDto
            {
                TransactionId = transaction.Id,
                Status = transaction.Status,
                PaymentId = transaction.IyzicoPaymentId,
                PaymentToken = transaction.IyzicoToken,
                Amount = transaction.Amount,
                Currency = transaction.Currency,
                PaidAmount = transaction.Amount,
                CompletedAt = transaction.CompletedAt?.ToString("o"),
                ErrorMessage = transaction.ErrorMessage,
                FlowType = transaction.FlowType,
                FlowResult = null // Will be populated in Phase 8
            };

            return new SuccessDataResult<PaymentVerifyResponseDto>(response);
        }

        #endregion
    }
}
