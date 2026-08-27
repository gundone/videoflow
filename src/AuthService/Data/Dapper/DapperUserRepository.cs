using AuthService.Abstractions;
using AuthService.Entities;
using Dapper;
using Npgsql;

namespace AuthService.Data.Dapper;

public class DapperUserRepository : IUserRepository
{
    private readonly string _connectionString;

    public DapperUserRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<User?> GetByIdAsync(Ulid id, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        // lang=sql
        var sql = """
                  SELECT id, 
                         email, 
                         password_hash AS PasswordHash, 
                         role, 
                         refresh_token AS RefreshToken, 
                         refresh_token_expires_at AS RefreshTokenExpiresAt 
                  FROM users 
                  WHERE id = @Id
                  """;
        return await conn.QueryFirstOrDefaultAsync<User>(
            new CommandDefinition(sql,
                new { Id = id.ToString() },
                cancellationToken: ct));
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        var sql= """
                 SELECT id, 
                     email, 
                     password_hash AS PasswordHash, 
                     role, 
                     refresh_token AS RefreshToken, 
                     refresh_token_expires_at AS RefreshTokenExpiresAt 
                 FROM users 
                 WHERE email = @Email
                 """;
        return await conn.QueryFirstOrDefaultAsync<User>(
            new CommandDefinition(
                sql,
                new { Email = email },
                cancellationToken: ct));
    }

    public async Task AddAsync(User user, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        var sql = """
                  INSERT INTO users (id, email, password_hash, role, refresh_token, refresh_token_expires_at) 
                  VALUES (@Id, @Email, @PasswordHash, @Role, @RefreshToken, @RefreshTokenExpiresAt)
                  """;
        await conn.ExecuteAsync(
            new CommandDefinition(
                sql,
                new
                {
                    Id = user.Id.ToString(),
                    user.Email,
                    user.PasswordHash,
                    user.Role,
                    RefreshToken = user.RefreshTokenHash,
                    user.RefreshTokenExpiresAt
                },
                cancellationToken: ct));
    }

    public async Task UpdateAsync(User user, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        var sql = """
                  UPDATE users 
                  SET email = @Email, 
                      password_hash = @PasswordHash, 
                      role = @Role, 
                      refresh_token = @RefreshToken, 
                      refresh_token_expires_at = @RefreshTokenExpiresAt 
                  WHERE id = @Id
                  """;
        await conn.ExecuteAsync(
            new CommandDefinition(
                sql,
                new
                {
                    Id = user.Id.ToString(),
                    user.Email,
                    user.PasswordHash,
                    user.Role,
                    RefreshToken = user.RefreshTokenHash,
                    user.RefreshTokenExpiresAt
                },
                cancellationToken: ct));
    }
}
