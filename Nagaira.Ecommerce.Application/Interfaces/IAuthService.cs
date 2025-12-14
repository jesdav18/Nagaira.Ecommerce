using Nagaira.Ecommerce.Application.DTOs;

namespace Nagaira.Ecommerce.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterUserDto dto);
    Task<AuthResponseDto> LoginAsync(LoginDto dto);
    Task ChangePasswordAsync(Guid userId, ChangePasswordDto dto);
    Task<UserDto?> GetUserByIdAsync(Guid id);
    string GenerateJwtToken(Guid userId, string email, string role);
}
