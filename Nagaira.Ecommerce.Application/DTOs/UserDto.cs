namespace Nagaira.Ecommerce.Application.DTOs;

public record UserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string PhoneNumber,
    string Role,
    Guid? PriceLevelId,
    string? PriceLevelName,
    bool IsActive
);

public record RegisterUserDto(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string PhoneNumber
);

public record LoginDto(
    string Email,
    string Password
);

public record AuthResponseDto(
    string Token,
    UserDto User
);

public record AuthResultDto(
    AuthResponseDto Response,
    string RefreshToken
);
