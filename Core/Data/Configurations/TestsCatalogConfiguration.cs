using Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Data.Configurations
{
    public class TestsCatalogConfiguration : IEntityTypeConfiguration<TestsCatalog>
    {
        public void Configure(EntityTypeBuilder<TestsCatalog> builder)
        {
            builder.ToTable("TestCatalogs");
            builder.HasKey(e => e.TestId);

            builder.Property(e => e.TestName)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(e => e.TestUnit)
                   .HasMaxLength(50);

            builder.Property(e => e.NormalRange)
                   .HasMaxLength(100);

            // Index for performance
            builder.HasIndex(e => e.TestName)
                   .IsUnique();
        }
    }
}