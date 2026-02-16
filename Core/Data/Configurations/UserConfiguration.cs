using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Data.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<AppUser>
    {
        public void Configure(EntityTypeBuilder<AppUser> builder)
        {
            // Configure FingerprintTemplate as varbinary(MAX)
            builder.Property(u => u.FingerprintTemplate)
                .HasColumnType("varbinary(MAX)");

            // Set default values
            builder.Property(u => u.IsActive)
                .HasDefaultValue(true);

            builder.Property(u => u.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            builder.Property(u => u.HasFingerprintEnrolled)
                .HasDefaultValue(false);

            builder.Property(u => u.LastLoginAt)
                .IsRequired(false);
        }
    }
}