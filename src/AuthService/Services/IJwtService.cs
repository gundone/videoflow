using AuthService.Entities;


namespace AuthService.Services;

public interface IJwtService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
}