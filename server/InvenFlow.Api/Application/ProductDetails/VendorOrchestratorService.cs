using InvenFlow.Api.Application.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace InvenFlow.Api.Application.ProductDetails;

public class VendorOrchestratorService : IVendorDetailsService
{
    private const string NoDetailsDescription = "No product details were found for this selection.";

    private readonly Dictionary<string, IVendorDetailsProvider> _providersByKey;
    private readonly ProviderSettings _settings;
    private readonly IVendorKeyMapper _vendorKeyMapper;
    private readonly ILogger<VendorOrchestratorService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public VendorOrchestratorService(
        IEnumerable<IVendorDetailsProvider> providers,
        IVendorKeyMapper vendorKeyMapper,
        IOptions<ProviderSettings> settings,
        IHttpContextAccessor httpContextAccessor,
        ILogger<VendorOrchestratorService> logger)
    {
        _providersByKey = providers
            .GroupBy(provider => Normalize(provider.VendorKey), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        _vendorKeyMapper = vendorKeyMapper;
        _settings = settings.Value;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<ProductDetailDto> GetDetailsAsync(
        string? supplierRealId,
        string? mpn,
        string? vendorKey,
        VendorSelectionStrategy? strategy,
        string? preferredProvider,
        CancellationToken cancellationToken = default)
    {
        var normalizedMpn = (mpn ?? string.Empty).Trim();
        var normalizedSupplierRealId = (supplierRealId ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedMpn))
        {
            return new ProductDetailDto();
        }

        var queryPreferredProvider = ResolveQueryOverride("preferredProvider");
        var queryStrategyMode = ResolveQueryOverride("strategyMode") ?? ResolveQueryOverride("strategy");
        var preferredProviderValue = queryPreferredProvider ?? preferredProvider;
        var requestedProviderKey = ResolveRequestedProviderKey(normalizedSupplierRealId, vendorKey, preferredProviderValue);
        var selectedStrategy = ResolveStrategyMode(strategy, queryStrategyMode);
        var singleModeProviderOverride = ResolveExplicitPreferredProvider(preferredProviderValue);

        return selectedStrategy switch
        {
            ProviderStrategyMode.SingleProvider => await ExecuteSingleProviderAsync(normalizedSupplierRealId, normalizedMpn, singleModeProviderOverride, cancellationToken),
            ProviderStrategyMode.ParallelAggregator => await ExecuteParallelAggregatorAsync(normalizedSupplierRealId, normalizedMpn, requestedProviderKey, cancellationToken),
            _ => await ExecuteFallbackChainAsync(normalizedSupplierRealId, normalizedMpn, requestedProviderKey, cancellationToken)
        };
    }

    private async Task<ProductDetailDto> ExecuteSingleProviderAsync(string supplierRealId, string mpn, string requestedProviderKey, CancellationToken cancellationToken)
    {
        var targetProviderKey = !string.IsNullOrWhiteSpace(requestedProviderKey)
            ? requestedProviderKey
            : ResolveConfiguredProviderKey(_settings.TargetProvider);
        var provider = TryGetEnabledProvider(targetProviderKey);
        if (provider is null)
        {
            return BuildUnavailableResult(supplierRealId, mpn, targetProviderKey);
        }

        return await InvokeProviderAsync(provider, supplierRealId, mpn, cancellationToken)
            ?? BuildUnavailableResult(supplierRealId, mpn, provider.VendorKey);
    }

    private async Task<ProductDetailDto> ExecuteParallelAggregatorAsync(string supplierRealId, string mpn, string requestedProviderKey, CancellationToken cancellationToken)
    {
        var enabledProviders = ResolveParallelProviders(requestedProviderKey).ToList();
        if (enabledProviders.Count == 0)
        {
            return BuildUnavailableResult(supplierRealId, mpn, requestedProviderKey);
        }

        var tasks = enabledProviders.Select(async provider => new ProviderAttempt(provider, await InvokeProviderAsync(provider, supplierRealId, mpn, cancellationToken))).ToList();
        var results = await Task.WhenAll(tasks);

        return MergeParallelResults(results, supplierRealId, mpn, requestedProviderKey);
    }

    private async Task<ProductDetailDto> ExecuteFallbackChainAsync(string supplierRealId, string mpn, string requestedProviderKey, CancellationToken cancellationToken)
    {
        foreach (var provider in ResolveFallbackChain(requestedProviderKey))
        {
            var result = await InvokeProviderAsync(provider, supplierRealId, mpn, cancellationToken);
            if (result is not null && HasMeaningfulResult(result))
            {
                return result;
            }
        }

        return BuildUnavailableResult(supplierRealId, mpn, requestedProviderKey);
    }

    private IEnumerable<IVendorDetailsProvider> ResolveFallbackChain(string requestedProviderKey)
    {
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var key in EnumerateFallbackKeys(requestedProviderKey))
        {
            var normalizedKey = ResolveConfiguredProviderKey(key);
            if (string.IsNullOrWhiteSpace(normalizedKey) || !visited.Add(normalizedKey))
            {
                continue;
            }

            var provider = TryGetEnabledProvider(normalizedKey);
            if (provider is not null)
            {
                yield return provider;
            }
        }
    }

    private IEnumerable<IVendorDetailsProvider> ResolveParallelProviders(string requestedProviderKey)
    {
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var key in EnumerateParallelKeys(requestedProviderKey))
        {
            var normalizedKey = ResolveConfiguredProviderKey(key);
            if (string.IsNullOrWhiteSpace(normalizedKey) || !visited.Add(normalizedKey))
            {
                continue;
            }

            var provider = TryGetEnabledProvider(normalizedKey);
            if (provider is not null)
            {
                yield return provider;
            }
        }
    }

    private IEnumerable<string> EnumerateFallbackKeys(string requestedProviderKey)
    {
        if (!string.IsNullOrWhiteSpace(requestedProviderKey))
        {
            yield return requestedProviderKey;
        }

        foreach (var key in _settings.FallbackSequence)
        {
            yield return key;
        }

        if (!string.IsNullOrWhiteSpace(_settings.TargetProvider))
        {
            yield return _settings.TargetProvider;
        }
    }

    private IEnumerable<string> EnumerateParallelKeys(string requestedProviderKey)
    {
        if (!string.IsNullOrWhiteSpace(requestedProviderKey))
        {
            yield return requestedProviderKey;
        }

        foreach (var key in _settings.ActiveParallelProviders)
        {
            yield return key;
        }

        if (!string.IsNullOrWhiteSpace(_settings.TargetProvider))
        {
            yield return _settings.TargetProvider;
        }
    }

    private async Task<ProductDetailDto?> InvokeProviderAsync(IVendorDetailsProvider provider, string supplierRealId, string mpn, CancellationToken cancellationToken)
    {
        try
        {
            return await provider.FetchDetailsAsync(supplierRealId, mpn, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Vendor details provider {VendorKey} failed for MPN {Mpn}", provider.VendorKey, mpn);
            return null;
        }
    }

    private string ResolveRequestedProviderKey(string supplierRealId, string? vendorKey, string? preferredProvider)
    {
        var normalizedPreferredProvider = Normalize(preferredProvider);
        if (!string.IsNullOrWhiteSpace(normalizedPreferredProvider))
        {
            return ResolveConfiguredProviderKey(normalizedPreferredProvider);
        }

        return ResolveConfiguredProviderKey(_vendorKeyMapper.ResolveProviderKey(supplierRealId, vendorKey));
    }

    private string ResolveExplicitPreferredProvider(string? preferredProvider)
    {
        var normalizedPreferredProvider = Normalize(preferredProvider);
        if (string.IsNullOrWhiteSpace(normalizedPreferredProvider))
        {
            return string.Empty;
        }

        return ResolveConfiguredProviderKey(normalizedPreferredProvider);
    }

    private ProviderStrategyMode ResolveStrategyMode(VendorSelectionStrategy? strategy, string? strategyOverride)
    {
        if (TryParseStrategyMode(strategyOverride, out var explicitMode))
        {
            return explicitMode;
        }

        if (strategy.HasValue)
        {
            return strategy.Value switch
            {
                VendorSelectionStrategy.DirectTargeted => ProviderStrategyMode.SingleProvider,
                VendorSelectionStrategy.Parallel => ProviderStrategyMode.ParallelAggregator,
                _ => ProviderStrategyMode.FallbackChain
            };
        }

        return TryParseStrategyMode(_settings.StrategyMode, out var configuredMode)
            ? configuredMode
            : ProviderStrategyMode.FallbackChain;
    }

    private bool TryParseStrategyMode(string? value, out ProviderStrategyMode mode)
    {
        var normalized = NormalizeStrategyName(value);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            mode = ProviderStrategyMode.FallbackChain;
            return false;
        }

        return normalized switch
        {
            "SINGLEPROVIDER" or "DIRECTTARGETED" => AssignStrategyMode(ProviderStrategyMode.SingleProvider, out mode),
            "PARALLELAGGREGATOR" or "PARALLEL" => AssignStrategyMode(ProviderStrategyMode.ParallelAggregator, out mode),
            "FALLBACKCHAIN" => AssignStrategyMode(ProviderStrategyMode.FallbackChain, out mode),
            _ => AssignStrategyMode(ProviderStrategyMode.FallbackChain, out mode, false)
        };
    }

    private static bool AssignStrategyMode(ProviderStrategyMode resolvedMode, out ProviderStrategyMode mode, bool success = true)
    {
        mode = resolvedMode;
        return success;
    }

    private ProductDetailDto MergeParallelResults(IEnumerable<ProviderAttempt> attempts, string supplierRealId, string mpn, string requestedProviderKey)
    {
        var successfulAttempts = attempts
            .Where(attempt => attempt.Details is not null && HasMeaningfulResult(attempt.Details))
            .ToList();

        if (successfulAttempts.Count == 0)
        {
            return BuildUnavailableResult(supplierRealId, mpn, requestedProviderKey);
        }

        var preferredKey = Normalize(requestedProviderKey);
        var primaryAttempt = successfulAttempts
            .OrderByDescending(attempt => ScoreResult(attempt.Provider.VendorKey, attempt.Details, preferredKey))
            .First();
        var primary = primaryAttempt.Details!;
        var secondaryDetails = successfulAttempts
            .Where(attempt => !ReferenceEquals(attempt, primaryAttempt))
            .Select(attempt => attempt.Details!)
            .ToList();
        var mergedAlternateOffers = successfulAttempts
            .SelectMany(attempt => BuildOffersForAggregation(attempt.Details!, ReferenceEquals(attempt, primaryAttempt)))
            .GroupBy(offer => BuildOfferIdentity(offer), StringComparer.OrdinalIgnoreCase)
            .Select(group => group.OrderByDescending(offer => offer.AvailableStock).First())
            .OrderByDescending(offer => offer.AvailableStock)
            .ThenBy(offer => offer.ProviderName)
            .ToList();
        var mergedPriceBreaks = successfulAttempts
            .SelectMany(attempt => attempt.Details!.PriceBreaks)
            .GroupBy(priceBreak => new { priceBreak.Qty, priceBreak.UnitPrice })
            .Select(group => group.First())
            .OrderBy(priceBreak => priceBreak.Qty)
            .ThenBy(priceBreak => priceBreak.UnitPrice)
            .ToList();

        var merged = new ProductDetailDto
        {
            ProviderName = successfulAttempts.Count > 1 ? $"{primary.ProviderName} (Aggregated)" : primary.ProviderName,
            SupplierRealId = string.IsNullOrWhiteSpace(primary.SupplierRealId) ? supplierRealId : primary.SupplierRealId,
            VendorCartId = primary.VendorCartId,
            Mpn = string.IsNullOrWhiteSpace(primary.Mpn) ? mpn : primary.Mpn,
            Manufacturer = FirstNonEmpty(primary.Manufacturer, secondaryDetails.Select(detail => detail.Manufacturer)),
            Category = FirstNonEmpty(primary.Category, secondaryDetails.Select(detail => detail.Category)),
            Description = BuildAggregatedDescription(primary.Description, successfulAttempts.Count),
            DatasheetUrl = FirstNonEmpty(primary.DatasheetUrl, secondaryDetails.Select(detail => detail.DatasheetUrl)),
            DirectPurchaseUrl = FirstNonEmpty(primary.DirectPurchaseUrl, secondaryDetails.Select(detail => detail.DirectPurchaseUrl)),
            AvailableStock = successfulAttempts.Sum(attempt => Math.Max(0, attempt.Details!.AvailableStock)),
            LeadTime = FirstNonEmpty(primary.LeadTime, secondaryDetails.Select(detail => detail.LeadTime)),
            MinQty = ResolvePositive(primary.MinQty, secondaryDetails.Select(detail => detail.MinQty), 1),
            OrderMultiple = ResolvePositive(primary.OrderMultiple, secondaryDetails.Select(detail => detail.OrderMultiple), 1),
            PackagingContainer = FirstNonEmpty(primary.PackagingContainer, secondaryDetails.Select(detail => detail.PackagingContainer)),
            Currency = FirstNonEmpty(primary.Currency, secondaryDetails.Select(detail => detail.Currency), "USD"),
            PriceBreaks = mergedPriceBreaks,
            Specifications = MergeSpecifications(primary, secondaryDetails),
            AlternateOffers = mergedAlternateOffers
        };

        return merged;
    }

    private static IEnumerable<ProductOfferDto> BuildOffersForAggregation(ProductDetailDto detail, bool isPrimary)
    {
        if (!isPrimary)
        {
            yield return MapDetailToOffer(detail);
        }

        foreach (var offer in detail.AlternateOffers)
        {
            yield return offer;
        }
    }

    private static ProductOfferDto MapDetailToOffer(ProductDetailDto detail)
    {
        return new ProductOfferDto
        {
            ProviderName = detail.ProviderName,
            SupplierRealId = detail.SupplierRealId,
            PartNumber = detail.Mpn,
            DistiSku = detail.VendorCartId,
            AvailableStock = detail.AvailableStock,
            LeadTime = detail.LeadTime,
            MinQty = detail.MinQty,
            OrderMultiple = detail.OrderMultiple,
            PackagingContainer = detail.PackagingContainer,
            Currency = string.IsNullOrWhiteSpace(detail.Currency) ? "USD" : detail.Currency,
            BestUnitPrice = detail.PriceBreaks.Where(priceBreak => priceBreak.Qty > 0).OrderBy(priceBreak => priceBreak.UnitPrice).Select(priceBreak => priceBreak.UnitPrice).FirstOrDefault(),
            DirectPurchaseUrl = detail.DirectPurchaseUrl
        };
    }

    private static Dictionary<string, string> MergeSpecifications(ProductDetailDto primary, IEnumerable<ProductDetailDto> secondaryDetails)
    {
        var merged = new Dictionary<string, string>(primary.Specifications, StringComparer.OrdinalIgnoreCase);
        var providerNames = new List<string> { primary.ProviderName };

        foreach (var detail in secondaryDetails)
        {
            if (!string.IsNullOrWhiteSpace(detail.ProviderName))
            {
                providerNames.Add(detail.ProviderName);
            }

            foreach (var pair in detail.Specifications)
            {
                if (!merged.ContainsKey(pair.Key) && !string.IsNullOrWhiteSpace(pair.Value))
                {
                    merged[pair.Key] = pair.Value;
                }
            }
        }

        merged["AggregationMode"] = "ParallelAggregator";
        merged["AggregatedProviders"] = string.Join(", ", providerNames.Where(name => !string.IsNullOrWhiteSpace(name)).Distinct(StringComparer.OrdinalIgnoreCase));
        return merged;
    }

    private static string BuildAggregatedDescription(string primaryDescription, int providerCount)
    {
        var description = string.IsNullOrWhiteSpace(primaryDescription) ? "Aggregated product details payload." : primaryDescription.Trim();
        return providerCount > 1
            ? $"{description} Aggregated from {providerCount} providers."
            : description;
    }

    private string ResolveConfiguredProviderKey(string? providerKey)
    {
        return _vendorKeyMapper.ResolveProviderKey(null, providerKey);
    }

    private string? ResolveQueryOverride(string key)
    {
        var query = _httpContextAccessor.HttpContext?.Request?.Query;
        if (query is null || !query.TryGetValue(key, out var values))
        {
            return null;
        }

        var rawValue = values.FirstOrDefault();
        return string.IsNullOrWhiteSpace(rawValue) ? null : rawValue.Trim();
    }

    private IVendorDetailsProvider? TryGetEnabledProvider(string? providerKey)
    {
        var normalizedKey = Normalize(providerKey);
        if (string.IsNullOrWhiteSpace(normalizedKey))
        {
            return null;
        }

        return _providersByKey.TryGetValue(normalizedKey, out var provider) && provider.IsEnabled
            ? provider
            : null;
    }

    private static bool HasMeaningfulResult(ProductDetailDto result)
    {
        if (string.IsNullOrWhiteSpace(result.Mpn))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(result.Manufacturer)
            || !string.IsNullOrWhiteSpace(result.Category)
            || result.AvailableStock > 0
            || result.PriceBreaks.Count > 0
            || result.AlternateOffers.Count > 0
            || !string.IsNullOrWhiteSpace(result.DirectPurchaseUrl))
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(result.Description)
            && !string.Equals(result.Description, NoDetailsDescription, StringComparison.OrdinalIgnoreCase);
    }

    private static int ScoreResult(string providerKey, ProductDetailDto? result, string preferredKey)
    {
        if (result is null)
        {
            return int.MinValue;
        }

        var score = 0;
        if (string.Equals(Normalize(providerKey), preferredKey, StringComparison.OrdinalIgnoreCase))
        {
            score += 200;
        }

        if (!string.IsNullOrWhiteSpace(result.Manufacturer))
        {
            score += 50;
        }

        if (!string.IsNullOrWhiteSpace(result.Category))
        {
            score += 35;
        }

        if (!string.IsNullOrWhiteSpace(result.Description) && !string.Equals(result.Description, NoDetailsDescription, StringComparison.OrdinalIgnoreCase))
        {
            score += 20;
        }

        if (!string.IsNullOrWhiteSpace(result.DirectPurchaseUrl))
        {
            score += 10;
        }

        score += Math.Min(100, Math.Max(0, result.AvailableStock));
        score += result.PriceBreaks.Count * 10;
        score += result.AlternateOffers.Count * 5;
        score += result.Specifications.Count;

        return score;
    }

    private static ProductDetailDto BuildUnavailableResult(string supplierRealId, string mpn, string? providerKey)
    {
        var normalizedProviderKey = Normalize(providerKey);

        return new ProductDetailDto
        {
            ProviderName = string.IsNullOrWhiteSpace(normalizedProviderKey) ? string.Empty : normalizedProviderKey,
            SupplierRealId = supplierRealId,
            Mpn = mpn,
            Description = string.IsNullOrWhiteSpace(normalizedProviderKey)
                ? NoDetailsDescription
                : $"Provider {normalizedProviderKey} is unavailable or returned no details."
        };
    }

    private static string Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToUpperInvariant();
    }

    private static string NormalizeStrategyName(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().Replace("-", string.Empty, StringComparison.Ordinal).Replace("_", string.Empty, StringComparison.Ordinal).ToUpperInvariant();
    }

    private static string FirstNonEmpty(string primary, IEnumerable<string> fallbacks, string defaultValue = "")
    {
        if (!string.IsNullOrWhiteSpace(primary))
        {
            return primary;
        }

        return fallbacks.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? defaultValue;
    }

    private static int ResolvePositive(int primary, IEnumerable<int> fallbacks, int defaultValue)
    {
        if (primary > 0)
        {
            return primary;
        }

        return fallbacks.FirstOrDefault(value => value > 0, defaultValue);
    }

    private static string BuildOfferIdentity(ProductOfferDto offer)
    {
        return string.Join("|", offer.ProviderName, offer.SupplierRealId, offer.PartNumber, offer.DistiSku);
    }

    private enum ProviderStrategyMode
    {
        SingleProvider,
        ParallelAggregator,
        FallbackChain
    }

    private sealed record ProviderAttempt(IVendorDetailsProvider Provider, ProductDetailDto? Details);
}