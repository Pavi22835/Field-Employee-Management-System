using FEMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FEMS.Infrastructure.Persistence.Configurations;

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> b)
    {
        b.ToTable("Employees");
        b.Property(x => x.EmployeeCode).HasMaxLength(50).IsRequired();
        b.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
        b.Property(x => x.LastName).HasMaxLength(100).IsRequired();
        b.HasIndex(x => x.EmployeeCode).IsUnique();

        b.HasOne(x => x.Supervisor).WithMany(x => x.DirectReports).HasForeignKey(x => x.SupervisorId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Devices).WithOne(x => x.AssignedEmployee).HasForeignKey(x => x.AssignedEmployeeId).OnDelete(DeleteBehavior.SetNull);
    }
}
