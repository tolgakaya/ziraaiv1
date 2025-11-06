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
using IResult = Core.Utilities.Results.IResult;

namespace Business.Services.Sponsor
{
    public class SponsorLogoService : ISponsorLogoService
    {
        private readonly ISponsorProfileRepository _sponsorProfileRepository;
        private readonly IFileStorageService _fileStorageService;
        private const int LOGO_SIZE = 512; // Full size logo
        private const int THUMBNAIL_SIZE = 128; // Thumbnail size
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg" };
        private const long MaxFileSize = 5 * 1024 * 1024; // 5MB

        public SponsorLogoService(
            ISponsorProfileRepository sponsorProfileRepository,
            IFileStorageService fileStorageService)
        {
            _sponsorProfileRepository = sponsorProfileRepository;
            _fileStorageService = fileStorageService;
        }

        public async Task<IDataResult<SponsorLogoUploadResult>> UploadLogoAsync(int sponsorId, IFormFile file)
        {
            // Validate file
            if (file == null || file.Length == 0)
                return new ErrorDataResult<SponsorLogoUploadResult>("No file provided");

            if (file.Length > MaxFileSize)
                return new ErrorDataResult<SponsorLogoUploadResult>($"File size exceeds maximum limit of {MaxFileSize / (1024 * 1024)}MB");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
                return new ErrorDataResult<SponsorLogoUploadResult>($"Invalid file type. Allowed types: {string.Join(", ", AllowedExtensions)}");

            // Get sponsor profile
            var sponsorProfile = await _sponsorProfileRepository.GetBySponsorIdAsync(sponsorId);
            if (sponsorProfile == null)
                return new ErrorDataResult<SponsorLogoUploadResult>("Sponsor profile not found");

            try
            {
                // Delete old logo if exists
                if (!string.IsNullOrEmpty(sponsorProfile.SponsorLogoUrl))
                {
                    await DeleteOldLogoAsync(sponsorProfile.SponsorLogoUrl, sponsorProfile.SponsorLogoThumbnailUrl);
                }

                // SVG files don't need resizing, upload directly
                if (extension == ".svg")
                {
                    using var logoStream = new MemoryStream();
                    await file.CopyToAsync(logoStream);
                    logoStream.Position = 0;

                    var svgFileName = $"sponsor_logo_{sponsorId}_{DateTime.Now.Ticks}.svg";
                    var svgUrl = await _fileStorageService.UploadFileAsync(logoStream, svgFileName, "image/svg+xml");

                    if (string.IsNullOrEmpty(svgUrl))
                        return new ErrorDataResult<SponsorLogoUploadResult>("Failed to upload logo");

                    // Update sponsor profile
                    sponsorProfile.SponsorLogoUrl = svgUrl;
                    sponsorProfile.SponsorLogoThumbnailUrl = svgUrl; // SVG is scalable, use same for thumbnail
                    sponsorProfile.UpdatedDate = DateTime.Now;
                    sponsorProfile.UpdatedByUserId = sponsorId;

                    _sponsorProfileRepository.Update(sponsorProfile);
                    await _sponsorProfileRepository.SaveChangesAsync();

                    return new SuccessDataResult<SponsorLogoUploadResult>(new SponsorLogoUploadResult
                    {
                        LogoUrl = svgUrl,
                        ThumbnailUrl = svgUrl
                    }, "Logo uploaded successfully");
                }

                // Process and upload full size logo (raster images)
                using var logoFullStream = new MemoryStream();
                using (var image = await Image.LoadAsync(file.OpenReadStream()))
                {
                    // Resize to max 512x512 maintaining aspect ratio
                    image.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Size = new Size(LOGO_SIZE, LOGO_SIZE),
                        Mode = ResizeMode.Max
                    }));

                    await image.SaveAsJpegAsync(logoFullStream);
                }
                logoFullStream.Position = 0;

                var logoFileName = $"sponsor_logo_{sponsorId}_{DateTime.Now.Ticks}.jpg";
                var logoUrl = await _fileStorageService.UploadFileAsync(logoFullStream, logoFileName, "image/jpeg");

                if (string.IsNullOrEmpty(logoUrl))
                    return new ErrorDataResult<SponsorLogoUploadResult>("Failed to upload logo");

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

                var thumbnailFileName = $"sponsor_logo_thumb_{sponsorId}_{DateTime.Now.Ticks}.jpg";
                var thumbnailUrl = await _fileStorageService.UploadFileAsync(thumbnailStream, thumbnailFileName, "image/jpeg");

                if (string.IsNullOrEmpty(thumbnailUrl))
                {
                    // Cleanup logo if thumbnail upload failed
                    await _fileStorageService.DeleteFileAsync(logoUrl);
                    return new ErrorDataResult<SponsorLogoUploadResult>("Failed to upload thumbnail");
                }

                // Update sponsor profile
                sponsorProfile.SponsorLogoUrl = logoUrl;
                sponsorProfile.SponsorLogoThumbnailUrl = thumbnailUrl;
                sponsorProfile.UpdatedDate = DateTime.Now;
                sponsorProfile.UpdatedByUserId = sponsorId;

                _sponsorProfileRepository.Update(sponsorProfile);
                await _sponsorProfileRepository.SaveChangesAsync();

                return new SuccessDataResult<SponsorLogoUploadResult>(new SponsorLogoUploadResult
                {
                    LogoUrl = logoUrl,
                    ThumbnailUrl = thumbnailUrl
                }, "Logo uploaded successfully");
            }
            catch (Exception ex)
            {
                return new ErrorDataResult<SponsorLogoUploadResult>($"Failed to upload logo: {ex.Message}");
            }
        }

        public async Task<IDataResult<SponsorLogoDto>> GetLogoUrlAsync(int sponsorId)
        {
            var sponsorProfile = await _sponsorProfileRepository.GetBySponsorIdAsync(sponsorId);
            if (sponsorProfile == null)
                return new ErrorDataResult<SponsorLogoDto>("Sponsor profile not found");

            if (string.IsNullOrEmpty(sponsorProfile.SponsorLogoUrl))
                return new ErrorDataResult<SponsorLogoDto>("No logo set for this sponsor");

            var logoDto = new SponsorLogoDto
            {
                SponsorId = sponsorProfile.SponsorId,
                LogoUrl = sponsorProfile.SponsorLogoUrl,
                ThumbnailUrl = sponsorProfile.SponsorLogoThumbnailUrl,
                UpdatedDate = sponsorProfile.UpdatedDate
            };

            return new SuccessDataResult<SponsorLogoDto>(logoDto, "Logo retrieved successfully");
        }

        public async Task<IResult> DeleteLogoAsync(int sponsorId)
        {
            var sponsorProfile = await _sponsorProfileRepository.GetBySponsorIdAsync(sponsorId);
            if (sponsorProfile == null)
                return new ErrorResult("Sponsor profile not found");

            if (string.IsNullOrEmpty(sponsorProfile.SponsorLogoUrl))
                return new SuccessResult("No logo to delete");

            try
            {
                await DeleteOldLogoAsync(sponsorProfile.SponsorLogoUrl, sponsorProfile.SponsorLogoThumbnailUrl);

                // Update sponsor profile
                sponsorProfile.SponsorLogoUrl = null;
                sponsorProfile.SponsorLogoThumbnailUrl = null;
                sponsorProfile.UpdatedDate = DateTime.Now;
                sponsorProfile.UpdatedByUserId = sponsorId;

                _sponsorProfileRepository.Update(sponsorProfile);
                await _sponsorProfileRepository.SaveChangesAsync();

                return new SuccessResult("Logo deleted successfully");
            }
            catch (Exception ex)
            {
                return new ErrorResult($"Failed to delete logo: {ex.Message}");
            }
        }

        private async Task DeleteOldLogoAsync(string logoUrl, string thumbnailUrl)
        {
            if (!string.IsNullOrEmpty(logoUrl))
            {
                await _fileStorageService.DeleteFileAsync(logoUrl);
            }

            if (!string.IsNullOrEmpty(thumbnailUrl))
            {
                await _fileStorageService.DeleteFileAsync(thumbnailUrl);
            }
        }
    }
}
