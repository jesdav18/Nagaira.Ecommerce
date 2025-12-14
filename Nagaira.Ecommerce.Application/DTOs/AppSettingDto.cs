using System.ComponentModel.DataAnnotations;

namespace Nagaira.Ecommerce.Application.DTOs;

public record AppSettingDto(
    Guid Id,
    string Key,
    string? Value,
    string Label,
    string? Description,
    string Category,
    string DataType,
    int DisplayOrder,
    bool IsActive,
    DateTime CreatedAt
);

public record CreateAppSettingDto(
    [Required(ErrorMessage = "La clave es requerida")]
    [StringLength(100, ErrorMessage = "La clave no puede exceder 100 caracteres")]
    string Key,
    
    string? Value,
    
    [Required(ErrorMessage = "La etiqueta es requerida")]
    [StringLength(255, ErrorMessage = "La etiqueta no puede exceder 255 caracteres")]
    string Label,
    
    string? Description,
    
    [Required(ErrorMessage = "La categoría es requerida")]
    [StringLength(50, ErrorMessage = "La categoría no puede exceder 50 caracteres")]
    string Category,
    
    [Required(ErrorMessage = "El tipo de dato es requerido")]
    [StringLength(20, ErrorMessage = "El tipo de dato no puede exceder 20 caracteres")]
    string DataType,
    
    int DisplayOrder,
    
    bool IsActive
);

public record UpdateAppSettingDto(
    Guid Id,
    
    [Required(ErrorMessage = "La clave es requerida")]
    [StringLength(100, ErrorMessage = "La clave no puede exceder 100 caracteres")]
    string Key,
    
    string? Value,
    
    [Required(ErrorMessage = "La etiqueta es requerida")]
    [StringLength(255, ErrorMessage = "La etiqueta no puede exceder 255 caracteres")]
    string Label,
    
    string? Description,
    
    [Required(ErrorMessage = "La categoría es requerida")]
    [StringLength(50, ErrorMessage = "La categoría no puede exceder 50 caracteres")]
    string Category,
    
    [Required(ErrorMessage = "El tipo de dato es requerido")]
    [StringLength(20, ErrorMessage = "El tipo de dato no puede exceder 20 caracteres")]
    string DataType,
    
    int DisplayOrder,
    
    bool IsActive
);

