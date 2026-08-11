using FEMS.Domain.Entities;
using FEMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FEMS.Infrastructure.Persistence.Configurations;

public class FormSubmissionConfiguration : IEntityTypeConfiguration<FormSubmission>
{
    public void Configure(EntityTypeBuilder<FormSubmission> b)
    {
        b.ToTable("FormSubmissions");
        b.Property(x => x.ReviewStatus).HasConversion<string>().HasMaxLength(20).HasDefaultValue(SubmissionReviewStatus.Pending);
        b.HasOne(x => x.DynamicForm).WithMany().HasForeignKey(x => x.DynamicFormId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Files).WithOne(x => x.FormSubmission).HasForeignKey(x => x.FormSubmissionId).OnDelete(DeleteBehavior.Cascade);
    }
}
