using Business.Services.FileStorage;
using Business.Services.Messaging;
using Business.Services.Sponsorship;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using MediatR;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.AnalysisMessages.Commands
{
    public class SendMessageWithAttachmentCommand : IRequest<IDataResult<AnalysisMessage>>
    {
        public int FromUserId { get; set; }
        public int ToUserId { get; set; }
        public int PlantAnalysisId { get; set; }
        public string Message { get; set; }
        public string MessageType { get; set; } = "Information";
        public List<IFormFile> Attachments { get; set; }

        public class SendMessageWithAttachmentCommandHandler : IRequestHandler<SendMessageWithAttachmentCommand, IDataResult<AnalysisMessage>>
        {
            private readonly IAnalysisMessageRepository _messageRepository;
            private readonly IAnalysisMessagingService _messagingService;
            private readonly IAttachmentValidationService _attachmentValidation;
            private readonly IFileStorageService _imageStorage; // FreeImageHost for images
            private readonly LocalFileStorageService _localStorage; // Local for documents

            public SendMessageWithAttachmentCommandHandler(
                IAnalysisMessageRepository messageRepository,
                IAnalysisMessagingService messagingService,
                IAttachmentValidationService attachmentValidation,
                IFileStorageService imageStorage, // FreeImageHost via DI
                LocalFileStorageService localStorage) // Local for non-images
            {
                _messageRepository = messageRepository;
                _messagingService = messagingService;
                _attachmentValidation = attachmentValidation;
                _imageStorage = imageStorage;
                _localStorage = localStorage;
            }

            public async Task<IDataResult<AnalysisMessage>> Handle(SendMessageWithAttachmentCommand request, CancellationToken cancellationToken)
            {
                // Validate messaging permission
                var canSend = await _messagingService.CanSendMessageForAnalysisAsync(
                    request.FromUserId,
                    request.ToUserId,
                    request.PlantAnalysisId);

                if (!canSend.canSend)
                    return new ErrorDataResult<AnalysisMessage>(canSend.errorMessage);

                // Validate attachments
                if (request.Attachments == null || !request.Attachments.Any())
                    return new ErrorDataResult<AnalysisMessage>("No attachments provided");

                var validationResult = await _attachmentValidation.ValidateAttachmentsAsync(
                    request.Attachments,
                    request.FromUserId);

                if (!validationResult.Success)
                    return new ErrorDataResult<AnalysisMessage>(validationResult.Message);

                // Upload attachments
                var uploadedUrls = new List<string>();
                var attachmentTypes = new List<string>();
                var attachmentSizes = new List<long>();
                var attachmentNames = new List<string>();
                var uploadedFileStorages = new List<bool>(); // Track which storage: true = image, false = local

                try
                {
                    foreach (var file in request.Attachments)
                    {
                        var fileName = $"msg_attachment_{request.FromUserId}_{DateTime.Now.Ticks}_{file.FileName}";
                        
                        // Select appropriate storage based on MIME type
                        var isImage = file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
                        var folder = isImage ? null : "attachments"; // Organize non-images in subfolder
                        
                        string url;
                        if (isImage)
                        {
                            // Use FreeImageHost for images
                            url = await _imageStorage.UploadFileAsync(file.OpenReadStream(), fileName, file.ContentType);
                        }
                        else
                        {
                            // Use local storage for documents, PDFs, etc.
                            url = await _localStorage.UploadFileAsync(file.OpenReadStream(), fileName, file.ContentType, folder);
                        }

                        if (string.IsNullOrEmpty(url))
                        {
                            // Cleanup uploaded files on failure
                            for (int i = 0; i < uploadedUrls.Count; i++)
                            {
                                try
                                {
                                    if (uploadedFileStorages[i])
                                        await _imageStorage.DeleteFileAsync(uploadedUrls[i]);
                                    else
                                        await _localStorage.DeleteFileAsync(uploadedUrls[i]);
                                }
                                catch
                                {
                                    // Log but don't fail cleanup
                                }
                            }
                            return new ErrorDataResult<AnalysisMessage>($"Failed to upload {file.FileName}");
                        }

                        uploadedUrls.Add(url);
                        attachmentTypes.Add(file.ContentType);
                        attachmentSizes.Add(file.Length);
                        attachmentNames.Add(file.FileName);
                        uploadedFileStorages.Add(isImage);
                    }

                    // Create message with attachments
                    var message = new AnalysisMessage
                    {
                        PlantAnalysisId = request.PlantAnalysisId,
                        FromUserId = request.FromUserId,
                        ToUserId = request.ToUserId,
                        Message = request.Message ?? string.Empty,
                        MessageType = request.MessageType,
                        MessageStatus = "Sent",
                        IsRead = false,
                        SentDate = DateTime.Now,
                        CreatedDate = DateTime.Now,

                        // Attachment metadata
                        HasAttachments = true,
                        AttachmentCount = uploadedUrls.Count,
                        AttachmentUrls = JsonSerializer.Serialize(uploadedUrls),
                        AttachmentTypes = JsonSerializer.Serialize(attachmentTypes),
                        AttachmentSizes = JsonSerializer.Serialize(attachmentSizes),
                        AttachmentNames = JsonSerializer.Serialize(attachmentNames)
                    };

                    _messageRepository.Add(message);

                    return new SuccessDataResult<AnalysisMessage>(
                        message,
                        $"Message sent with {uploadedUrls.Count} attachment(s)");
                }
                catch (Exception ex)
                {
                    // Cleanup on error
                    for (int i = 0; i < uploadedUrls.Count; i++)
                    {
                        try
                        {
                            if (uploadedFileStorages[i])
                                await _imageStorage.DeleteFileAsync(uploadedUrls[i]);
                            else
                                await _localStorage.DeleteFileAsync(uploadedUrls[i]);
                        }
                        catch
                        {
                            // Log but don't fail
                        }
                    }

                    return new ErrorDataResult<AnalysisMessage>($"Failed to send message with attachments: {ex.Message}");
                }
            }
        }
    }
}
