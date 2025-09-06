using System;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace Core.Entities.Concrete
{
    public class User : IEntity
    {
        public User()
        {
            // Initialize with safe defaults
            var now = DateTime.Now;
            
            if(UserId == 0){
              RecordDate = now;
              UpdateContactDate = now;
            }
            
            Status = true;
            
            // Fix infinity values that might be read from database
            // This is only for fixing existing data, not for new records
            if (UserId > 0) // Only apply fixes for existing records
            {
                if (BirthDate.HasValue && (BirthDate.Value == DateTime.MaxValue || BirthDate.Value == DateTime.MinValue))
                {
                    BirthDate = null;
                }
                if (UpdateContactDate == DateTime.MaxValue || UpdateContactDate == DateTime.MinValue)
                {
                    UpdateContactDate = now;
                }
                if (RecordDate == DateTime.MaxValue || RecordDate == DateTime.MinValue)
                {
                    RecordDate = now;
                }
            }
        }

        public int UserId { get; set; }
        public long CitizenId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        [JsonIgnore]
        public string RefreshToken { get; set; }
        public string MobilePhones { get; set; }
        public bool Status { get; set; }
        public DateTime? BirthDate { get; set; }  // Made nullable
        public int? Gender { get; set; }  // Made nullable
        public DateTime RecordDate { get; set; }
        public string Address { get; set; }
        public string Notes { get; set; }
        public DateTime UpdateContactDate { get; set; }

        /// <summary>
        /// This is required when encoding token. Not in db. The default is Person.
        /// </summary>
        [NotMapped]
        public string AuthenticationProviderType { get; set; } = "Person";

        public byte[] PasswordSalt { get; set; }
        public byte[] PasswordHash { get; set; }

        public bool UpdateMobilePhone(string mobilePhone)
        {
            if (mobilePhone == MobilePhones)
            {
                return false;
            }

            MobilePhones = mobilePhone;
            return true;
        }
    }
}
