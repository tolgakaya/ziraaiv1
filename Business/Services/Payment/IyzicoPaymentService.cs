using Business.Services.Payment;
using Core.Configuration;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.Dtos.Payment;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Globalization;
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
        private readonly ISponsorshipCodeRepository _sponsorshipCodeRepository;
        private readonly IUserSubscriptionRepository _userSubscriptionRepository;
        private readonly ISubscriptionTierRepository _subscriptionTierRepository;
        private readonly IUserRepository _userRepository;
        private readonly IyzicoOptions _iyzicoOptions;
        private readonly ILogger<IyzicoPaymentService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public IyzicoPaymentService(
            IPaymentTransactionRepository paymentTransactionRepository,
            ISponsorshipPurchaseRepository sponsorshipPurchaseRepository,
            ISponsorshipCodeRepository sponsorshipCodeRepository,
            IUserSubscriptionRepository userSubscriptionRepository,
            ISubscriptionTierRepository subscriptionTierRepository,
            IUserRepository userRepository,
            IOptions<IyzicoOptions> iyzicoOptions,
            ILogger<IyzicoPaymentService> logger,
            IHttpClientFactory httpClientFactory)
        {
            _paymentTransactionRepository = paymentTransactionRepository;
            _sponsorshipPurchaseRepository = sponsorshipPurchaseRepository;
            _sponsorshipCodeRepository = sponsorshipCodeRepository;
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
                    // Handle JsonElement from ASP.NET Core model binding
                    SponsorBulkPurchaseFlowData flowData;
                    if (request.FlowData is JsonElement jsonElement)
                    {
                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        flowData = JsonSerializer.Deserialize<SponsorBulkPurchaseFlowData>(jsonElement.GetRawText(), options);
                    }
                    else
                    {
                        flowData = JsonSerializer.Deserialize<SponsorBulkPurchaseFlowData>(
                            JsonSerializer.Serialize(request.FlowData));
                    }

                    _logger.LogInformation($"[iyzico] SponsorBulkPurchase - TierId: {flowData?.SubscriptionTierId}, Quantity: {flowData?.Quantity}");

                    if (flowData == null)
                    {
                        _logger.LogError("[iyzico] FlowData deserialization returned null");
                        return new ErrorDataResult<PaymentInitializeResponseDto>("Invalid flow data");
                    }

                    var tier = await _subscriptionTierRepository.GetAsync(t => t.Id == flowData.SubscriptionTierId);
                    if (tier == null)
                    {
                        _logger.LogError($"[iyzico] Subscription tier not found in database. TierId: {flowData.SubscriptionTierId}");
                        return new ErrorDataResult<PaymentInitializeResponseDto>("Subscription tier not found");
                    }

                    _logger.LogInformation($"[iyzico] Found tier: {tier.TierName}, Price: {tier.MonthlyPrice}, IsActive: {tier.IsActive}");

                    amount = tier.MonthlyPrice * flowData.Quantity;
                    subscriptionTierId = flowData.SubscriptionTierId;
                }
                else // FarmerSubscription
                {
                    // Handle JsonElement from ASP.NET Core model binding
                    FarmerSubscriptionFlowData flowData;
                    if (request.FlowData is JsonElement jsonElement)
                    {
                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        flowData = JsonSerializer.Deserialize<FarmerSubscriptionFlowData>(jsonElement.GetRawText(), options);
                    }
                    else
                    {
                        flowData = JsonSerializer.Deserialize<FarmerSubscriptionFlowData>(
                            JsonSerializer.Serialize(request.FlowData));
                    }

                    _logger.LogInformation($"[iyzico] FarmerSubscription - TierId: {flowData?.SubscriptionTierId}, Duration: {flowData?.DurationMonths}");

                    if (flowData == null)
                    {
                        _logger.LogError("[iyzico] FlowData deserialization returned null");
                        return new ErrorDataResult<PaymentInitializeResponseDto>("Invalid flow data");
                    }

                    var tier = await _subscriptionTierRepository.GetAsync(t => t.Id == flowData.SubscriptionTierId);
                    if (tier == null)
                    {
                        _logger.LogError($"[iyzico] Subscription tier not found in database. TierId: {flowData.SubscriptionTierId}");
                        return new ErrorDataResult<PaymentInitializeResponseDto>("Subscription tier not found");
                    }

                    _logger.LogInformation($"[iyzico] Found tier: {tier.TierName}, Price: {tier.MonthlyPrice}, IsActive: {tier.IsActive}");

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
                // IMPORTANT: price and paidPrice must be numeric (decimal), NOT strings
                var iyzicoRequest = new
                {
                    locale = "tr",
                    conversationId = conversationId,
                    price = amount, // Numeric decimal value
                    paidPrice = amount, // Numeric decimal value
                    currency = currency,
                    basketId = conversationId, // Use conversationId as basketId
                    paymentChannel = _iyzicoOptions.PaymentChannel,
                    paymentGroup = _iyzicoOptions.PaymentGroup,
                    callbackUrl = _iyzicoOptions.Callback.DeepLinkScheme,
                    enabledInstallments = new[] { 1 },
                    buyer = new
                    {
                        id = userId.ToString(),
                        name = !string.IsNullOrEmpty(user.FullName) && user.FullName.Contains(' ')
                            ? user.FullName.Split(' ')[0]
                            : "User",
                        surname = !string.IsNullOrEmpty(user.FullName) && user.FullName.Contains(' ')
                            ? string.Join(" ", user.FullName.Split(' ').Skip(1))
                            : user.FullName ?? "User",
                        email = user.Email,
                        gsmNumber = !string.IsNullOrEmpty(user.MobilePhones) ? user.MobilePhones : "+905350000000",
                        identityNumber = "11111111111", // Required by iyzico, test value for sandbox
                        registrationDate = user.RecordDate.ToString("yyyy-MM-dd HH:mm:ss"),
                        lastLoginDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        registrationAddress = !string.IsNullOrEmpty(user.Address) ? user.Address : "Istanbul, Turkey",
                        city = "Istanbul",
                        country = "Turkey",
                        zipCode = "34732",
                        ip = "127.0.0.1"
                    },
                    shippingAddress = new
                    {
                        address = "Istanbul, Turkey",
                        zipCode = "34742",
                        contactName = user.FullName ?? "User",
                        city = "Istanbul",
                        country = "Turkey"
                    },
                    billingAddress = new
                    {
                        address = "Istanbul, Turkey",
                        zipCode = "34742",
                        contactName = user.FullName ?? "User",
                        city = "Istanbul",
                        country = "Turkey"
                    },
                    basketItems = new[]
                    {
                        new
                        {
                            id = subscriptionTierId?.ToString() ?? "1",
                            price = amount, // Numeric decimal value
                            name = request.FlowType == PaymentFlowType.SponsorBulkPurchase
                                ? "Sponsorship Package"
                                : "Subscription",
                            category1 = "Subscription",
                            category2 = "Service",
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

                // IMPORTANT: iyzico puts response in Message field, not Data field
                var responseJson = iyzicoResponse.Message;
                if (string.IsNullOrEmpty(responseJson))
                {
                    _logger.LogError($"[iyzico] Response is null or empty. Success: {iyzicoResponse.Success}");
                    return new ErrorDataResult<PaymentInitializeResponseDto>("iyzico API returned empty response");
                }

                _logger.LogDebug($"[iyzico] Raw response: {responseJson}");
                var iyzicoData = JsonSerializer.Deserialize<JsonElement>(responseJson);

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

                // IMPORTANT: iyzico puts response in Message field, not Data field
                var responseJson = iyzicoResponse.Message;
                if (string.IsNullOrEmpty(responseJson))
                {
                    _logger.LogError($"[iyzico] Verify response is null or empty. Success: {iyzicoResponse.Success}");
                    return new ErrorDataResult<PaymentVerifyResponseDto>("iyzico API returned empty response");
                }

                _logger.LogDebug($"[iyzico] Verify response: {responseJson}");
                var iyzicoData = JsonSerializer.Deserialize<JsonElement>(responseJson);

                // Verify response signature
                var responseSignature = iyzicoData.GetProperty("signature").GetString();
                if (!VerifyResponseSignature(iyzicoData, responseSignature))
                {
                    _logger.LogError("[iyzico] Response signature verification failed");
                    await _paymentTransactionRepository.UpdateStatusAsync(
                        transaction.Id,
                        PaymentStatus.Failed,
                        "Response signature verification failed");

                    return new ErrorDataResult<PaymentVerifyResponseDto>("Invalid response signature");
                }

                _logger.LogInformation("[iyzico] Response signature verified successfully");

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
                        responseJson);

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
                var authString = GenerateAuthorizationHeader(randomString, endpoint, requestJson);

                var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                request.Headers.Add("Authorization", authString);
                request.Headers.Add("x-iyzi-rnd", randomString);
                request.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");

                _logger.LogDebug($"[iyzico] Calling {endpoint}");
                _logger.LogDebug($"[iyzico] Random: {randomString}");
                _logger.LogDebug($"[iyzico] Auth Header: {authString}");

                // IMPORTANT: Log FULL request body to diagnose Error 11 on iyzico payment page
                _logger.LogInformation($"[iyzico] FULL Request Body: {requestJson}");

                var response = await client.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"[iyzico] API call failed. Status: {response.StatusCode}, Response: {responseContent}");
                    return new ErrorDataResult<string>($"API call failed: {response.StatusCode}");
                }

                // Parse iyzico response to check for their own error format
                try
                {
                    var jsonDoc = JsonDocument.Parse(responseContent);
                    if (jsonDoc.RootElement.TryGetProperty("status", out var status))
                    {
                        var statusValue = status.GetString();
                        if (statusValue == "failure")
                        {
                            var errorCode = jsonDoc.RootElement.TryGetProperty("errorCode", out var ec) ? ec.GetString() : "unknown";
                            var errorMessage = jsonDoc.RootElement.TryGetProperty("errorMessage", out var em) ? em.GetString() : "Unknown error";
                            _logger.LogError($"[iyzico] API returned error. Code: {errorCode}, Message: {errorMessage}");
                            return new ErrorDataResult<string>($"iyzico error [{errorCode}]: {errorMessage}");
                        }
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning($"[iyzico] Could not parse response as JSON: {ex.Message}");
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
        /// Format: IYZWSv2 base64(apiKey:randomKey:signature)
        /// Signature: HMACSHA256(randomKey + uri.path + request.body, secretKey)
        /// Reference: https://docs.iyzico.com/on-hazirliklar/kimlik-dogrulama/hmacsha256-kimlik-dogrulama
        /// </summary>
        private string GenerateAuthorizationHeader(string randomKey, string uriPath, string requestBody)
        {
            // Step 1: Create payload to hash: randomKey + uri.path + request.body
            var dataToHash = randomKey + uriPath + requestBody;

            _logger.LogDebug($"[iyzico] Random Key: {randomKey}");
            _logger.LogDebug($"[iyzico] URI Path: {uriPath}");
            _logger.LogDebug($"[iyzico] Data to hash length: {dataToHash.Length}");
            _logger.LogDebug($"[iyzico] Data to hash (first 150 chars): {dataToHash.Substring(0, Math.Min(150, dataToHash.Length))}");

            // Step 2: Hash with HMAC-SHA256 using secretKey
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_iyzicoOptions.SecretKey)))
            {
                var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(dataToHash));
                // CRITICAL: Postman uses HEX encoding for signature, NOT Base64!
                var signature = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

                _logger.LogDebug($"[iyzico] Signature (hex): {signature}");

                // Step 3: Create authorization string: apiKey:VALUE&randomKey:VALUE&signature:HEX_VALUE
                var authString = $"apiKey:{_iyzicoOptions.ApiKey}&randomKey:{randomKey}&signature:{signature}";
                _logger.LogDebug($"[iyzico] Auth string (before base64): {authString}");

                // Step 4: Base64 encode the authorization string
                var authBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(authString));

                // Step 5: Return with IYZWSv2 prefix
                var finalHeader = $"IYZWSv2 {authBase64}";
                _logger.LogDebug($"[iyzico] Final Authorization Header: {finalHeader}");

                return finalHeader;
            }
        }

        /// <summary>
        /// Process successful payment based on flow type
        /// Creates SponsorshipPurchase with codes for sponsors, or UserSubscription for farmers
        /// </summary>
        private async Task ProcessSuccessfulPaymentAsync(PaymentTransaction transaction)
        {
            _logger.LogInformation($"[iyzico] Processing successful payment. TransactionId: {transaction.Id}, FlowType: {transaction.FlowType}");

            try
            {
                if (transaction.FlowType == PaymentFlowType.SponsorBulkPurchase)
                {
                    await ProcessSponsorBulkPurchaseAsync(transaction);
                }
                else if (transaction.FlowType == PaymentFlowType.FarmerSubscription)
                {
                    await ProcessFarmerSubscriptionAsync(transaction);
                }
                else
                {
                    _logger.LogWarning($"[iyzico] Unknown flow type: {transaction.FlowType}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[iyzico] Error processing successful payment. TransactionId: {transaction.Id}");
                throw;
            }
        }

        /// <summary>
        /// Process sponsor bulk purchase: Create SponsorshipPurchase and generate codes
        /// </summary>
        private async Task ProcessSponsorBulkPurchaseAsync(PaymentTransaction transaction)
        {
            _logger.LogInformation($"[iyzico] Processing sponsor bulk purchase. TransactionId: {transaction.Id}");

            // Deserialize flow data
            var flowData = JsonSerializer.Deserialize<SponsorBulkPurchaseFlowData>(transaction.FlowDataJson);

            // Get subscription tier
            var tier = await _subscriptionTierRepository.GetAsync(t => t.Id == flowData.SubscriptionTierId);
            if (tier == null)
            {
                _logger.LogError($"[iyzico] Subscription tier not found: {flowData.SubscriptionTierId}");
                return;
            }

            // Create sponsorship purchase record
            var purchase = new SponsorshipPurchase
            {
                SponsorId = transaction.UserId,
                SubscriptionTierId = flowData.SubscriptionTierId,
                Quantity = flowData.Quantity,
                UnitPrice = tier.MonthlyPrice,
                TotalAmount = transaction.Amount,
                Currency = transaction.Currency,
                PurchaseDate = DateTime.Now,
                PaymentMethod = "CreditCard",
                PaymentReference = transaction.IyzicoPaymentId,
                PaymentStatus = "Completed",
                PaymentCompletedDate = transaction.CompletedAt,
                PaymentTransactionId = transaction.Id,
                CodePrefix = "AGRI",
                ValidityDays = 30,
                Status = "Active",
                CreatedDate = DateTime.Now,
                CodesGenerated = 0,
                CodesUsed = 0
            };

            _sponsorshipPurchaseRepository.Add(purchase);
            await _sponsorshipPurchaseRepository.SaveChangesAsync();

            _logger.LogInformation($"[iyzico] Sponsorship purchase created. PurchaseId: {purchase.Id}");

            // Generate sponsorship codes
            var codes = await _sponsorshipCodeRepository.GenerateCodesAsync(
                purchase.Id,
                transaction.UserId,
                flowData.SubscriptionTierId,
                flowData.Quantity,
                purchase.CodePrefix,
                purchase.ValidityDays);

            // Update codes generated count
            purchase.CodesGenerated = codes.Count;
            _sponsorshipPurchaseRepository.Update(purchase);
            await _sponsorshipPurchaseRepository.SaveChangesAsync();

            // Link purchase to transaction
            transaction.SponsorshipPurchaseId = purchase.Id;
            _paymentTransactionRepository.Update(transaction);
            await _paymentTransactionRepository.SaveChangesAsync();

            _logger.LogInformation($"[iyzico] Generated {codes.Count} sponsorship codes. PurchaseId: {purchase.Id}");
        }

        /// <summary>
        /// Process farmer subscription: Create or update UserSubscription
        /// </summary>
        private async Task ProcessFarmerSubscriptionAsync(PaymentTransaction transaction)
        {
            _logger.LogInformation($"[iyzico] Processing farmer subscription. TransactionId: {transaction.Id}");

            // Deserialize flow data
            var flowData = JsonSerializer.Deserialize<FarmerSubscriptionFlowData>(transaction.FlowDataJson);

            // Get subscription tier
            var tier = await _subscriptionTierRepository.GetAsync(t => t.Id == flowData.SubscriptionTierId);
            if (tier == null)
            {
                _logger.LogError($"[iyzico] Subscription tier not found: {flowData.SubscriptionTierId}");
                return;
            }

            // Check if user already has a subscription
            var existingSubscription = await _userSubscriptionRepository.GetActiveSubscriptionByUserIdAsync(transaction.UserId);

            if (existingSubscription != null)
            {
                // Extend existing subscription
                var extensionDays = flowData.DurationMonths * 30;
                existingSubscription.EndDate = existingSubscription.EndDate.AddDays(extensionDays);
                existingSubscription.AutoRenew = false; // Disable auto-renew for manual purchases
                existingSubscription.UpdatedDate = DateTime.Now;
                existingSubscription.PaymentTransactionId = transaction.Id;

                _userSubscriptionRepository.Update(existingSubscription);
                await _userSubscriptionRepository.SaveChangesAsync();

                transaction.UserSubscriptionId = existingSubscription.Id;

                _logger.LogInformation($"[iyzico] Extended existing subscription. SubscriptionId: {existingSubscription.Id}, ExtensionDays: {extensionDays}");
            }
            else
            {
                // Create new subscription
                var startDate = DateTime.Now;
                var endDate = startDate.AddMonths(flowData.DurationMonths);

                var subscription = new UserSubscription
                {
                    UserId = transaction.UserId,
                    SubscriptionTierId = flowData.SubscriptionTierId,
                    StartDate = startDate,
                    EndDate = endDate,
                    IsActive = true,
                    AutoRenew = false,
                    PaymentMethod = "CreditCard",
                    PaymentReference = transaction.IyzicoPaymentId,
                    PaymentTransactionId = transaction.Id,
                    PaidAmount = transaction.Amount,
                    Currency = transaction.Currency,
                    LastPaymentDate = DateTime.Now,
                    CurrentDailyUsage = 0,
                    CurrentMonthlyUsage = 0,
                    LastUsageResetDate = startDate,
                    MonthlyUsageResetDate = startDate,
                    Status = "Active",
                    IsTrialSubscription = false,
                    IsSponsoredSubscription = false,
                    QueueStatus = SubscriptionQueueStatus.Active,
                    ActivatedDate = startDate,
                    CreatedDate = DateTime.Now,
                    UpdatedDate = DateTime.Now
                };

                _userSubscriptionRepository.Add(subscription);
                await _userSubscriptionRepository.SaveChangesAsync();

                transaction.UserSubscriptionId = subscription.Id;

                _logger.LogInformation($"[iyzico] Created new subscription. SubscriptionId: {subscription.Id}, Duration: {flowData.DurationMonths} months");
            }

            // Update transaction with subscription ID
            _paymentTransactionRepository.Update(transaction);
            await _paymentTransactionRepository.SaveChangesAsync();
        }

        /// <summary>
        /// Build PaymentVerifyResponseDto from transaction
        /// Populates FlowResult based on transaction flow type and linked entities
        /// </summary>
        private async Task<IDataResult<PaymentVerifyResponseDto>> BuildVerifyResponseAsync(PaymentTransaction transaction)
        {
            object flowResult = null;

            // Populate FlowResult based on flow type
            if (transaction.Status == PaymentStatus.Success)
            {
                if (transaction.FlowType == PaymentFlowType.SponsorBulkPurchase && transaction.SponsorshipPurchaseId.HasValue)
                {
                    var purchase = await _sponsorshipPurchaseRepository.GetWithCodesAsync(transaction.SponsorshipPurchaseId.Value);
                    if (purchase != null)
                    {
                        flowResult = new SponsorBulkPurchaseResult
                        {
                            PurchaseId = purchase.Id,
                            CodesGenerated = purchase.CodesGenerated,
                            SubscriptionTierName = purchase.SubscriptionTier?.DisplayName ?? "Unknown"
                        };
                    }
                }
                else if (transaction.FlowType == PaymentFlowType.FarmerSubscription && transaction.UserSubscriptionId.HasValue)
                {
                    var subscription = await _userSubscriptionRepository.GetSubscriptionWithTierAsync(transaction.UserSubscriptionId.Value);
                    if (subscription != null)
                    {
                        flowResult = new FarmerSubscriptionResult
                        {
                            SubscriptionId = subscription.Id,
                            SubscriptionTierName = subscription.SubscriptionTier?.DisplayName ?? "Unknown",
                            StartDate = subscription.StartDate.ToString("o"),
                            EndDate = subscription.EndDate.ToString("o")
                        };
                    }
                }
            }

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
                FlowResult = flowResult
            };

            return new SuccessDataResult<PaymentVerifyResponseDto>(response);
        }

        /// <summary>
        /// Verify iyzico response signature to ensure response authenticity
        /// Signature format: HMACSHA256(field1:field2:...:fieldN, secretKey) in HEX
        /// </summary>
        private bool VerifyResponseSignature(JsonElement responseData, string expectedSignature)
        {
            try
            {
                // Extract fields in exact order as per Postman collection (lines 2615-2622)
                var paymentStatus = responseData.GetProperty("paymentStatus").GetString();
                var paymentId = responseData.TryGetProperty("paymentId", out var pidProp) ? pidProp.GetString() : "";
                var currency = responseData.GetProperty("currency").GetString();
                var basketId = responseData.GetProperty("basketId").GetString();
                var conversationId = responseData.GetProperty("conversationId").GetString();

                // Format decimal values (remove trailing zeros, as per formatBigDecimal in Postman)
                var paidPrice = FormatBigDecimal(responseData.GetProperty("paidPrice").GetDecimal());
                var price = FormatBigDecimal(responseData.GetProperty("price").GetDecimal());

                var token = responseData.GetProperty("token").GetString();

                // Build signature data string
                var dataToEncrypt = $"{paymentStatus}:{paymentId}:{currency}:{basketId}:{conversationId}:{paidPrice}:{price}:{token}";

                _logger.LogDebug($"[iyzico] Signature verification data: {dataToEncrypt}");

                // Calculate signature using HMACSHA256 with HEX encoding
                using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_iyzicoOptions.SecretKey)))
                {
                    var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(dataToEncrypt));
                    var calculatedSignature = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

                    _logger.LogDebug($"[iyzico] Expected signature: {expectedSignature}");
                    _logger.LogDebug($"[iyzico] Calculated signature: {calculatedSignature}");

                    return calculatedSignature.Equals(expectedSignature, StringComparison.OrdinalIgnoreCase);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[iyzico] Error verifying response signature");
                return false;
            }
        }

        /// <summary>
        /// Format decimal value by removing trailing zeros (matches Postman's formatBigDecimal)
        /// Example: 4999.50 -> "4999.5", 100.00 -> "100"
        /// </summary>
        private string FormatBigDecimal(decimal value)
        {
            var str = value.ToString(CultureInfo.InvariantCulture);

            if (str.Contains('.'))
            {
                // Remove trailing zeros after decimal point
                str = System.Text.RegularExpressions.Regex.Replace(str, @"(\.[0-9]*?)0+$", "$1");
                // Remove decimal point if no digits remain after it
                str = System.Text.RegularExpressions.Regex.Replace(str, @"\.$", "");
            }

            return str;
        }

        #endregion
    }
}
