using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Nagaira.Ecommerce.Infrastructure.Data;

public static class DateTimeValueConverter
{
    public static ValueConverter<DateTime, DateTime> Create()
    {
        return new ValueConverter<DateTime, DateTime>(
            v => v.Kind == DateTimeKind.Utc ? v : DateTime.SpecifyKind(v.ToUniversalTime(), DateTimeKind.Utc),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
    }

    public static ValueConverter<DateTime?, DateTime?> CreateNullable()
    {
        return new ValueConverter<DateTime?, DateTime?>(
            v => v.HasValue 
                ? (v.Value.Kind == DateTimeKind.Utc 
                    ? v 
                    : DateTime.SpecifyKind(v.Value.ToUniversalTime(), DateTimeKind.Utc)) 
                : null,
            v => v.HasValue 
                ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) 
                : null);
    }
}

