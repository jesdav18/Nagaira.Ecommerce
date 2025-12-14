namespace Nagaira.Ecommerce.Domain.Entities;

public class AppSetting : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string Label { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = "general";
    public string DataType { get; set; } = "string";
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
}

