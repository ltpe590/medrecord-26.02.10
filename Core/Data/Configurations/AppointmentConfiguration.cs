using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Data.Configurations
{
    public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
    {
        public void Configure(EntityTypeBuilder<Appointment> builder)
        {
            builder.HasKey(a => a.AppointmentId);

            builder.Property(a => a.Reason).HasMaxLength(200);
            builder.Property(a => a.Notes).HasMaxLength(500);
            builder.Property(a => a.Status).HasConversion<int>();

            builder.HasOne(a => a.Patient)
                   .WithMany()
                   .HasForeignKey(a => a.PatientId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(a => a.ScheduledAt);
            builder.HasIndex(a => a.PatientId);
        }
    }
}
