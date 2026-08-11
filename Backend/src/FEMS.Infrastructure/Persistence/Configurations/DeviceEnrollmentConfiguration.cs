using FEMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FEMS.Infrastructure.Persistence.Configurations;

public class DeviceEnrollmentConfiguration : IEntityTypeConfiguration<DeviceEnrollment>
{
    public void Configure(EntityTypeBuilder<DeviceEnrollment> b)
    {
        b.ToTable("DeviceEnrollments");
        b.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
    }
}
