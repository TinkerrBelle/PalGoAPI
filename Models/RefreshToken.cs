// Models/RefreshToken.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PalGoAPI.Models
{
    [Table("RefreshTokens")]
    public class RefreshToken
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } // Changed from int to string for Identity

        [Required]
        [MaxLength(500)]
        public string Token { get; set; }

        [Required]
        public DateTime ExpiresAt { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? RevokedAt { get; set; }

        [MaxLength(255)]
        public string DeviceInfo { get; set; }

        [MaxLength(50)]
        public string IpAddress { get; set; }

        // Navigation property - Changed from User to ApplicationUser
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }

        // Helper properties
        [NotMapped]
        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

        [NotMapped]
        public bool IsActive => RevokedAt == null && !IsExpired;
    }
}