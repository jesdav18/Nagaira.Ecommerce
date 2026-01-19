using Nagaira.Ecommerce.Application.DTOs;

namespace Nagaira.Ecommerce.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResultDto> RegisterAsync(RegisterUserDto dto);
    Task<AuthResultDto> LoginAsync(LoginDto dto);
    Task<AuthResultDto> RefreshAsync(string refreshToken);
    Task RevokeRefreshTokenAsync(string refreshToken);
    Task ChangePasswordAsync(Guid userId, ChangePasswordDto dto);
    Task<UserDto?> GetUserByIdAsync(Guid id);
    string GenerateJwtToken(Guid userId, string email, string role);
}
