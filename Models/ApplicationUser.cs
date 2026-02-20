using Microsoft.AspNetCore.Identity;

namespace PalGoAPI.Models
{
    public enum UserRole
    {
        Customer = 0,
        Runner = 1,
        Admin = 2
    }

    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        // ADD THESE:
        public string? EmailVerificationCode { get; set; }
        public DateTime? EmailVerificationExpiry { get; set; }
        public int IsEmailVerified { get; set; } = 0;  // 0 = false, 1 = true in Oracle
    }
}