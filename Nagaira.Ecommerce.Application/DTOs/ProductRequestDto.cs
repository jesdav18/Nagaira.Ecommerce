namespace Nagaira.Ecommerce.Application.DTOs;

public record ProductRequestDto(
    Guid Id,
    string Name,
    string Phone,
    string? Email,
    string? City,
    string? Address,
    string Description,
    string Urgency,
    string? Link,
    string? ImageUrl,
    string? ImageName,
    string Status,
    DateTime CreatedAt
);
