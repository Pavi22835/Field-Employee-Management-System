using FEMS.Domain.Entities;

namespace FEMS.Application.Common.Interfaces;

public interface IJwtTokenService
{
    string GenerateAccessToken(User user, IEnumerable<string> roles, Guid? employeeId, Guid? deviceId);
    (string Token, DateTimeOffset ExpiresAt) GenerateRefreshToken();
    string HashToken(string token);
}
