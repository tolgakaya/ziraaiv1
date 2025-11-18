using System;

namespace Entities.Dtos
{
    /// <summary>
    /// DTO for updating app info (Admin only)
    /// </summary>
    public class UpdateAppInfoDto
    {
        // Company Info
        public string CompanyName { get; set; }
        public string CompanyDescription { get; set; }
        public string AppVersion { get; set; }

        // Address
        public string Address { get; set; }

        // Contact Info
        public string Email { get; set; }
        public string Phone { get; set; }
        public string WebsiteUrl { get; set; }

        // Social Media Links
        public string FacebookUrl { get; set; }
        public string InstagramUrl { get; set; }
        public string YouTubeUrl { get; set; }
        public string TwitterUrl { get; set; }
        public string LinkedInUrl { get; set; }

        // Legal Pages URLs
        public string TermsOfServiceUrl { get; set; }
        public string PrivacyPolicyUrl { get; set; }
        public string CookiePolicyUrl { get; set; }
    }

    /// <summary>
    /// DTO for viewing app info (Farmer/Sponsor)
    /// </summary>
    public class AppInfoDto
    {
        // Company Info
        public string CompanyName { get; set; }
        public string CompanyDescription { get; set; }
        public string AppVersion { get; set; }

        // Address
        public string Address { get; set; }

        // Contact Info
        public string Email { get; set; }
        public string Phone { get; set; }
        public string WebsiteUrl { get; set; }

        // Social Media Links
        public string FacebookUrl { get; set; }
        public string InstagramUrl { get; set; }
        public string YouTubeUrl { get; set; }
        public string TwitterUrl { get; set; }
        public string LinkedInUrl { get; set; }

        // Legal Pages URLs
        public string TermsOfServiceUrl { get; set; }
        public string PrivacyPolicyUrl { get; set; }
        public string CookiePolicyUrl { get; set; }

        // Metadata
        public DateTime UpdatedDate { get; set; }
    }

    /// <summary>
    /// DTO for admin view with additional metadata
    /// </summary>
    public class AdminAppInfoDto
    {
        public int Id { get; set; }

        // Company Info
        public string CompanyName { get; set; }
        public string CompanyDescription { get; set; }
        public string AppVersion { get; set; }

        // Address
        public string Address { get; set; }

        // Contact Info
        public string Email { get; set; }
        public string Phone { get; set; }
        public string WebsiteUrl { get; set; }

        // Social Media Links
        public string FacebookUrl { get; set; }
        public string InstagramUrl { get; set; }
        public string YouTubeUrl { get; set; }
        public string TwitterUrl { get; set; }
        public string LinkedInUrl { get; set; }

        // Legal Pages URLs
        public string TermsOfServiceUrl { get; set; }
        public string PrivacyPolicyUrl { get; set; }
        public string CookiePolicyUrl { get; set; }

        // Metadata
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        public int? UpdatedByUserId { get; set; }
        public string UpdatedByUserName { get; set; }
    }
}
