using System;

namespace Entities.Dtos
{
    public class UserAvatarDto
    {
        public int UserId { get; set; }
        public string AvatarUrl { get; set; }
        public string AvatarThumbnailUrl { get; set; }
        public DateTime? AvatarUpdatedDate { get; set; }
    }
}
