using Core.Utilities.Results;
using Entities.Dtos;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using IResult = Core.Utilities.Results.IResult;

namespace Business.Services.Sponsor
{
    public interface ISponsorLogoService
    {
        Task<IDataResult<SponsorLogoUploadResult>> UploadLogoAsync(int sponsorId, IFormFile file);
        Task<IDataResult<SponsorLogoDto>> GetLogoUrlAsync(int sponsorId);
        Task<IResult> DeleteLogoAsync(int sponsorId);
    }

    public class SponsorLogoUploadResult
    {
        public string LogoUrl { get; set; }
        public string ThumbnailUrl { get; set; }
    }
}
