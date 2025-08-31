using System.Collections.Generic;
using System.Security.Claims;

namespace Tests.Helpers
{
    public static class ClaimsData
    {
        public static List<Claim> GetClaims()
        {
            return new ()
            {
                new Claim("username", "deneme"),
                new Claim("email", "test@test.com"),
                new Claim("nameidentifier", "1"),
                new Claim(ClaimTypes.Role, "Farmer")
            };
        }

        public static List<Claim> GetFarmerClaims()
        {
            return new ()
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Name, "TestFarmer"),
                new Claim(ClaimTypes.Email, "farmer@test.com"),
                new Claim(ClaimTypes.Role, "Farmer"),
                new Claim("UserId", "1"),
                new Claim("FullName", "Test Farmer")
            };
        }

        public static List<Claim> GetAdminClaims()
        {
            return new ()
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Name, "TestAdmin"),
                new Claim(ClaimTypes.Email, "admin@test.com"),
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim("UserId", "1"),
                new Claim("FullName", "Test Admin")
            };
        }

        public static List<Claim> GetSponsorClaims()
        {
            return new ()
            {
                new Claim(ClaimTypes.NameIdentifier, "2"),
                new Claim(ClaimTypes.Name, "TestSponsor"),
                new Claim(ClaimTypes.Email, "sponsor@test.com"),
                new Claim(ClaimTypes.Role, "Sponsor"),
                new Claim("UserId", "2"),
                new Claim("FullName", "Test Sponsor")
            };
        }

        public static List<Claim> GetUnauthorizedClaims()
        {
            return new ()
            {
                new Claim(ClaimTypes.NameIdentifier, "999"),
                new Claim(ClaimTypes.Name, "UnauthorizedUser"),
                new Claim(ClaimTypes.Email, "unauthorized@test.com"),
                new Claim(ClaimTypes.Role, "Guest")
            };
        }
    }
}