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

    // Diğer CRUD işlemleri eklenebilir...
} 