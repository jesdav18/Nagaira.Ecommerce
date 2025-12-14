namespace Nagaira.Ecommerce.Application.DTOs;

public record PagedResultDto<T>(
    IEnumerable<T> Items,
    int TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages
);

