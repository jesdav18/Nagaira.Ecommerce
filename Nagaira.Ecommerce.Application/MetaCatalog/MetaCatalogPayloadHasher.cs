using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Nagaira.Ecommerce.Application.Interfaces;

namespace Nagaira.Ecommerce.Application.MetaCatalog;

public class MetaCatalogPayloadHasher : IMetaCatalogPayloadHasher
{
    public string HashUpsert(MetaCatalogProduct item)
    {
        var canonical = string.Join('\n',
        [
            Field("action", "upsert"),
            Field("retailer_id", item.RetailerId),
            Field("name", item.Name),
            Field("description", item.Description),
            Field("brand", item.Brand),
            Field("availability", item.Availability),
            Field("condition", item.Condition),
            Field("price", NormalizeDecimalString(item.Price)),
            Field("currency", item.Currency),
            Field("url", item.Url),
            Field("image_url", item.ImageUrl),
            Field("category_name", item.CategoryName),
            Field("sku", item.Sku)
        ]);

        return Sha256(canonical);
    }

    public string HashDelete(string retailerId)
    {
        var canonical = string.Join('\n',
        [
            Field("action", "delete"),
            Field("retailer_id", retailerId)
        ]);

        return Sha256(canonical);
    }

    private static string Field(string name, string? value)
    {
        return $"{name}={NormalizeString(value)}";
    }

    private static string NormalizeString(string? value)
    {
        return value?.Trim() ?? string.Empty;
    }

    private static string NormalizeDecimalString(string? value)
    {
        var normalized = NormalizeString(value);
        return decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
            ? parsed.ToString("0.00", CultureInfo.InvariantCulture)
            : normalized;
    }

    private static string Sha256(string canonical)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
