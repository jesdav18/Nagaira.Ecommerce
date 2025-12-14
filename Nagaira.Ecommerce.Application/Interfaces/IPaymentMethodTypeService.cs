using Nagaira.Ecommerce.Application.DTOs;

namespace Nagaira.Ecommerce.Application.Interfaces;

public interface IPaymentMethodTypeService
{
    Task<IEnumerable<PaymentMethodTypeDto>> GetAllPaymentMethodTypesAsync();
    Task<IEnumerable<PaymentMethodTypeDto>> GetActivePaymentMethodTypesAsync();
    Task<PaymentMethodTypeDto?> GetPaymentMethodTypeByIdAsync(Guid id);
    Task<PaymentMethodTypeDto> CreatePaymentMethodTypeAsync(CreatePaymentMethodTypeDto dto);
    Task UpdatePaymentMethodTypeAsync(UpdatePaymentMethodTypeDto dto);
    Task DeletePaymentMethodTypeAsync(Guid id);
    Task ActivatePaymentMethodTypeAsync(Guid id);
    Task DeactivatePaymentMethodTypeAsync(Guid id);
}

