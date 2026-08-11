using FEMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FEMS.Infrastructure.Persistence.Configurations;

public class FieldAreaConfiguration : IEntityTypeConfiguration<FieldArea>
{
    public void Configure(EntityTypeBuilder<FieldArea> b)
    {
        b.ToTable("FieldAreas");
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.Latitude).HasColumnType("decimal(9,6)");
        b.Property(x => x.Longitude).HasColumnType("decimal(9,6)");
        b.Property(x => x.EnforcementMode).HasConversion<string>().HasMaxLength(20);
    }
}
