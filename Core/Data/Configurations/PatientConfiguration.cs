using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Data.Configurations
{
    public class PatientConfiguration : IEntityTypeConfiguration<Patient>
    {
        public void Configure(EntityTypeBuilder<Patient> builder)
        {
            builder.ToTable("Patients");
            builder.HasKey(e => e.PatientId);

            builder.OwnsOne(p => p.PhoneNumber, pb =>
            {
                pb.Property(p => p.Value)
                  .HasColumnName("PhoneNumber")
                  .HasMaxLength(20);
            });

            builder.Property(p => p.DateOfBirth)
                   .HasConversion(
                       d => d.ToDateTime(TimeOnly.MinValue),  // Convert to DateTime for DB
                       d => DateOnly.FromDateTime(d))         // Convert back to DateOnly
                   .IsRequired();

            // Unique constraint: prevent duplicate patients with same name + date of birth.
            // IsDeleted is included so soft-deleted patients don't block re-registration.
            builder.HasIndex(p => new { p.Name, p.DateOfBirth, p.IsDeleted })
                   .HasDatabaseName("IX_Patients_Name_DateOfBirth_IsDeleted")
                   .IsUnique();
        }
    }
}