using FEMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FEMS.Infrastructure.Persistence.Configurations;

public class AppVersionConfiguration : IEntityTypeConfiguration<AppVersion>
{
    public void Configure(EntityTypeBuilder<AppVersion> b)
    {
        b.ToTable("AppVersions");
        b.HasIndex(x => x.Platform).IsUnique();
    }
}
