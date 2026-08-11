using FEMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FEMS.Infrastructure.Persistence.Configurations;

public class SystemSettingConfiguration : IEntityTypeConfiguration<SystemSetting>
{
    public void Configure(EntityTypeBuilder<SystemSetting> b)
    {
        b.ToTable("SystemSettings");
        b.Property(x => x.LocationTrackingMode).HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.MinimumSupportedAppVersion).HasMaxLength(20);
    }
}
