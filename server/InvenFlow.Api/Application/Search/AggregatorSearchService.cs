using System.Runtime.CompilerServices;
using InvenFlow.Api.Application.DTOs;
using InvenFlow.Api.Application.ProductDetails;
using Microsoft.Extensions.Options;

namespace InvenFlow.Api.Application.Search;

public class AggregatorSearchService
{
    private readonly Dictionary<string, IProductAdapter> _adaptersByKey;
    private readonly ProviderSettings _providerSettings;
    private readonly IVendorKeyMapper _vendorKeyMapper;
    private readonly ILogger<AggregatorSearchService> _logger;

    public AggregatorSearchService(
        IEnumerable<IProductAdapter> adapters,
        IOptions<ProviderSettings> providerSettings,
        IVendorKeyMapper vendorKeyMapper,
        ILogger<AggregatorSearchService> logger)
    {
        _adaptersByKey = adapters
            .GroupBy(ResolveAdapterKey, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        _providerSettings = providerSettings.Value;
        _vendorKeyMapper = vendorKeyMapper;
        _logger = logger;
    }

    public async Task<List<ProviderResultGroupDto>> SearchAsync(
        string query,
        string category,
        string? strategyModeOverride = null,
        string? preferredProviderOverride = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query) || string.IsNullOrWhiteSpace(category))
        {
            return new List<ProviderResultGroupDto>();
        }

        // Resolves from appsettings/dynamic settings if strategyModeOverride is null
        var strategyMode = ResolveStrategyMode(strategyModeOverride);
        var providerKeys = ResolveProviderKeys(strategyMode, preferredProviderOverride);

        _logger.LogInformation(
            "Aggregator search started for query {Query}, category {Category}, strategy {Strategy}, provider keys {ProviderKeys}", 
            query, category, strategyMode, string.Join(",", providerKeys));

        if (providerKeys.Count == 0)
        {
            _logger.LogWarning("No providers resolved for category {Category}", category);
            return new List<ProviderResultGroupDto>();
        }

        return strategyMode switch
        {
            StrategyMode.SingleProvider => await ExecuteSingleProviderAsync(query, providerKeys.First(), cancellationToken),
            StrategyMode.ParallelAggregator => await ExecuteParallelAsync(query, providerKeys, cancellationToken),
            _ => await ExecuteFallbackChainAsync(query, providerKeys, cancellationToken)
        };
    }

    private async Task<List<ProviderResultGroupDto>> ExecuteSingleProviderAsync(
        string query, 
        string providerKey, 
        CancellationToken cancellationToken)
    {
        var result = await ExecuteProviderAsync(providerKey, query, cancellationToken);
        return result.Results.Count == 0
            ? new List<ProviderResultGroupDto>()
            : new List<ProviderResultGroupDto> { result };
    }

    private async Task<List<ProviderResultGroupDto>> ExecuteParallelAsync(
        string query, 
        List<string> providerKeys, 
        CancellationToken cancellationToken)
    {
        var tasks = providerKeys
            .Select(providerKey => ExecuteProviderAsync(providerKey, query, cancellationToken))
            .ToList();

        var results = await Task.WhenAll(tasks);
        return results
            .Where(group => group.Results.Count > 0)
            .ToList();
    }

    private async Task<List<ProviderResultGroupDto>> ExecuteFallbackChainAsync(
        string query, 
        List<string> providerKeys, 
        CancellationToken cancellationToken)
    {
        foreach (var providerKey in providerKeys)
        {
            var result = await ExecuteProviderAsync(providerKey, query, cancellationToken);
            if (result.Results.Count > 0)
            {
                return new List<ProviderResultGroupDto> { result };
            }
        }

        return new List<ProviderResultGroupDto>();
    }

    private async Task<ProviderResultGroupDto> ExecuteProviderAsync(
        string providerKey, 
        string query, 
        CancellationToken cancellationToken)
    {
        if (string.Equals(providerKey, "MOCK", StringComparison.OrdinalIgnoreCase))
        {
            return BuildMockSearchGroup(query);
        }

        if (!_adaptersByKey.TryGetValue(providerKey, out var adapter))
        {
            _logger.LogWarning("No adapter registered for provider key {ProviderKey}", providerKey);
            return new ProviderResultGroupDto { ProviderName = providerKey, Results = new List<UniversalProductDto>() };
        }

        var providerName = ResolveProviderName(adapter);

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var results = await adapter.SearchAsync(query);
            foreach (var item in results)
            {
                if (string.IsNullOrWhiteSpace(item.ProviderName))
                {
                    item.ProviderName = providerName;
                }
            }

            var ordered = results
                .OrderByDescending(x => x.AvailableStock)
                .ThenBy(x => x.SKU)
                .ToList();

            _logger.LogInformation(
                "Provider {ProviderKey} adapter {AdapterName} returned {Count} results for query {Query}", 
                providerKey, adapter.GetType().Name, ordered.Count, query);

            return new ProviderResultGroupDto
            {
                ProviderName = providerName,
                Results = ordered
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex, 
                "Provider {ProviderKey} adapter {AdapterName} failed for query {Query}", 
                providerKey, adapter.GetType().Name, query);

            return new ProviderResultGroupDto
            {
                ProviderName = providerName,
                Results = new List<UniversalProductDto>()
            };
        }
    }

    public async IAsyncEnumerable<UniversalProductDto> SearchStreamAsync(
        string query,
        string category,
        string? strategyModeOverride = null,
        string? preferredProviderOverride = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var grouped = await SearchAsync(query, category, strategyModeOverride, preferredProviderOverride, cancellationToken);
        foreach (var group in grouped)
        {
            foreach (var result in group.Results)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return result;
            }
        }
    }

    private static string ResolveProviderName(IProductAdapter adapter)
    {
        return adapter switch
        {
            FetchchipsLocalAdapter => "Fetchchips Direct",
            ArrowElectronicsAdapter => "Arrow Electronics",
            DigiKeyElectronicsAdapter => "DigiKey",
            _ => adapter.GetType().Name
        };
    }

    private static string ResolveAdapterKey(IProductAdapter adapter)
    {
        return adapter switch
        {
            FetchchipsLocalAdapter => "DEFAULT",
            ArrowElectronicsAdapter => "ARROW",
            DigiKeyElectronicsAdapter => "DIGIKEY",
            _ => adapter.GetType().Name.ToUpperInvariant()
        };
    }

    private StrategyMode ResolveStrategyMode(string? strategyOverride)
    {
        var normalized = NormalizeStrategyName(strategyOverride);

        // Fallback to configured settings if override was omitted or empty
        if (string.IsNullOrWhiteSpace(normalized))
        {
            normalized = NormalizeStrategyName(_providerSettings.StrategyMode);
        }

        return normalized switch
        {
            "SINGLEPROVIDER" => StrategyMode.SingleProvider,
            "PARALLELAGGREGATOR" => StrategyMode.ParallelAggregator,
            _ => StrategyMode.FallbackChain
        };
    }

    private List<string> ResolveProviderKeys(StrategyMode strategyMode, string? preferredProviderOverride)
    {
        var keys = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (strategyMode == StrategyMode.SingleProvider)
        {
            var preferredKey = ResolveProviderKey(preferredProviderOverride);
            AddProviderKey(preferredKey, keys, seen);
            AddProviderKey(ResolveProviderKey(_providerSettings.TargetProvider), keys, seen);
            return keys;
        }

        var configured = strategyMode == StrategyMode.ParallelAggregator
            ? _providerSettings.ActiveParallelProviders
            : _providerSettings.FallbackSequence;

        if (strategyMode == StrategyMode.FallbackChain)
        {
            var preferredKey = ResolveProviderKey(preferredProviderOverride);
            AddProviderKey(preferredKey, keys, seen);
        }

        foreach (var configuredKey in configured)
        {
            AddProviderKey(ResolveProviderKey(configuredKey), keys, seen);
        }
        return keys;
    }

    private string ResolveProviderKey(string? key)
    {
        return _vendorKeyMapper.ResolveProviderKey(null, key);
    }

    private static void AddProviderKey(string key, ICollection<string> keys, ISet<string> seen)
    {
        if (string.IsNullOrWhiteSpace(key) || !seen.Add(key))
        {
            return;
        }

        keys.Add(key);
    }

    private static string NormalizeStrategyName(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().Replace("-", string.Empty, StringComparison.Ordinal).Replace("_", string.Empty, StringComparison.Ordinal).ToUpperInvariant();
    }

    private ProviderResultGroupDto BuildMockSearchGroup(string query)
    {
        var normalizedQuery = (query ?? string.Empty).Trim().ToUpperInvariant();
        var part = string.IsNullOrWhiteSpace(normalizedQuery) ? "DEMO-PART" : normalizedQuery;

        var results = new List<UniversalProductDto>
        {
            new()
            {
                ProviderName = "Mock Demo Provider",
                DistiSku = $"MOCK-{part}-A",
                PartNumber = part,
                PackagingContainer = "Tape & Reel",
                MinQty = 1,
                RegionStock = "Demo Warehouse - 12000",
                RoHSStatus = "Compliant",
                LeadTime = "In stock",
                ItemId = $"MOCK-{part}-01",
                Category = "Electronics",
                Title = $"{part} Mock Search Result A",
                BrandOrManufacturer = "Demo Semiconductor",
                SKU = part,
                Description = $"Mock search result for {part}.",
                PublicSupplierName = "Mock Demo Provider",
                SupplierRealId = $"MOCK:{part}",
                DirectPurchaseUrl = $"https://invenflow.local/demo/search/{Uri.EscapeDataString(part)}",
                VendorCartId = $"MOCK:{part}",
                AvailableStock = 12000,
                AvailabilityStatus = "In Stock",
                Currency = "USD",
                PriceBreaks = new List<PriceTierDto>
                {
                    new() { Qty = 1, UnitPrice = 0.08m },
                    new() { Qty = 100, UnitPrice = 0.05m }
                },
                Attributes = new Dictionary<string, string>
                {
                    ["Source"] = "MockSearch",
                    ["Mode"] = "SingleProvider"
                }
            }
        };

        return new ProviderResultGroupDto
        {
            ProviderName = "Mock Demo Provider",
            Results = results
        };
    }

    private enum StrategyMode
    {
        SingleProvider,
        ParallelAggregator,
        FallbackChain
    }
}

public class ProviderResultGroupDto
{
    public string ProviderName { get; set; } = string.Empty;
    public List<UniversalProductDto> Results { get; set; } = new();
}