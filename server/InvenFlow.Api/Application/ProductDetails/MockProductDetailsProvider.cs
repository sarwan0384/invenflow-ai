using InvenFlow.Api.Application.DTOs;

namespace InvenFlow.Api.Application.ProductDetails;

public class MockProductDetailsProvider : IVendorDetailsProvider
{
    public string VendorKey => "MOCK";
    public bool IsEnabled => true;

    public Task<ProductDetailDto> FetchDetailsAsync(string supplierRealId, string mpn, CancellationToken cancellationToken = default)
    {
        var normalizedMpn = (mpn ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedMpn))
        {
            return Task.FromResult(new ProductDetailDto());
        }

        var context = InferContext(normalizedMpn);
        var seed = ComputeSeed(normalizedMpn);
        var unitPrice = Math.Round(0.18m + (seed % 200) / 100m, 2);
        var orderMultiple = context.Category.Contains("Microcontroller", StringComparison.OrdinalIgnoreCase) ? 5 : 10;
        var stock = 120 + (seed % 900);
        var effectiveSupplierRealId = string.IsNullOrWhiteSpace(supplierRealId)
            ? $"MOCK:{normalizedMpn.ToUpperInvariant()}"
            : supplierRealId.Trim();
        var alternateOffers = BuildMockSearchArray(normalizedMpn, context.Package, orderMultiple, stock, unitPrice);

        var detail = new ProductDetailDto
        {
            ProviderName = "Mock Demo Provider",
            SupplierRealId = effectiveSupplierRealId,
            VendorCartId = $"MOCK:{normalizedMpn.ToUpperInvariant()}",
            Mpn = normalizedMpn,
            Manufacturer = context.Manufacturer,
            Category = context.Category,
            Description = $"Demo-ready {context.Category.ToLowerInvariant()} profile for {normalizedMpn} from {context.Manufacturer}. Routed via {ResolveChannel(supplierRealId)}.",
            DatasheetUrl = $"https://invenflow.local/demo/datasheets/{Uri.EscapeDataString(normalizedMpn)}.pdf",
            DirectPurchaseUrl = $"https://invenflow.local/demo/products/{Uri.EscapeDataString(normalizedMpn)}",
            AvailableStock = stock,
            LeadTime = stock > 250 ? "In stock" : "2-3 business days",
            MinQty = orderMultiple,
            OrderMultiple = orderMultiple,
            PackagingContainer = context.Package,
            Currency = "USD",
            PriceBreaks = new List<PriceTierDto>
            {
                new() { Qty = orderMultiple, UnitPrice = unitPrice },
                new() { Qty = orderMultiple * 10, UnitPrice = Math.Round(unitPrice * 0.92m, 2) },
                new() { Qty = orderMultiple * 50, UnitPrice = Math.Round(unitPrice * 0.84m, 2) }
            },
            Specifications = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Voltage"] = context.Voltage,
                ["Current"] = context.Current,
                ["Package"] = context.Package,
                ["Lifecycle"] = context.Lifecycle,
                ["DemoMode"] = "Mock provider",
                ["GeneratedOfferCount"] = alternateOffers.Count.ToString()
            },
            AlternateOffers = alternateOffers
        };

        return Task.FromResult(detail);
    }

    private static List<ProductOfferDto> BuildMockSearchArray(string mpn, string packageName, int orderMultiple, int stock, decimal unitPrice)
    {
        var normalizedMpn = mpn.ToUpperInvariant();

        return new List<ProductOfferDto>
        {
            new()
            {
                ProviderName = "Mock Demo Backup",
                SupplierRealId = $"MOCK-B:{normalizedMpn}",
                PartNumber = mpn,
                DistiSku = $"ALT-{normalizedMpn}",
                AvailableStock = Math.Max(50, stock / 2),
                LeadTime = "5 business days",
                MinQty = orderMultiple,
                OrderMultiple = orderMultiple,
                PackagingContainer = packageName,
                Currency = "USD",
                BestUnitPrice = Math.Round(unitPrice * 0.97m, 2),
                DirectPurchaseUrl = $"https://invenflow.local/demo/alternate/{Uri.EscapeDataString(mpn)}"
            },
            new()
            {
                ProviderName = "Mock Demo Express",
                SupplierRealId = $"MOCK-X:{normalizedMpn}",
                PartNumber = mpn,
                DistiSku = $"FAST-{normalizedMpn}",
                AvailableStock = Math.Max(25, stock / 3),
                LeadTime = "Next-day dispatch",
                MinQty = orderMultiple,
                OrderMultiple = orderMultiple,
                PackagingContainer = packageName,
                Currency = "USD",
                BestUnitPrice = Math.Round(unitPrice * 1.04m, 2),
                DirectPurchaseUrl = $"https://invenflow.local/demo/express/{Uri.EscapeDataString(mpn)}"
            },
            new()
            {
                ProviderName = "Mock Demo Volume",
                SupplierRealId = $"MOCK-V:{normalizedMpn}",
                PartNumber = mpn,
                DistiSku = $"VOL-{normalizedMpn}",
                AvailableStock = Math.Max(200, stock),
                LeadTime = "7 business days",
                MinQty = orderMultiple * 5,
                OrderMultiple = orderMultiple * 5,
                PackagingContainer = packageName,
                Currency = "USD",
                BestUnitPrice = Math.Round(unitPrice * 0.88m, 2),
                DirectPurchaseUrl = $"https://invenflow.local/demo/volume/{Uri.EscapeDataString(mpn)}"
            }
        };
    }

    private static (string Manufacturer, string Category, string Package, string Voltage, string Current, string Lifecycle) InferContext(string mpn)
    {
        var value = mpn.ToUpperInvariant();

        if (value.StartsWith("STM32", StringComparison.Ordinal))
        {
            return ("STMicroelectronics", "Microcontroller", "LQFP-64", "1.8-3.6V", "120mA", "Active");
        }

        if (value.StartsWith("ESP", StringComparison.Ordinal))
        {
            return ("Espressif Systems", "Wireless MCU", "QFN-48", "3.0-3.6V", "240mA", "Active");
        }

        if (value.StartsWith("ATMEGA", StringComparison.Ordinal) || value.StartsWith("ATSAM", StringComparison.Ordinal))
        {
            return ("Microchip Technology", "Microcontroller", "TQFP-32", "1.8-5.5V", "90mA", "Active");
        }

        if (value.StartsWith("LM", StringComparison.Ordinal) || value.StartsWith("OPA", StringComparison.Ordinal) || value.StartsWith("AD", StringComparison.Ordinal))
        {
            return ("Analog Devices", "Analog IC", "SOIC-8", "2.7-36V", "25mA", "Active");
        }

        if (value.StartsWith("IRF", StringComparison.Ordinal) || value.Contains("MOSFET", StringComparison.Ordinal))
        {
            return ("Infineon", "Power MOSFET", "TO-220", "20-100V", "75A", "Active");
        }

        return ("Demo Semiconductor", "General Purpose IC", "SMD", "3.3V", "N/A", "Sample");
    }

    private static string ResolveChannel(string supplierRealId)
    {
        if (string.IsNullOrWhiteSpace(supplierRealId))
        {
            return "generic demo inventory";
        }

        var prefix = supplierRealId.Split(':', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();

        return string.IsNullOrWhiteSpace(prefix) ? "generic demo inventory" : prefix;
    }

    private static int ComputeSeed(string value)
    {
        var seed = 17;
        foreach (var ch in value)
        {
            seed = (seed * 31) + ch;
        }

        return Math.Abs(seed);
    }
}