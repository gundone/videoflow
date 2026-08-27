using AuthService.Entities;

namespace AuthService.Services;


public record AuthResult(bool Success, string? ErrorMessage, User? User, string? AccessToken, string? RefreshToken);
public interface IAuthService
{
    Task<AuthResult> RegisterAsync(string email, string password, CancellationToken ct = default);
    Task<AuthResult> LoginAsync(string email, string password, CancellationToken ct = default);
}