using FEMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FEMS.Infrastructure.Persistence.Configurations;

public class DynamicFormConfiguration : IEntityTypeConfiguration<DynamicForm>
{
    public void Configure(EntityTypeBuilder<DynamicForm> b)
    {
        b.ToTable("DynamicForms");
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.HasMany(x => x.Fields).WithOne(x => x.DynamicForm).HasForeignKey(x => x.DynamicFormId).OnDelete(DeleteBehavior.Cascade);
    }
}
