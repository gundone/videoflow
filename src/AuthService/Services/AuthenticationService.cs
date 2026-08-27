using AuthService.Abstractions;
using AuthService.Entities;
using Crypt = BCrypt.Net.BCrypt;

namespace AuthService.Services;

public class AuthenticationService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtService _jwtService;
    private const int RefreshTokenTtlDays = 7;

    public AuthenticationService(IUserRepository userRepository, IJwtService jwtService)
    {
        _userRepository = userRepository;
        _jwtService = jwtService;
    }

    public async Task<AuthResult> RegisterAsync(string email, string password, CancellationToken ct = default)
    {
        var existing = await _userRepository.GetByEmailAsync(email, ct);
        if (existing is not null)
            return Fail("Email already registered");

       
        var user = new User
        {
            Id = Ulid.NewUlid(),
            Email = email,
            PasswordHash = Crypt.HashPassword(password),
            Role = "user",
        };

        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();
        user.RefreshTokenHash = Crypt.HashPassword(refreshToken);
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenTtlDays);

        await _userRepository.AddAsync(user, ct);
        return Ok(user, accessToken, refreshToken);
    }

    public async Task<AuthResult> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByEmailAsync(email, ct);
        if (user is null)
            return Fail("Invalid email or password");

        if (!Crypt.Verify(password, user.PasswordHash))
            return Fail("Invalid email or password");

        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();

        user.RefreshTokenHash = Crypt.HashPassword(refreshToken);
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenTtlDays);
        await _userRepository.UpdateAsync(user, ct);

        return Ok(user, accessToken, refreshToken);
    }

    private static AuthResult Fail(string message)
        => new(false, message, null, null, null);

    private static AuthResult Ok(User user, string accessToken, string refreshToken)
        => new(true, null, user, accessToken, refreshToken);
}