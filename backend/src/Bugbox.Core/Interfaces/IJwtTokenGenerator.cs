namespace Bugbox.Core.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateToken(Guid userId, string email, string name);
}
