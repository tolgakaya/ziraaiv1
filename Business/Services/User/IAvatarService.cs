using Core.Utilities.Results;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using IResult = Core.Utilities.Results.IResult;

namespace Business.Services.User
{
    public interface IAvatarService
    {
        Task<IDataResult<AvatarUploadResult>> UploadAvatarAsync(int userId, IFormFile file);
        Task<IDataResult<string>> GetAvatarUrlAsync(int userId);
        Task<IResult> DeleteAvatarAsync(int userId);
    }

    public class AvatarUploadResult
    {
        public string AvatarUrl { get; set; }
        public string ThumbnailUrl { get; set; }
    }
}
