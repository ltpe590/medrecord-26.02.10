using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Data.Configurations
{
    public class SpecialtyProfileConfiguration : IEntityTypeConfiguration<SpecialtyProfile>
    {
        public void Configure(EntityTypeBuilder<SpecialtyProfile> builder)
        {
            builder.HasKey(x => x.SpecialtyProfileId);

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.HasIndex(x => x.Name)
                .IsUnique();
        }
    }
}
