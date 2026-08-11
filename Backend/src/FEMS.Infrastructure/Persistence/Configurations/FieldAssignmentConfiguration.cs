using FEMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FEMS.Infrastructure.Persistence.Configurations;

public class FieldAssignmentConfiguration : IEntityTypeConfiguration<FieldAssignment>
{
    public void Configure(EntityTypeBuilder<FieldAssignment> b)
    {
        b.ToTable("FieldAssignments");
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.HasOne(x => x.Employee).WithMany(x => x.FieldAssignments).HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.FieldArea).WithMany(x => x.Assignments).HasForeignKey(x => x.FieldAreaId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.DynamicForm).WithMany(x => x.FieldAssignments).HasForeignKey(x => x.DynamicFormId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.FieldVisit).WithOne(x => x.FieldAssignment).HasForeignKey<FieldVisit>(x => x.FieldAssignmentId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.EmployeeId, x.VisitDate });
    }
}
