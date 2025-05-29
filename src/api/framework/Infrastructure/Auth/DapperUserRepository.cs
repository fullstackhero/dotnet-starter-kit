using System;
using System.Data;
using System.Threading.Tasks;
using Dapper;

namespace FSH.Framework.Infrastructure.Auth;

public class DapperUserRepository
{
    private readonly IDbConnection _db;

    public DapperUserRepository(IDbConnection db)
    {
        _db = db;
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _db.QueryFirstOrDefaultAsync<User>(
            @"SELECT 
                id,
                email,
                phone_number,
                tckn,
                password_hash,
                first_name,
                last_name,
                birth_date,
                is_identity_verified,
                is_phone_verified,
                is_email_verified,
                status,
                created_at,
                updated_at
            FROM users WHERE email = @Email", new { Email = email });
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await _db.QueryFirstOrDefaultAsync<User>(
            @"SELECT 
                id,
                email,
                phone_number,
                tckn,
                password_hash,
                first_name,
                last_name,
                birth_date,
                is_identity_verified,
                is_phone_verified,
                is_email_verified,
                status,
                created_at,
                updated_at
            FROM users WHERE id = @Id", new { Id = id });
    }

    public async Task<List<User>> GetAllUsersAsync()
    {
        var users = await _db.QueryAsync<User>(
            @"SELECT 
                id,
                email,
                phone_number,
                tckn,
                password_hash,
                first_name,
                last_name,
                birth_date,
                is_identity_verified,
                is_phone_verified,
                is_email_verified,
                status,
                created_at,
                updated_at
            FROM users ORDER BY created_at DESC");
        return users.ToList();
    }

    public async Task<bool> ValidatePasswordAsync(string email, string password)
    {
        var user = await GetByEmailAsync(email);
        if (user == null) return false;
        return BCrypt.Net.BCrypt.Verify(password, user.password_hash);
    }

    public async Task CreateAsync(User user)
    {
        user.password_hash = BCrypt.Net.BCrypt.HashPassword(user.password_hash);
        await _db.ExecuteAsync(
            @"INSERT INTO users (id, email, phone_number, tckn, password_hash, first_name, last_name, birth_date, is_identity_verified, is_phone_verified, is_email_verified, status, created_at, updated_at)
              VALUES (@id, @email, @phone_number, @tckn, @password_hash, @first_name, @last_name, @birth_date, @is_identity_verified, @is_phone_verified, @is_email_verified, @status, @created_at, @updated_at)",
            user);
    }

    public async Task UpdatePasswordAsync(Guid userId, string newPassword)
    {
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await _db.ExecuteAsync(
            @"UPDATE users SET password_hash = @PasswordHash, updated_at = @UpdatedAt WHERE id = @Id",
            new { PasswordHash = hashedPassword, UpdatedAt = DateTime.UtcNow, Id = userId });
    }

    public async Task UpdateAsync(User user)
    {
        await _db.ExecuteAsync(
            @"UPDATE users SET 
                email = @email,
                phone_number = @phone_number,
                first_name = @first_name,
                last_name = @last_name,
                updated_at = @updated_at
              WHERE id = @id",
            user);
    }

    // Diğer CRUD işlemleri eklenebilir...
} 