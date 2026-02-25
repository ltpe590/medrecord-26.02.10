using Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Data.Configurations;

public sealed class LabResultsConfiguration : IEntityTypeConfiguration<LabResults>
{
    public void Configure(EntityTypeBuilder<LabResults> builder)
    {
        builder.HasKey(r => r.LabId);

        builder.Property(r => r.TestId).IsRequired();
        builder.Property(r => r.VisitId).IsRequired();

        builder.Property(r => r.ResultValue).HasMaxLength(500);
        builder.Property(r => r.Unit).HasMaxLength(50);
        builder.Property(r => r.NormalRange).HasMaxLength(100);
        builder.Property(r => r.Notes).HasMaxLength(500);

        builder.Property(r => r.CreatedAt).IsRequired();

        // LabResults -> Visit (many-to-one)
        builder.HasOne(r => r.Visit)
               .WithMany()
               .HasForeignKey(r => r.VisitId)
               .OnDelete(DeleteBehavior.Cascade);

        // LabResults -> TestsCatalog (many-to-one)
        builder.HasOne(r => r.TestCatalog)
               .WithMany()
               .HasForeignKey(r => r.TestId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
