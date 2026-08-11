using FEMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FEMS.Infrastructure.Persistence.Configurations;

public class FormFieldConfiguration : IEntityTypeConfiguration<FormField>
{
    public void Configure(EntityTypeBuilder<FormField> b)
    {
        b.ToTable("FormFields");
        b.Property(x => x.FieldType).HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.Label).HasMaxLength(200).IsRequired();
    }
}
