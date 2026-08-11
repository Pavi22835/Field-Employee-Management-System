using FEMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FEMS.Infrastructure.Persistence.Configurations;

public class DeviceConfiguration : IEntityTypeConfiguration<Device>
{
    public void Configure(EntityTypeBuilder<Device> b)
    {
        b.ToTable("Devices");
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.HasIndex(x => x.AppInstallationId).IsUnique();
        b.HasIndex(x => x.AndroidId);

        b.HasMany(x => x.Enrollments).WithOne(x => x.Device).HasForeignKey(x => x.DeviceId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.ComplianceSnapshots).WithOne(x => x.Device).HasForeignKey(x => x.DeviceId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Events).WithOne(x => x.Device).HasForeignKey(x => x.DeviceId).OnDelete(DeleteBehavior.Cascade);
    }
}
