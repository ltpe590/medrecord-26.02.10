using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Data.Configurations;

public sealed class VisitConfiguration : IEntityTypeConfiguration<Visit>
{
    public void Configure(EntityTypeBuilder<Visit> builder)
    {
        builder.HasKey(v => v.VisitId);

        builder.Property(v => v.PatientId)
               .IsRequired();

        builder.Property(v => v.StartedAt)
               .IsRequired();

        builder.Property(v => v.EndedAt)
               .IsRequired(false);

        builder.Property(v => v.PausedAt)
               .IsRequired(false);

        builder.Property(v => v.PresentingSymptomText)
               .IsRequired()
               .HasMaxLength(500);

        builder.Property(v => v.PresentingSymptomDurationText)
               .IsRequired()
               .HasMaxLength(200);

        builder.Property(v => v.ShortNote)
               .IsRequired()
               .HasMaxLength(500);

        // 🔹 Owned value object (generic, cross-specialty)
        builder.OwnsOne(
            visit => visit.Vitals,
            vitals =>
            {
                vitals.Property(v => v.Temperature)
                      .HasPrecision(4, 1); // e.g. 36.5
                vitals.Property(v => v.Systolic);
                vitals.Property(v => v.Diastolic);
            })
            .Navigation(v => v.Vitals)
            .IsRequired(false);

        builder.OwnsMany(v => v.Entries, e =>
        {
            e.Property(x => x.SystemCode).HasMaxLength(20);
        });

        // 🔹 Visit → VisitEntry (one-to-many)
        builder.HasMany(v => v.Entries)
               .WithOne()
               .HasForeignKey(e => e.VisitId)
               .OnDelete(DeleteBehavior.Cascade);

    }
}
