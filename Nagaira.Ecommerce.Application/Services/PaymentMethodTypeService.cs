using Nagaira.Ecommerce.Application.DTOs;
using Nagaira.Ecommerce.Application.Interfaces;
using Nagaira.Ecommerce.Domain.Entities;
using Nagaira.Ecommerce.Domain.Interfaces;

namespace Nagaira.Ecommerce.Application.Services;

public class PaymentMethodTypeService : IPaymentMethodTypeService
{
    private readonly IUnitOfWork _unitOfWork;

    public PaymentMethodTypeService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<PaymentMethodTypeDto>> GetAllPaymentMethodTypesAsync()
    {
        var types = await _unitOfWork.PaymentMethodTypes.GetAllAsync();
        return types.Select(MapToDto);
    }

    public async Task<IEnumerable<PaymentMethodTypeDto>> GetActivePaymentMethodTypesAsync()
    {
        var types = await _unitOfWork.PaymentMethodTypes.GetActivePaymentMethodTypesAsync();
        return types.Select(MapToDto);
    }

    public async Task<PaymentMethodTypeDto?> GetPaymentMethodTypeByIdAsync(Guid id)
    {
        var type = await _unitOfWork.PaymentMethodTypes.GetByIdAsync(id);
        if (type == null || type.IsDeleted) return null;
        return MapToDto(type);
    }

    public async Task<PaymentMethodTypeDto> CreatePaymentMethodTypeAsync(CreatePaymentMethodTypeDto dto)
    {
        var paymentMethodType = new PaymentMethodType
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Label = dto.Label,
            Description = dto.Description,
            DisplayOrder = dto.DisplayOrder,
            IsActive = dto.IsActive,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.PaymentMethodTypes.AddAsync(paymentMethodType);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(paymentMethodType);
    }

    public async Task UpdatePaymentMethodTypeAsync(UpdatePaymentMethodTypeDto dto)
    {
        var paymentMethodType = await _unitOfWork.PaymentMethodTypes.GetByIdAsync(dto.Id);
        if (paymentMethodType == null || paymentMethodType.IsDeleted)
            throw new Exception("Tipo de medio de pago no encontrado");

        paymentMethodType.Name = dto.Name;
        paymentMethodType.Label = dto.Label;
        paymentMethodType.Description = dto.Description;
        paymentMethodType.DisplayOrder = dto.DisplayOrder;
        paymentMethodType.IsActive = dto.IsActive;
        paymentMethodType.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.PaymentMethodTypes.UpdateAsync(paymentMethodType);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DeletePaymentMethodTypeAsync(Guid id)
    {
        await _unitOfWork.PaymentMethodTypes.DeleteAsync(id);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task ActivatePaymentMethodTypeAsync(Guid id)
    {
        var paymentMethodType = await _unitOfWork.PaymentMethodTypes.GetByIdAsync(id);
        if (paymentMethodType == null || paymentMethodType.IsDeleted)
            throw new Exception("Tipo de medio de pago no encontrado");

        paymentMethodType.IsActive = true;
        paymentMethodType.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.PaymentMethodTypes.UpdateAsync(paymentMethodType);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DeactivatePaymentMethodTypeAsync(Guid id)
    {
        var paymentMethodType = await _unitOfWork.PaymentMethodTypes.GetByIdAsync(id);
        if (paymentMethodType == null || paymentMethodType.IsDeleted)
            throw new Exception("Tipo de medio de pago no encontrado");

        paymentMethodType.IsActive = false;
        paymentMethodType.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.PaymentMethodTypes.UpdateAsync(paymentMethodType);
        await _unitOfWork.SaveChangesAsync();
    }

    private static PaymentMethodTypeDto MapToDto(PaymentMethodType type)
    {
        return new PaymentMethodTypeDto(
            type.Id,
            type.Name,
            type.Label,
            type.Description,
            type.DisplayOrder,
            type.IsActive,
            type.CreatedAt
        );
    }
}

