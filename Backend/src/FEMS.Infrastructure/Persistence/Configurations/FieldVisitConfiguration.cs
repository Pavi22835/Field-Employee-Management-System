using FEMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FEMS.Infrastructure.Persistence.Configurations;

public class FieldVisitConfiguration : IEntityTypeConfiguration<FieldVisit>
{
    public void Configure(EntityTypeBuilder<FieldVisit> b)
    {
        b.ToTable("FieldVisits");
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.CheckInLatitude).HasColumnType("decimal(9,6)");
        b.Property(x => x.CheckInLongitude).HasColumnType("decimal(9,6)");
        b.Property(x => x.CheckOutLatitude).HasColumnType("decimal(9,6)");
        b.Property(x => x.CheckOutLongitude).HasColumnType("decimal(9,6)");

        b.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Device).WithMany().HasForeignKey(x => x.DeviceId).OnDelete(DeleteBehavior.SetNull);
        b.HasMany(x => x.LocationPoints).WithOne(x => x.FieldVisit).HasForeignKey(x => x.FieldVisitId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.FormSubmissions).WithOne(x => x.FieldVisit).HasForeignKey(x => x.FieldVisitId).OnDelete(DeleteBehavior.Cascade);
    }
}
