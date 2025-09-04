using Business.Services.Notification.Models;
using Core.Utilities.Results;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Business.Services.Notification
{
    /// <summary>
    /// WhatsApp Business API service implementation for sending messages and managing templates
    /// </summary>
    public class WhatsAppService : IWhatsAppService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<WhatsAppService> _logger;
        private readonly string _baseUrl;
        private readonly string _accessToken;
        private readonly string _phoneNumberId;
        private readonly string _businessAccountId;

        public WhatsAppService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<WhatsAppService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            
            _baseUrl = _configuration["WhatsApp:ApiUrl"] ?? "https://graph.facebook.com/v18.0/";
            _accessToken = _configuration["WhatsApp:AccessToken"];
            _phoneNumberId = _configuration["WhatsApp:PhoneNumberId"];
            _businessAccountId = _configuration["WhatsApp:BusinessAccountId"];

            if (string.IsNullOrEmpty(_accessToken) || string.IsNullOrEmpty(_phoneNumberId))
            {
                _logger.LogWarning("WhatsApp configuration is incomplete. Service will not function properly.");
            }

            // Set default headers
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
        }

        /// <inheritdoc/>
        public async Task<IDataResult<string>> SendTemplateMessageAsync(
            string phoneNumber, 
            string templateName, 
            Dictionary<string, object> templateParameters, 
            string languageCode = "tr")
        {
            try
            {
                var validationResult = ValidatePhoneNumber(phoneNumber);
                if (!validationResult.Success)
                {
                    return new ErrorDataResult<string>(validationResult.Message);
                }

                var normalizedPhone = validationResult.Data;
                
                var message = new WhatsAppMessageDto
                {
                    To = normalizedPhone,
                    Type = "template",
                    Template = new WhatsAppTemplateDto
                    {
                        Name = templateName,
                        Language = new WhatsAppLanguageDto { Code = languageCode },
                        Components = BuildTemplateComponents(templateParameters)
                    }
                };

                var response = await SendMessageAsync(message);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending WhatsApp template message to {PhoneNumber} with template {TemplateName}", 
                    phoneNumber, templateName);
                return new ErrorDataResult<string>($"WhatsApp template message sending failed: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public async Task<IDataResult<string>> SendTextMessageAsync(
            string phoneNumber, 
            string message, 
            bool previewUrl = true)
        {
            try
            {
                var validationResult = ValidatePhoneNumber(phoneNumber);
                if (!validationResult.Success)
                {
                    return new ErrorDataResult<string>(validationResult.Message);
                }

                var normalizedPhone = validationResult.Data;

                if (string.IsNullOrWhiteSpace(message))
                {
                    return new ErrorDataResult<string>("Message content cannot be empty");
                }

                if (message.Length > 4096)
                {
                    return new ErrorDataResult<string>("Message content exceeds maximum length of 4096 characters");
                }

                var whatsAppMessage = new WhatsAppMessageDto
                {
                    To = normalizedPhone,
                    Type = "text",
                    Text = new WhatsAppTextDto
                    {
                        Body = message,
                        PreviewUrl = previewUrl
                    }
                };

                var response = await SendMessageAsync(whatsAppMessage);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending WhatsApp text message to {PhoneNumber}", phoneNumber);
                return new ErrorDataResult<string>($"WhatsApp text message sending failed: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public async Task<IDataResult<List<NotificationResultDto>>> SendBulkTemplateMessagesAsync(
            List<BulkWhatsAppRecipientDto> recipients,
            string templateName,
            string languageCode = "tr")
        {
            var results = new List<NotificationResultDto>();
            
            if (recipients == null || !recipients.Any())
            {
                return new ErrorDataResult<List<NotificationResultDto>>("Recipients list cannot be empty");
            }

            _logger.LogInformation("Starting bulk WhatsApp message sending to {RecipientCount} recipients with template {TemplateName}", 
                recipients.Count, templateName);

            foreach (var recipient in recipients)
            {
                try
                {
                    var result = await SendTemplateMessageAsync(
                        recipient.PhoneNumber, 
                        templateName, 
                        recipient.Parameters, 
                        languageCode);

                    var notificationResult = new NotificationResultDto
                    {
                        Success = result.Success,
                        MessageId = result.Data,
                        Channel = NotificationChannel.WhatsApp,
                        StatusMessage = result.Message,
                        ErrorDetails = result.Success ? null : result.Message,
                        DeliveryAttemptAt = DateTime.Now
                    };

                    results.Add(notificationResult);

                    // Add small delay to respect rate limits
                    await Task.Delay(100);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending bulk WhatsApp message to {PhoneNumber}", recipient.PhoneNumber);
                    results.Add(new NotificationResultDto
                    {
                        Success = false,
                        Channel = NotificationChannel.WhatsApp,
                        StatusMessage = "Sending failed",
                        ErrorDetails = ex.Message,
                        DeliveryAttemptAt = DateTime.Now
                    });
                }
            }

            var successCount = results.Count(r => r.Success);
            _logger.LogInformation("Bulk WhatsApp sending completed: {SuccessCount}/{TotalCount} messages sent successfully", 
                successCount, results.Count);

            return new SuccessDataResult<List<NotificationResultDto>>(results, 
                $"Bulk sending completed: {successCount}/{results.Count} messages sent successfully");
        }

        /// <inheritdoc/>
        public async Task<IDataResult<string>> GetMessageStatusAsync(string messageId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(messageId))
                {
                    return new ErrorDataResult<string>("Message ID cannot be empty");
                }

                var url = $"{_baseUrl}{messageId}";
                var response = await _httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    // Parse response to extract status
                    // Implementation depends on WhatsApp API response structure
                    _logger.LogDebug("Message status retrieved for ID {MessageId}: {Content}", messageId, content);
                    return new SuccessDataResult<string>("delivered", "Message status retrieved successfully");
                }
                else
                {
                    _logger.LogError("Failed to get message status for ID {MessageId}. Response: {Content}", messageId, content);
                    return new ErrorDataResult<string>("Failed to retrieve message status");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting message status for ID {MessageId}", messageId);
                return new ErrorDataResult<string>($"Error retrieving message status: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public async Task<IDataResult<List<WhatsAppTemplateInfoDto>>> GetApprovedTemplatesAsync()
        {
            try
            {
                var url = $"{_baseUrl}{_businessAccountId}/message_templates?fields=name,status,language,category,components";
                var response = await _httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    // Parse response and extract template information
                    // This would need to be implemented based on actual API response structure
                    var templates = new List<WhatsAppTemplateInfoDto>();
                    
                    _logger.LogDebug("Templates retrieved successfully: {Content}", content);
                    return new SuccessDataResult<List<WhatsAppTemplateInfoDto>>(templates, "Templates retrieved successfully");
                }
                else
                {
                    _logger.LogError("Failed to get templates. Response: {Content}", content);
                    return new ErrorDataResult<List<WhatsAppTemplateInfoDto>>("Failed to retrieve templates");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting WhatsApp templates");
                return new ErrorDataResult<List<WhatsAppTemplateInfoDto>>($"Error retrieving templates: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public IDataResult<string> ValidatePhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                return new ErrorDataResult<string>("Phone number cannot be empty");
            }

            // Remove all non-digit characters
            var cleanNumber = Regex.Replace(phoneNumber, @"[^\d]", "");

            // Turkish phone number validation and normalization
            if (cleanNumber.StartsWith("90"))
            {
                // Already has country code
                if (cleanNumber.Length == 12)
                {
                    return new SuccessDataResult<string>(cleanNumber, "Phone number is valid");
                }
            }
            else if (cleanNumber.StartsWith("0"))
            {
                // Remove leading zero and add country code
                cleanNumber = "90" + cleanNumber.Substring(1);
                if (cleanNumber.Length == 12)
                {
                    return new SuccessDataResult<string>(cleanNumber, "Phone number normalized and valid");
                }
            }
            else if (cleanNumber.StartsWith("5") && cleanNumber.Length == 10)
            {
                // Add Turkish country code
                cleanNumber = "90" + cleanNumber;
                return new SuccessDataResult<string>(cleanNumber, "Phone number normalized with country code");
            }

            return new ErrorDataResult<string>("Invalid phone number format. Expected Turkish mobile number (5XXXXXXXXX)");
        }

        /// <inheritdoc/>
        public async Task<IResult> HealthCheckAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(_accessToken) || string.IsNullOrEmpty(_phoneNumberId))
                {
                    return new ErrorResult("WhatsApp configuration is incomplete");
                }

                // Test API connectivity with a simple request
                var url = $"{_baseUrl}{_phoneNumberId}";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("WhatsApp API health check successful");
                    return new SuccessResult("WhatsApp API is healthy");
                }
                else
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogError("WhatsApp API health check failed. Status: {StatusCode}, Response: {Content}", 
                        response.StatusCode, content);
                    return new ErrorResult($"WhatsApp API health check failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during WhatsApp API health check");
                return new ErrorResult($"WhatsApp API health check error: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public async Task<IResult> ProcessWebhookAsync(WhatsAppWebhookDto webhookData)
        {
            try
            {
                if (webhookData?.Entry == null || !webhookData.Entry.Any())
                {
                    return new ErrorResult("Invalid webhook data");
                }

                _logger.LogDebug("Processing WhatsApp webhook with {EntryCount} entries", webhookData.Entry.Count);

                foreach (var entry in webhookData.Entry)
                {
                    foreach (var change in entry.Changes ?? new List<WhatsAppWebhookChangeDto>())
                    {
                        if (change.Value?.Statuses != null)
                        {
                            foreach (var status in change.Value.Statuses)
                            {
                                await ProcessMessageStatusUpdate(status);
                            }
                        }

                        if (change.Value?.Messages != null)
                        {
                            foreach (var message in change.Value.Messages)
                            {
                                await ProcessIncomingMessage(message);
                            }
                        }
                    }
                }

                return new SuccessResult("Webhook processed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing WhatsApp webhook");
                return new ErrorResult($"Webhook processing failed: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public async Task<IResult> MarkMessageAsReadAsync(string messageId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(messageId))
                {
                    return new ErrorResult("Message ID cannot be empty");
                }

                var url = $"{_baseUrl}{_phoneNumberId}/messages";
                var payload = new
                {
                    messaging_product = "whatsapp",
                    status = "read",
                    message_id = messageId
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogDebug("Message {MessageId} marked as read", messageId);
                    return new SuccessResult("Message marked as read");
                }
                else
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to mark message as read. Response: {Content}", responseContent);
                    return new ErrorResult("Failed to mark message as read");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking message as read: {MessageId}", messageId);
                return new ErrorResult($"Error marking message as read: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public async Task<IDataResult<string>> GetMediaUrlAsync(string mediaId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(mediaId))
                {
                    return new ErrorDataResult<string>("Media ID cannot be empty");
                }

                var url = $"{_baseUrl}{mediaId}";
                var response = await _httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    // Parse response to extract media URL
                    // Implementation depends on actual API response structure
                    _logger.LogDebug("Media URL retrieved for ID {MediaId}", mediaId);
                    return new SuccessDataResult<string>("media_url_here", "Media URL retrieved successfully");
                }
                else
                {
                    _logger.LogError("Failed to get media URL for ID {MediaId}. Response: {Content}", mediaId, content);
                    return new ErrorDataResult<string>("Failed to retrieve media URL");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting media URL for ID {MediaId}", mediaId);
                return new ErrorDataResult<string>($"Error retrieving media URL: {ex.Message}");
            }
        }

        #region Private Methods

        private async Task<IDataResult<string>> SendMessageAsync(WhatsAppMessageDto message)
        {
            try
            {
                var url = $"{_baseUrl}{_phoneNumberId}/messages";
                var json = JsonSerializer.Serialize(new
                {
                    messaging_product = "whatsapp",
                    to = message.To,
                    type = message.Type,
                    template = message.Template != null ? new
                    {
                        name = message.Template.Name,
                        language = new { code = message.Template.Language.Code },
                        components = message.Template.Components?.Select(c => new
                        {
                            type = c.Type,
                            parameters = c.Parameters?.Select(p => new
                            {
                                type = p.Type,
                                text = p.Text
                            }).ToArray()
                        }).ToArray()
                    } : null,
                    text = message.Text != null ? new
                    {
                        body = message.Text.Body,
                        preview_url = message.Text.PreviewUrl
                    } : null
                }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var responseData = JsonSerializer.Deserialize<WhatsAppResponseDto>(responseContent, 
                        new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                    
                    var messageId = responseData?.Messages?.FirstOrDefault()?.Id;
                    if (!string.IsNullOrEmpty(messageId))
                    {
                        _logger.LogInformation("WhatsApp message sent successfully to {PhoneNumber}. Message ID: {MessageId}", 
                            message.To, messageId);
                        return new SuccessDataResult<string>(messageId, "Message sent successfully");
                    }
                    else
                    {
                        _logger.LogWarning("WhatsApp message sent but no message ID returned. Response: {Response}", responseContent);
                        return new SuccessDataResult<string>("unknown", "Message sent but ID not available");
                    }
                }
                else
                {
                    _logger.LogError("Failed to send WhatsApp message to {PhoneNumber}. Status: {StatusCode}, Response: {Response}", 
                        message.To, response.StatusCode, responseContent);
                    return new ErrorDataResult<string>($"Failed to send message: {response.StatusCode} - {responseContent}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while sending WhatsApp message to {PhoneNumber}", message.To);
                return new ErrorDataResult<string>($"Exception during message sending: {ex.Message}");
            }
        }

        private List<WhatsAppComponentDto> BuildTemplateComponents(Dictionary<string, object> parameters)
        {
            if (parameters == null || !parameters.Any())
            {
                return new List<WhatsAppComponentDto>();
            }

            var components = new List<WhatsAppComponentDto>();
            
            // Create body component with parameters
            var bodyComponent = new WhatsAppComponentDto
            {
                Type = "body",
                Parameters = parameters.Values.Select(value => new WhatsAppParameterDto
                {
                    Type = "text",
                    Text = value?.ToString() ?? ""
                }).ToList()
            };

            components.Add(bodyComponent);
            return components;
        }

        private async Task ProcessMessageStatusUpdate(WhatsAppWebhookStatusDto status)
        {
            try
            {
                _logger.LogDebug("Processing message status update: ID {MessageId}, Status {Status}", 
                    status.Id, status.Status);

                // Here you would update your database with the new status
                // This could involve updating notification logs or triggering events
                
                await Task.CompletedTask; // Placeholder for actual implementation
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message status update for ID {MessageId}", status.Id);
            }
        }

        private async Task ProcessIncomingMessage(WhatsAppIncomingMessageDto message)
        {
            try
            {
                _logger.LogDebug("Processing incoming message: ID {MessageId}, From {From}, Type {Type}", 
                    message.Id, message.From, message.Type);

                // Here you would handle incoming messages (replies, opt-out requests, etc.)
                
                await Task.CompletedTask; // Placeholder for actual implementation
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing incoming message ID {MessageId}", message.Id);
            }
        }

        #endregion
    }
}