using Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Data.Configurations
{
    public class DrugCatalogConfiguration : IEntityTypeConfiguration<DrugCatalog>
    {
        public void Configure(EntityTypeBuilder<DrugCatalog> builder)
        {
            builder.ToTable("DrugCatalogs");
            builder.HasKey(e => e.DrugId);

            builder.Property(e => e.BrandName)
                   .IsRequired()
                   .HasMaxLength(200);

            builder.Property(e => e.Composition)
                   .HasMaxLength(500);

            builder.Property(e => e.Form)
                   .HasMaxLength(50);

            builder.Property(e => e.DosageStrength)
                   .HasMaxLength(100);

            builder.Property(e => e.Frequency)
                   .HasMaxLength(100);

            builder.HasIndex(e => e.BrandName)
                   .IsUnique();
        }
    }
}