using Nagaira.Ecommerce.Application.DTOs;

namespace Nagaira.Ecommerce.Application.Interfaces;

public interface IPaymentMethodService
{
    Task<IEnumerable<PaymentMethodDto>> GetAllPaymentMethodsAsync();
    Task<IEnumerable<PaymentMethodDto>> GetActivePaymentMethodsAsync();
    Task<PaymentMethodDto?> GetPaymentMethodByIdAsync(Guid id);
    Task<PaymentMethodDto> CreatePaymentMethodAsync(CreatePaymentMethodDto dto);
    Task UpdatePaymentMethodAsync(UpdatePaymentMethodDto dto);
    Task DeletePaymentMethodAsync(Guid id);
    Task ActivatePaymentMethodAsync(Guid id);
    Task DeactivatePaymentMethodAsync(Guid id);
    Task<IEnumerable<PaymentMethodTypeSimpleDto>> GetPaymentMethodTypesAsync();
}

