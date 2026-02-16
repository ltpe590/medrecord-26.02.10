using Microsoft.AspNetCore.Identity;

namespace Core.Entities
{
    public class AppUser : IdentityUser
    {
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastLoginAt { get; set; }

        public int SpecialtyProfileId { get; set; } = 1;
        public SpecialtyProfile? SpecialtyProfile { get; set; }

        // For fingerprint authentication
        public bool HasFingerprintEnrolled { get; set; }

        public byte[] FingerprintTemplate { get; set; } = Array.Empty<byte>();
    }
}