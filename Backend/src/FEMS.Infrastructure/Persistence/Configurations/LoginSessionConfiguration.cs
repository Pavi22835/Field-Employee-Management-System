using FEMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FEMS.Infrastructure.Persistence.Configurations;

public class LoginSessionConfiguration : IEntityTypeConfiguration<LoginSession>
{
    public void Configure(EntityTypeBuilder<LoginSession> b)
    {
        b.ToTable("LoginSessions");
        b.HasOne(x => x.Device).WithMany(x => x.LoginSessions).HasForeignKey(x => x.DeviceId).OnDelete(DeleteBehavior.SetNull);
    }
}
