namespace Nagaira.Ecommerce.Application.DTOs;

public record AddressDto(
    Guid Id,
    string Street,
    string City,
    string State,
    string PostalCode,
    string Country,
    bool IsDefault
);

public record CreateAddressDto(
    string Street,
    string City,
    string State,
    string PostalCode,
    string Country,
    bool IsDefault
);
