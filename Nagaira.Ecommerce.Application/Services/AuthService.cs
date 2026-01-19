using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Nagaira.Ecommerce.Application.DTOs;
using Nagaira.Ecommerce.Application.Interfaces;
using Nagaira.Ecommerce.Domain.Entities;
using Nagaira.Ecommerce.Domain.Interfaces;

namespace Nagaira.Ecommerce.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthService> _logger;
    private readonly string _jwtSecret;
    private readonly string _jwtIssuer;
    private readonly int _jwtExpirationDays;
    private readonly int _refreshTokenDays;

    public AuthService(IUnitOfWork unitOfWork, IEmailService emailService, ILogger<AuthService> logger, string jwtSecret, string jwtIssuer, int jwtExpirationDays, int refreshTokenDays)
    {
        _unitOfWork = unitOfWork;
        _emailService = emailService;
        _logger = logger;
        _jwtSecret = jwtSecret;
        _jwtIssuer = jwtIssuer;
        _jwtExpirationDays = jwtExpirationDays;
        _refreshTokenDays = refreshTokenDays;
    }

    public async Task<AuthResultDto> RegisterAsync(RegisterUserDto dto)
    {
        if (await _unitOfWork.Users.EmailExistsAsync(dto.Email))
            throw new Exception("Email already exists");
        ValidatePassword(dto.Password);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            PhoneNumber = dto.PhoneNumber,
            Role = UserRole.Customer,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        try
        {
            await _emailService.SendWelcomeAsync(user);
        }
        catch
        {
            // Ignore email failures to avoid blocking registration.
        }

        var token = GenerateJwtToken(user.Id, user.Email, user.Role.ToString());
        var refreshToken = await CreateRefreshTokenAsync(user);
        return new AuthResultDto(new AuthResponseDto(token, MapToDto(user)), refreshToken);
    }

    public async Task<AuthResultDto> LoginAsync(LoginDto dto)
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(dto.Email);
        if (user == null)
        {
            _logger.LogWarning("Login failed: user not found for {Email}", dto.Email);
            throw new Exception("Invalid credentials");
        }

        if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
        {
            _logger.LogWarning("Login blocked due to lockout for {Email}", dto.Email);
            throw new Exception("Account is temporarily locked");
        }

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            user.FailedLoginAttempts += 1;
            if (user.FailedLoginAttempts >= 5)
            {
                user.LockoutEnd = DateTime.UtcNow.AddMinutes(15);
                user.FailedLoginAttempts = 0;
            }
            await _unitOfWork.Users.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();
            _logger.LogWarning("Login failed: invalid password for {Email}", dto.Email);
            throw new Exception("Invalid credentials");
        }

        if (!user.IsActive)
            throw new Exception("Account is inactive");

        if (user.FailedLoginAttempts != 0 || user.LockoutEnd.HasValue)
        {
            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;
            await _unitOfWork.Users.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();
        }

        var token = GenerateJwtToken(user.Id, user.Email, user.Role.ToString());
        var refreshToken = await CreateRefreshTokenAsync(user);
        return new AuthResultDto(new AuthResponseDto(token, MapToDto(user)), refreshToken);
    }

    public async Task<AuthResultDto> RefreshAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new Exception("Refresh token missing");

        var tokenHash = HashToken(refreshToken);
        var stored = await _unitOfWork.RefreshTokens.GetByTokenHashAsync(tokenHash);
        if (stored == null || stored.RevokedAt.HasValue || stored.ExpiresAt <= DateTime.UtcNow)
        {
            _logger.LogWarning("Refresh token invalid or expired");
            throw new Exception("Invalid refresh token");
        }

        var user = stored.User;
        if (!user.IsActive)
            throw new Exception("Account is inactive");

        stored.RevokedAt = DateTime.UtcNow;
        await _unitOfWork.RefreshTokens.UpdateAsync(stored);
        await _unitOfWork.SaveChangesAsync();

        var newAccessToken = GenerateJwtToken(user.Id, user.Email, user.Role.ToString());
        var newRefreshToken = await CreateRefreshTokenAsync(user);
        return new AuthResultDto(new AuthResponseDto(newAccessToken, MapToDto(user)), newRefreshToken);
    }

    public async Task RevokeRefreshTokenAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return;

        var tokenHash = HashToken(refreshToken);
        var stored = await _unitOfWork.RefreshTokens.GetByTokenHashAsync(tokenHash);
        if (stored == null || stored.RevokedAt.HasValue) return;

        stored.RevokedAt = DateTime.UtcNow;
        await _unitOfWork.RefreshTokens.UpdateAsync(stored);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordDto dto)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null) throw new Exception("User not found");

        if (!BCrypt.Net.BCrypt.Verify(dto.OldPassword, user.PasswordHash))
            throw new Exception("La contraseña actual es incorrecta");

        ValidatePassword(dto.NewPassword);
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Users.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<UserDto?> GetUserByIdAsync(Guid id)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id);
        return user != null ? MapToDto(user) : null;
    }

    public string GenerateJwtToken(Guid userId, string email, string role)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtIssuer,
            audience: _jwtIssuer,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(_jwtExpirationDays),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static UserDto MapToDto(User user)
    {
        return new UserDto(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.PhoneNumber,
            user.Role.ToString(),
            user.PriceLevelId,
            user.PriceLevel?.Name,
            user.IsActive
        );
    }

    private async Task<string> CreateRefreshTokenAsync(User user)
    {
        var token = GenerateRefreshToken();
        var tokenHash = HashToken(token);
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenDays),
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.RefreshTokens.AddAsync(refreshToken);
        await _unitOfWork.SaveChangesAsync();
        return token;
    }

    private static string GenerateRefreshToken()
    {
        var bytes = new byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    private static string HashToken(string token)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(hash);
    }

    private static void ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 10)
            throw new Exception("La contraseAña debe tener al menos 10 caracteres");

        var hasUpper = password.Any(char.IsUpper);
        var hasLower = password.Any(char.IsLower);
        var hasDigit = password.Any(char.IsDigit);
        var hasSymbol = password.Any(ch => !char.IsLetterOrDigit(ch));

        if (!hasUpper || !hasLower || !hasDigit || !hasSymbol)
            throw new Exception("La contraseAña debe incluir mayAºsculas, minAºsculas, nAºmeros y sA-mbolos");
    }
}
