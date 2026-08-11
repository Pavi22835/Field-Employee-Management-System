using FEMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FEMS.Infrastructure.Persistence.Configurations;

public class DeviceComplianceConfiguration : IEntityTypeConfiguration<DeviceCompliance>
{
    public void Configure(EntityTypeBuilder<DeviceCompliance> b) => b.ToTable("DeviceCompliances");
}
