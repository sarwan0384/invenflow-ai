using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using InvenFlow.Api.Application.DTOs;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace InvenFlow.Api.Application.Search;

public class NexarElectronicsAdapter : IProductAdapter
{
    private const string TokenCacheKey = "nexar-access-token";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<NexarElectronicsAdapter> _logger;
    private readonly IMemoryCache _cache;

    public NexarElectronicsAdapter(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<NexarElectronicsAdapter> logger,
        IMemoryCache cache)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _cache = cache;
    }

    public string CategoryDomain => "Electronics";

    public async Task<List<UniversalProductDto>> SearchAsync(string query)
    {
        var normalizedQuery = query?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedQuery))
        {
            return new List<UniversalProductDto>();
        }

        var endpoint = _configuration["NexarApi:Endpoint"] ?? "https://api.nexar.com/graphql";

        try
        {
            _logger.LogInformation("Executing live Nexar search for {Query}", normalizedQuery);
            Console.WriteLine($"[NEXAR] Querying for: {normalizedQuery}");

            var token = await GetAccessTokenAsync();
            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogWarning("Nexar token missing; skipping external provider for {Query}", normalizedQuery);
                return new List<UniversalProductDto>();
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = new StringContent(BuildGraphQlPayload(normalizedQuery), Encoding.UTF8, "application/json");

            using var response = await _httpClient.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[NEXAR] Raw HTTP Response Status: {response.StatusCode}");
            Console.WriteLine($"[NEXAR] Raw JSON Payload: {body}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Nexar GraphQL request failed with status {StatusCode}: {Body}", (int)response.StatusCode, body);
                return new List<UniversalProductDto>();
            }

            var parsed = JsonSerializer.Deserialize<NexarGraphQlResponse>(body, JsonOptions);
            if (parsed?.Errors?.Count > 0)
            {
                _logger.LogError("Nexar GraphQL returned errors: {Errors}", JsonSerializer.Serialize(parsed.Errors));
                return new List<UniversalProductDto>();
            }

            var mapped = MapToUniversalProducts(parsed?.Data?.SupSearchMpn?.Results);
            _logger.LogInformation("Nexar live search returned {Count} normalized offers for {Query}", mapped.Count, normalizedQuery);
            return mapped;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Nexar live search failed for {Query}. Falling back to internal inventory only.", normalizedQuery);
            return new List<UniversalProductDto>();
        }
    }

    private async Task<string?> GetAccessTokenAsync()
    {
        if (_cache.TryGetValue<string>(TokenCacheKey, out var cachedToken) && !string.IsNullOrWhiteSpace(cachedToken))
        {
            return cachedToken;
        }

        var tokenEndpoint = _configuration["NexarApi:TokenEndpoint"] ?? "https://identity.nexar.com/connect/token";
        var clientId = _configuration["NexarApi:ClientId"];
        var clientSecret = _configuration["NexarApi:ClientSecret"];

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            _logger.LogWarning("Nexar credentials are not configured. Skipping live Nexar adapter execution.");
            return null;
        }

        using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
                ["scope"] = "supply.domain"
            })
        };

        using var tokenResponse = await _httpClient.SendAsync(tokenRequest);
        var tokenBody = await tokenResponse.Content.ReadAsStringAsync();
        Console.WriteLine($"[NEXAR] Token Response Status: {tokenResponse.StatusCode}");
        Console.WriteLine($"[NEXAR] Token Response Payload: {tokenBody}");

        if (!tokenResponse.IsSuccessStatusCode)
        {
            _logger.LogError("Nexar token request failed with status {StatusCode}: {Body}", (int)tokenResponse.StatusCode, tokenBody);
            return null;
        }

        var tokenPayload = JsonSerializer.Deserialize<NexarTokenResponse>(tokenBody, JsonOptions);
        if (string.IsNullOrWhiteSpace(tokenPayload?.AccessToken))
        {
            _logger.LogError("Nexar token response did not contain an access token.");
            return null;
        }

        var cacheDurationSeconds = Math.Max(30, tokenPayload.ExpiresIn - 60);
        _cache.Set(TokenCacheKey, tokenPayload.AccessToken, TimeSpan.FromSeconds(cacheDurationSeconds));

        return tokenPayload.AccessToken;
    }

    private static string BuildGraphQlPayload(string query)
    {
        var payload = new
        {
            query = "query Search($q: String!) { supSearchMpn(q: $q, limit: 10) { results { part { mpn name sellers { company { name } offers { inventoryLevel prices { quantity price currency } } } } } } }",
            variables = new
            {
                q = query
            }
        };

        return JsonSerializer.Serialize(payload);
    }

    private static List<UniversalProductDto> MapToUniversalProducts(List<NexarResult>? results)
    {
        if (results is null || results.Count == 0)
        {
            return new List<UniversalProductDto>();
        }

        var mapped = new List<UniversalProductDto>();
        foreach (var result in results)
        {
            if (result?.Part is null)
            {
                continue;
            }

            var sellers = result.Part.Sellers ?? new List<NexarSeller>();
            foreach (var seller in sellers)
            {
                if (seller is null)
                {
                    continue;
                }

                var offers = seller.Offers ?? new List<NexarOffer>();
                if (offers.Count == 0)
                {
                    offers = new List<NexarOffer> { new() { InventoryLevel = 0, Prices = new List<NexarPrice>() } };
                }

                foreach (var offer in offers)
                {
                    if (offer is null)
                    {
                        continue;
                    }

                    var publicSupplierName = string.IsNullOrWhiteSpace(seller.Company?.Name)
                    ? "Verified Distributor"
                    : seller.Company.Name;
                    var supplierRealId = publicSupplierName;

                    var tiers = (offer.Prices ?? new List<NexarPrice>())
                        .Where(p => p is not null)
                        .OrderBy(p => p.Quantity)
                        .Select(p => new PriceTierDto
                    {
                            Qty = p.Quantity,
                            UnitPrice = ParseDecimal(p.Price)
                        })
                        .ToList();

                    if (tiers.Count == 0)
                    {
                        tiers.Add(new PriceTierDto { Qty = 1, UnitPrice = 0m });
                    }

                    var attributes = new Dictionary<string, string>
                    {
                        ["Source"] = "NexarLive"
                    };

                    mapped.Add(new UniversalProductDto
                    {
                        ItemId = Guid.NewGuid().ToString("N"),
                        Category = "Electronics",
                        Title = string.IsNullOrWhiteSpace(result.Part.Name) ? (result.Part.Mpn ?? "Unknown part") : result.Part.Name,
                        BrandOrManufacturer = "Unknown",
                        SKU = result.Part.Mpn ?? string.Empty,
                        Description = result.Part.Name ?? result.Part.Mpn ?? string.Empty,
                        PublicSupplierName = publicSupplierName,
                        SupplierRealId = supplierRealId,
                        AvailableStock = offer.InventoryLevel ?? 0,
                        AvailabilityStatus = (offer.InventoryLevel ?? 0) > 0 ? "In Stock - Ships in 24h" : "Backorder - Lead time on request",
                        Currency = offer.Prices?.FirstOrDefault()?.Currency ?? "USD",
                        PriceBreaks = tiers,
                        Attributes = attributes
                    });
                }
            }
        }

        return mapped;
    }

    private static decimal ParseDecimal(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Number)
        {
            return value.TryGetDecimal(out var numeric)
                ? numeric
                : Convert.ToDecimal(value.GetDouble(), CultureInfo.InvariantCulture);
        }

        if (value.ValueKind == JsonValueKind.String)
        {
            var text = value.GetString();
            if (string.IsNullOrWhiteSpace(text))
            {
                return 0m;
            }

            return decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedText)
                ? parsedText
                : 0m;
        }

        return 0m;
    }

    private sealed class NexarTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }

    private sealed class NexarGraphQlResponse
    {
        public NexarData? Data { get; set; }
        public List<NexarError>? Errors { get; set; }
    }

    private sealed class NexarData
    {
        public NexarSupSearchMpn? SupSearchMpn { get; set; }
    }

    private sealed class NexarSupSearchMpn
    {
        public List<NexarResult>? Results { get; set; }
    }

    private sealed class NexarResult
    {
        public NexarPart? Part { get; set; }
        public List<NexarOffer>? Offers { get; set; }
    }

    private sealed class NexarPart
    {
        public string? Mpn { get; set; }
        public string? Name { get; set; }
        public List<NexarSeller>? Sellers { get; set; }
    }

    private sealed class NexarSeller
    {
        public NexarCompany? Company { get; set; }
        public List<NexarOffer>? Offers { get; set; }
    }

    private sealed class NexarOffer
    {
        public int? InventoryLevel { get; set; }
        public List<NexarPrice>? Prices { get; set; }
    }

    private sealed class NexarPrice
    {
        public int Quantity { get; set; }
        public JsonElement Price { get; set; }
        public string? Currency { get; set; }
    }

    private sealed class NexarCompany
    {
        public string? Name { get; set; }
    }

    private sealed class NexarError
    {
        public string? Message { get; set; }
    }
}
