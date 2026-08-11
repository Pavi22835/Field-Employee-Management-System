using FEMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FEMS.Infrastructure.Persistence.Configurations;

public class FieldVisitLocationConfiguration : IEntityTypeConfiguration<FieldVisitLocation>
{
    public void Configure(EntityTypeBuilder<FieldVisitLocation> b)
    {
        b.ToTable("FieldVisitLocations");
        b.Property(x => x.Latitude).HasColumnType("decimal(9,6)");
        b.Property(x => x.Longitude).HasColumnType("decimal(9,6)");
        b.HasIndex(x => new { x.FieldVisitId, x.CapturedAt });
    }
}
