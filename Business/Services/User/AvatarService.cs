using Business.Services.FileStorage;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Dtos;
using Microsoft.AspNetCore.Http;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UserEntity = Core.Entities.Concrete.User;
using IResult = Core.Utilities.Results.IResult;

namespace Business.Services.User
{
    public class AvatarService : IAvatarService
    {
        private readonly IUserRepository _userRepository;
        private readonly IFileStorageService _fileStorageService;
        private const int AVATAR_SIZE = 512; // Full size avatar
        private const int THUMBNAIL_SIZE = 128; // Thumbnail size
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        private const long MaxFileSize = 5 * 1024 * 1024; // 5MB

        public AvatarService(
            IUserRepository userRepository,
            IFileStorageService fileStorageService)
        {
            _userRepository = userRepository;
            _fileStorageService = fileStorageService;
        }

        public async Task<IDataResult<AvatarUploadResult>> UploadAvatarAsync(int userId, IFormFile file)
        {
            // Validate file
            if (file == null || file.Length == 0)
                return new ErrorDataResult<AvatarUploadResult>("No file provided");

            if (file.Length > MaxFileSize)
                return new ErrorDataResult<AvatarUploadResult>($"File size exceeds maximum limit of {MaxFileSize / (1024 * 1024)}MB");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
                return new ErrorDataResult<AvatarUploadResult>($"Invalid file type. Allowed types: {string.Join(", ", AllowedExtensions)}");

            // Get user
            UserEntity user = await _userRepository.GetAsync(u => u.UserId == userId);
            if (user == null)
                return new ErrorDataResult<AvatarUploadResult>("User not found");

            try
            {
                // Delete old avatar if exists
                if (!string.IsNullOrEmpty(user.AvatarUrl))
                {
                    await DeleteOldAvatarAsync(user.AvatarUrl, user.AvatarThumbnailUrl);
                }

                // Process and upload full size avatar
                using var avatarStream = new MemoryStream();
                using (var image = await Image.LoadAsync(file.OpenReadStream()))
                {
                    // Resize to max 512x512 maintaining aspect ratio
                    image.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Size = new Size(AVATAR_SIZE, AVATAR_SIZE),
                        Mode = ResizeMode.Max
                    }));

                    await image.SaveAsJpegAsync(avatarStream);
                }
                avatarStream.Position = 0;

                var avatarFileName = $"avatar_{userId}_{DateTime.Now.Ticks}.jpg";
                var avatarUrl = await _fileStorageService.UploadFileAsync(avatarStream, avatarFileName, "image/jpeg");

                if (string.IsNullOrEmpty(avatarUrl))
                    return new ErrorDataResult<AvatarUploadResult>("Failed to upload avatar");

                // Process and upload thumbnail
                using var thumbnailStream = new MemoryStream();
                using (var image = await Image.LoadAsync(file.OpenReadStream()))
                {
                    // Resize to 128x128 maintaining aspect ratio
                    image.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Size = new Size(THUMBNAIL_SIZE, THUMBNAIL_SIZE),
                        Mode = ResizeMode.Max
                    }));

                    await image.SaveAsJpegAsync(thumbnailStream);
                }
                thumbnailStream.Position = 0;

                var thumbnailFileName = $"avatar_thumb_{userId}_{DateTime.Now.Ticks}.jpg";
                var thumbnailUrl = await _fileStorageService.UploadFileAsync(thumbnailStream, thumbnailFileName, "image/jpeg");

                if (string.IsNullOrEmpty(thumbnailUrl))
                {
                    // Cleanup avatar if thumbnail upload failed
                    await _fileStorageService.DeleteFileAsync(avatarUrl);
                    return new ErrorDataResult<AvatarUploadResult>("Failed to upload thumbnail");
                }

                // Update user entity
                user.AvatarUrl = avatarUrl;
                user.AvatarThumbnailUrl = thumbnailUrl;
                user.AvatarUpdatedDate = DateTime.Now;

                _userRepository.Update(user);
                await _userRepository.SaveChangesAsync();

                return new SuccessDataResult<AvatarUploadResult>(new AvatarUploadResult
                {
                    AvatarUrl = user.AvatarUrl,
                    ThumbnailUrl = user.AvatarThumbnailUrl
                }, "Avatar uploaded successfully");
            }
            catch (Exception ex)
            {
                return new ErrorDataResult<AvatarUploadResult>($"Failed to upload avatar: {ex.Message}");
            }
        }

        public async Task<IDataResult<UserAvatarDto>> GetAvatarUrlAsync(int userId)
        {
            UserEntity user = await _userRepository.GetAsync(u => u.UserId == userId);
            if (user == null)
                return new ErrorDataResult<UserAvatarDto>("User not found");

            if (string.IsNullOrEmpty(user.AvatarUrl))
                return new ErrorDataResult<UserAvatarDto>("No avatar set for this user");

            var avatarDto = new UserAvatarDto
            {
                UserId = user.UserId,
                AvatarUrl = user.AvatarUrl,
                AvatarThumbnailUrl = user.AvatarThumbnailUrl,
                AvatarUpdatedDate = user.AvatarUpdatedDate
            };

            return new SuccessDataResult<UserAvatarDto>(avatarDto, "Avatar retrieved successfully");
        }

        public async Task<IResult> DeleteAvatarAsync(int userId)
        {
            UserEntity user = await _userRepository.GetAsync(u => u.UserId == userId);
            if (user == null)
                return new ErrorResult("User not found");

            if (string.IsNullOrEmpty(user.AvatarUrl))
                return new SuccessResult("No avatar to delete");

            try
            {
                await DeleteOldAvatarAsync(user.AvatarUrl, user.AvatarThumbnailUrl);

                // Update user entity
                user.AvatarUrl = null;
                user.AvatarThumbnailUrl = null;
                user.AvatarUpdatedDate = null;

                _userRepository.Update(user);
                await _userRepository.SaveChangesAsync();

                return new SuccessResult("Avatar deleted successfully");
            }
            catch (Exception ex)
            {
                return new ErrorResult($"Failed to delete avatar: {ex.Message}");
            }
        }

        private async Task DeleteOldAvatarAsync(string avatarUrl, string thumbnailUrl)
        {
            if (!string.IsNullOrEmpty(avatarUrl))
            {
                await _fileStorageService.DeleteFileAsync(avatarUrl);
            }

            if (!string.IsNullOrEmpty(thumbnailUrl))
            {
                await _fileStorageService.DeleteFileAsync(thumbnailUrl);
            }
        }
    }
}
