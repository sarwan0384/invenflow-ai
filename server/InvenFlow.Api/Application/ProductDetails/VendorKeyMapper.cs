using Microsoft.Extensions.Options;

namespace InvenFlow.Api.Application.ProductDetails;

public class VendorKeyMappingOptions
{
    public Dictionary<string, string> ProviderAliases { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public interface IVendorKeyMapper
{
    string ResolveProviderKey(string? supplierRealId, string? vendorKey);
}

public class VendorKeyMapper : IVendorKeyMapper
{
    private static readonly Dictionary<string, string> DefaultAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ARROW"] = "ARROW",
        ["ARROW ELECTRONICS"] = "ARROW",
        ["DIGIKEY"] = "DIGIKEY",
        ["DIGI-KEY"] = "DIGIKEY",
        ["FETCHCHIPS"] = "FETCHCHIPS",
        ["FETCHCHIPS DIRECT"] = "FETCHCHIPS"
    };

    private readonly Dictionary<string, string> _aliases;

    public VendorKeyMapper(IOptions<VendorKeyMappingOptions> options)
    {
        _aliases = new Dictionary<string, string>(DefaultAliases, StringComparer.OrdinalIgnoreCase);

        foreach (var (key, value) in options.Value.ProviderAliases)
        {
            if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value))
            {
                _aliases[key.Trim()] = value.Trim().ToUpperInvariant();
            }
        }
    }

    public string ResolveProviderKey(string? supplierRealId, string? vendorKey)
    {
        var explicitVendorKey = Normalize(vendorKey);
        if (!string.IsNullOrWhiteSpace(explicitVendorKey) && _aliases.TryGetValue(explicitVendorKey, out var mappedExplicitKey))
        {
            return mappedExplicitKey;
        }

        if (!string.IsNullOrWhiteSpace(explicitVendorKey))
        {
            return explicitVendorKey;
        }

        var supplierKey = Normalize((supplierRealId ?? string.Empty).Split(':', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).FirstOrDefault());
        if (!string.IsNullOrWhiteSpace(supplierKey) && _aliases.TryGetValue(supplierKey, out var mappedSupplierKey))
        {
            return mappedSupplierKey;
        }

        return supplierKey;
    }

    private static string Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToUpperInvariant();
    }
}
