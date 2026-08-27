using AuthService.Entities;

namespace AuthService.Abstractions;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Ulid id, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
    Task UpdateAsync(User user, CancellationToken ct = default);
}