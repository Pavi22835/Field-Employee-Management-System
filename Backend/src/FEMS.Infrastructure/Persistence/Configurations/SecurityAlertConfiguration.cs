using FEMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FEMS.Infrastructure.Persistence.Configurations;

public class SecurityAlertConfiguration : IEntityTypeConfiguration<SecurityAlert>
{
    public void Configure(EntityTypeBuilder<SecurityAlert> b)
    {
        b.ToTable("SecurityAlerts");
        b.Property(x => x.Severity).HasConversion<string>().HasMaxLength(20);
        b.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.Device).WithMany().HasForeignKey(x => x.DeviceId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.FieldVisit).WithMany().HasForeignKey(x => x.FieldVisitId).OnDelete(DeleteBehavior.SetNull);
    }
}
