using FEMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FEMS.Infrastructure.Persistence.Configurations;

public class FormSubmissionFileConfiguration : IEntityTypeConfiguration<FormSubmissionFile>
{
    public void Configure(EntityTypeBuilder<FormSubmissionFile> b)
    {
        b.ToTable("FormSubmissionFiles");
        b.Property(x => x.FileHash).HasMaxLength(128).IsRequired();
        b.Property(x => x.CapturedLatitude).HasColumnType("decimal(9,6)");
        b.Property(x => x.CapturedLongitude).HasColumnType("decimal(9,6)");
        b.HasOne(x => x.FormField).WithMany().HasForeignKey(x => x.FormFieldId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.Device).WithMany().HasForeignKey(x => x.DeviceId).OnDelete(DeleteBehavior.SetNull);
    }
}
