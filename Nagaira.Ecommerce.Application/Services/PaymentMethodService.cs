using Nagaira.Ecommerce.Application.DTOs;
using Nagaira.Ecommerce.Application.Interfaces;
using Nagaira.Ecommerce.Domain.Entities;
using Nagaira.Ecommerce.Domain.Interfaces;

namespace Nagaira.Ecommerce.Application.Services;

public class PaymentMethodService : IPaymentMethodService
{
    private readonly IUnitOfWork _unitOfWork;

    public PaymentMethodService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<PaymentMethodDto>> GetAllPaymentMethodsAsync()
    {
        var paymentMethods = await _unitOfWork.PaymentMethods.GetAllAsync();
        var typeLabels = await GetTypeLabelMapAsync();
        return paymentMethods.Select(pm => MapToDto(pm, typeLabels));
    }

    public async Task<IEnumerable<PaymentMethodDto>> GetActivePaymentMethodsAsync()
    {
        var paymentMethods = await _unitOfWork.PaymentMethods.GetActivePaymentMethodsAsync();
        var typeLabels = await GetTypeLabelMapAsync();
        return paymentMethods.Select(pm => MapToDto(pm, typeLabels));
    }

    public async Task<PaymentMethodDto?> GetPaymentMethodByIdAsync(Guid id)
    {
        var paymentMethod = await _unitOfWork.PaymentMethods.GetByIdAsync(id);
        if (paymentMethod == null || paymentMethod.IsDeleted) return null;
        var typeLabels = await GetTypeLabelMapAsync();
        return MapToDto(paymentMethod, typeLabels);
    }

    public async Task<PaymentMethodDto> CreatePaymentMethodAsync(CreatePaymentMethodDto dto)
    {
        // Validar que el tipo existe en la base de datos
        var typeExists = await _unitOfWork.PaymentMethodTypes.GetAllAsync();
        if (!typeExists.Any(t => t.Name == dto.Type && !t.IsDeleted && t.IsActive))
            throw new Exception("Tipo de medio de pago inválido o inactivo");

        var paymentMethod = new PaymentMethod
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Description = dto.Description ?? string.Empty,
            Type = dto.Type,
            AccountNumber = dto.AccountNumber,
            BankName = dto.BankName,
            AccountHolderName = dto.AccountHolderName,
            WalletProvider = dto.WalletProvider,
            WalletNumber = dto.WalletNumber,
            QrCodeUrl = dto.QrCodeUrl,
            Instructions = dto.Instructions,
            DisplayOrder = dto.DisplayOrder,
            IsActive = dto.IsActive,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.PaymentMethods.AddAsync(paymentMethod);
        await _unitOfWork.SaveChangesAsync();

        var typeLabels = await GetTypeLabelMapAsync();
        return MapToDto(paymentMethod, typeLabels);
    }

    public async Task UpdatePaymentMethodAsync(UpdatePaymentMethodDto dto)
    {
        var paymentMethod = await _unitOfWork.PaymentMethods.GetByIdAsync(dto.Id);
        if (paymentMethod == null || paymentMethod.IsDeleted)
            throw new Exception("Medio de pago no encontrado");

        // Validar que el tipo existe en la base de datos
        var typeExists = await _unitOfWork.PaymentMethodTypes.GetAllAsync();
        if (!typeExists.Any(t => t.Name == dto.Type && !t.IsDeleted && t.IsActive))
            throw new Exception("Tipo de medio de pago inválido o inactivo");

        paymentMethod.Name = dto.Name;
        paymentMethod.Description = dto.Description ?? string.Empty;
        paymentMethod.Type = dto.Type;
        paymentMethod.AccountNumber = dto.AccountNumber;
        paymentMethod.BankName = dto.BankName;
        paymentMethod.AccountHolderName = dto.AccountHolderName;
        paymentMethod.WalletProvider = dto.WalletProvider;
        paymentMethod.WalletNumber = dto.WalletNumber;
        paymentMethod.QrCodeUrl = dto.QrCodeUrl;
        paymentMethod.Instructions = dto.Instructions;
        paymentMethod.DisplayOrder = dto.DisplayOrder;
        paymentMethod.IsActive = dto.IsActive;
        paymentMethod.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.PaymentMethods.UpdateAsync(paymentMethod);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DeletePaymentMethodAsync(Guid id)
    {
        await _unitOfWork.PaymentMethods.DeleteAsync(id);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task ActivatePaymentMethodAsync(Guid id)
    {
        var paymentMethod = await _unitOfWork.PaymentMethods.GetByIdAsync(id);
        if (paymentMethod == null || paymentMethod.IsDeleted)
            throw new Exception("Medio de pago no encontrado");

        paymentMethod.IsActive = true;
        paymentMethod.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.PaymentMethods.UpdateAsync(paymentMethod);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DeactivatePaymentMethodAsync(Guid id)
    {
        var paymentMethod = await _unitOfWork.PaymentMethods.GetByIdAsync(id);
        if (paymentMethod == null || paymentMethod.IsDeleted)
            throw new Exception("Medio de pago no encontrado");

        paymentMethod.IsActive = false;
        paymentMethod.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.PaymentMethods.UpdateAsync(paymentMethod);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<IEnumerable<PaymentMethodTypeSimpleDto>> GetPaymentMethodTypesAsync()
    {
        var types = await _unitOfWork.PaymentMethodTypes.GetActivePaymentMethodTypesAsync();
        return types.Select(t => new PaymentMethodTypeSimpleDto(t.Name, t.Label));
    }

    private async Task<Dictionary<string, string>> GetTypeLabelMapAsync()
    {
        var types = await _unitOfWork.PaymentMethodTypes.GetAllAsync();
        return types
            .Where(t => !t.IsDeleted)
            .ToDictionary(
                t => t.Name,
                t => string.IsNullOrWhiteSpace(t.Label) ? t.Name : t.Label,
                StringComparer.OrdinalIgnoreCase
            );
    }

    private static PaymentMethodDto MapToDto(PaymentMethod paymentMethod, Dictionary<string, string> typeLabels)
    {
        var typeLabel = typeLabels.TryGetValue(paymentMethod.Type, out var label)
            ? label
            : paymentMethod.Type;

        return new PaymentMethodDto(
            paymentMethod.Id,
            paymentMethod.Name,
            paymentMethod.Description,
            paymentMethod.Type,
            typeLabel,
            paymentMethod.AccountNumber,
            paymentMethod.BankName,
            paymentMethod.AccountHolderName,
            paymentMethod.WalletProvider,
            paymentMethod.WalletNumber,
            paymentMethod.QrCodeUrl,
            paymentMethod.Instructions,
            paymentMethod.DisplayOrder,
            paymentMethod.IsActive,
            paymentMethod.CreatedAt
        );
    }
}

