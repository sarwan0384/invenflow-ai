using System.Net.Http.Headers;
using System.Text.Json;
using HtmlAgilityPack;
using InvenFlow.Api.Services;
using InvenFlow.Core.Entities;
using InvenFlow.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InvenFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SyncController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly GeminiInvoiceService _aiService;
    private readonly ILogger<SyncController> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SyncController(
        AppDbContext context,
        IHttpClientFactory httpClientFactory,
        GeminiInvoiceService aiService,
        ILogger<SyncController> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _aiService = aiService;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    [HttpPost("external-url")]
    public async Task<IActionResult> SyncExternalUrl([FromBody] SyncExternalUrlRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Url))
        {
            return BadRequest(new { message = "A valid URL is required for external sync." });
        }

        if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var targetUri))
        {
            return BadRequest(new { message = "The provided URL is invalid." });
        }

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.UserAgent.Clear();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("InvenFlowSync/1.0");
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));

        HttpResponseMessage response;
        try
        {
            response = await client.GetAsync(targetUri);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch external URL {Url}", request.Url);
            return BadRequest(new { message = "Unable to fetch the provided URL." });
        }

        if (!response.IsSuccessStatusCode)
        {
            return StatusCode((int)response.StatusCode, new { message = "The external URL returned a non-success status." });
        }

        var contentType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
        var body = await response.Content.ReadAsStringAsync();
        var sourceType = "API";
        var products = new List<ExtractedProduct>();

        if (contentType.Contains("json", StringComparison.OrdinalIgnoreCase) || LooksLikeJson(body))
        {
            products = ParseJsonProducts(body);
            sourceType = "API";
        }
        else if (contentType.Contains("html", StringComparison.OrdinalIgnoreCase))
        {
            products = ParseHtmlTables(body);
            if (products.Count > 0)
            {
                sourceType = "Web";
            }
            else
            {
                products = await _aiService.ExtractProductsFromHtmlAsync(body, request.Url);
                sourceType = "AI";
            }
        }
        else
        {
            if (body.Contains("<html", StringComparison.OrdinalIgnoreCase))
            {
                products = ParseHtmlTables(body);
                if (products.Count > 0)
                {
                    sourceType = "Web";
                }
                else
                {
                    products = await _aiService.ExtractProductsFromHtmlAsync(body, request.Url);
                    sourceType = "AI";
                }
            }
            else if (LooksLikeJson(body))
            {
                products = ParseJsonProducts(body);
                sourceType = "API";
            }
            else
            {
                products = await _aiService.ExtractProductsFromHtmlAsync(body, request.Url);
                sourceType = "AI";
            }
        }

        if (products.Count == 0)
        {
            return BadRequest(new { message = "No inventory products could be extracted from the provided source." });
        }

        var (added, updated) = await ApplySyncResultsAsync(products);
        return Ok(new SyncExternalUrlResponse
        {
            Added = added,
            Updated = updated,
            SourceType = sourceType,
        });
    }

    private static bool LooksLikeJson(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return false;
        }

        var trimmed = content.TrimStart();
        return trimmed.StartsWith('{') || trimmed.StartsWith('[');
    }

    private static List<ExtractedProduct> ParseJsonProducts(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind == JsonValueKind.Object)
            {
                if (TryFindArray(root, out var array))
                {
                    root = array;
                }
            }

            if (root.ValueKind != JsonValueKind.Array)
            {
                return new List<ExtractedProduct>();
            }

            var products = new List<ExtractedProduct>();
            foreach (var element in root.EnumerateArray())
            {
                products.Add(ParseJsonProduct(element));
            }

            return products.Where(p => !string.IsNullOrWhiteSpace(p.Name) || !string.IsNullOrWhiteSpace(p.Sku)).ToList();
        }
        catch (JsonException)
        {
            return new List<ExtractedProduct>();
        }
    }

    private static bool TryFindArray(JsonElement root, out JsonElement arrayElement)
    {
        if (root.TryGetProperty("items", out arrayElement) || root.TryGetProperty("products", out arrayElement) || root.TryGetProperty("data", out arrayElement))
        {
            return arrayElement.ValueKind == JsonValueKind.Array;
        }

        arrayElement = default;
        return false;
    }

    private static ExtractedProduct ParseJsonProduct(JsonElement element)
    {
        var product = new ExtractedProduct
        {
            Sku = GetStringValue(element, "sku", "SKU", "id", "productId"),
            Name = GetStringValue(element, "name", "productName", "title"),
            Category = GetStringValue(element, "category", "type", "group"),
            QuantityOnHand = GetIntValue(element, "quantityOnHand", "quantity", "qty", "stock"),
            UnitPrice = GetDecimalValue(element, "unitPrice", "price", "unit_price", "listPrice"),
        };

        return product;
    }

    private static string GetStringValue(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
            {
                return value.GetString()?.Trim() ?? string.Empty;
            }

            if (element.TryGetProperty(name, out value) && value.ValueKind is JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False)
            {
                return value.ToString()?.Trim() ?? string.Empty;
            }
        }

        return string.Empty;
    }

    private static int? GetIntValue(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (!element.TryGetProperty(name, out var value))
            {
                continue;
            }

            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var intValue))
            {
                return intValue;
            }

            if (value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), out var parsed))
            {
                return parsed;
            }
        }

        return null;
    }

    private static decimal? GetDecimalValue(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (!element.TryGetProperty(name, out var value))
            {
                continue;
            }

            if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var decimalValue))
            {
                return decimalValue;
            }

            if (value.ValueKind == JsonValueKind.String && decimal.TryParse(value.GetString(), out var parsed))
            {
                return parsed;
            }
        }

        return null;
    }

    private static List<ExtractedProduct> ParseHtmlTables(string html)
    {
        var document = new HtmlDocument();
        document.LoadHtml(html);
        var tables = document.DocumentNode.SelectNodes("//table");
        if (tables == null)
        {
            return new List<ExtractedProduct>();
        }

        var products = new List<ExtractedProduct>();
        foreach (var table in tables)
        {
            var headerRow = table.SelectSingleNode(".//tr[th]") ?? table.SelectSingleNode(".//thead/tr") ?? table.SelectSingleNode(".//tr");
            if (headerRow == null)
            {
                continue;
            }

            var headerCells = headerRow.SelectNodes(".//th|.//td")?.Select(node => node.InnerText.Trim().ToLowerInvariant()).ToList() ?? new List<string>();
            if (headerCells.Count == 0)
            {
                continue;
            }

            var rows = table.SelectNodes(".//tr");
            if (rows == null)
            {
                continue;
            }

            foreach (var row in rows.Skip(1))
            {
                var cells = row.SelectNodes(".//td");
                if (cells == null || cells.Count != headerCells.Count)
                {
                    continue;
                }

                var product = new ExtractedProduct();
                for (var index = 0; index < headerCells.Count; index++)
                {
                    var columnText = cells[index].InnerText.Trim();
                    var headerText = headerCells[index];

                    if (headerText.Contains("sku") || headerText.Contains("product code") || headerText.Contains("item code") || headerText.Contains("id"))
                    {
                        product.Sku = columnText;
                        continue;
                    }

                    if (headerText.Contains("name") || headerText.Contains("product") || headerText.Contains("title"))
                    {
                        product.Name = columnText;
                        continue;
                    }

                    if (headerText.Contains("category") || headerText.Contains("type") || headerText.Contains("group"))
                    {
                        product.Category = columnText;
                        continue;
                    }

                    if (headerText.Contains("quantity") || headerText.Contains("qty") || headerText.Contains("stock"))
                    {
                        product.QuantityOnHand = ParseInt(columnText) ?? product.QuantityOnHand;
                        continue;
                    }

                    if (headerText.Contains("price") || headerText.Contains("unit price") || headerText.Contains("amount"))
                    {
                        product.UnitPrice = ParseDecimal(columnText) ?? product.UnitPrice;
                        continue;
                    }
                }

                if (!string.IsNullOrWhiteSpace(product.Name) || !string.IsNullOrWhiteSpace(product.Sku))
                {
                    products.Add(product);
                }
            }
        }

        return products;
    }

    private static int? ParseInt(string value)
    {
        if (int.TryParse(value.Replace(",", string.Empty), out var result))
        {
            return result;
        }

        return null;
    }

    private static decimal? ParseDecimal(string value)
    {
        var cleaned = value.Replace("$", string.Empty).Replace(",", string.Empty).Trim();
        if (decimal.TryParse(cleaned, out var result))
        {
            return result;
        }

        return null;
    }

    private async Task<(int Added, int Updated)> ApplySyncResultsAsync(List<ExtractedProduct> products)
    {
        var tenantId = _httpContextAccessor.HttpContext?.User.FindFirst("tenantId")?.Value;
        if (string.IsNullOrWhiteSpace(tenantId) || !Guid.TryParse(tenantId, out var parsedTenantId))
        {
            return (0, 0);
        }

        var existingItems = await _context.InventoryItems.ToListAsync();
        var updated = 0;
        var added = 0;

        foreach (var product in products)
        {
            var existing = FindExistingInventoryItem(existingItems, product);
            if (existing != null)
            {
                var hasQuantity = product.QuantityOnHand.HasValue;
                var hasPrice = product.UnitPrice.HasValue;
                var shouldUpdate = (hasQuantity && product.QuantityOnHand.Value != existing.QuantityOnHand)
                    || (hasPrice && product.UnitPrice.Value != existing.UnitPrice);

                if (shouldUpdate)
                {
                    existing.QuantityOnHand = product.QuantityOnHand ?? existing.QuantityOnHand;
                    existing.UnitPrice = product.UnitPrice ?? existing.UnitPrice;
                    existing.Name = string.IsNullOrWhiteSpace(product.Name) ? existing.Name : product.Name;
                    existing.Category = string.IsNullOrWhiteSpace(product.Category) ? existing.Category : product.Category;
                    existing.UpdatedAt = DateTime.UtcNow;
                    updated++;
                }
            }
            else
            {
                _context.InventoryItems.Add(new InventoryItem
                {
                    Id = Guid.NewGuid(),
                    TenantId = parsedTenantId,
                    Sku = product.Sku ?? string.Empty,
                    Name = product.Name ?? string.Empty,
                    Category = product.Category ?? string.Empty,
                    QuantityOnHand = product.QuantityOnHand ?? 0,
                    UnitPrice = product.UnitPrice ?? 0m,
                    UpdatedAt = DateTime.UtcNow,
                });

                added++;
            }
        }

        if (added > 0 || updated > 0)
        {
            await _context.SaveChangesAsync();
        }

        return (added, updated);
    }

    private static InventoryItem? FindExistingInventoryItem(List<InventoryItem> existingItems, ExtractedProduct product)
    {
        if (!string.IsNullOrWhiteSpace(product.Sku))
        {
            var sku = product.Sku.Trim();
            var existing = existingItems.FirstOrDefault(item => string.Equals(item.Sku.Trim(), sku, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                return existing;
            }
        }

        if (!string.IsNullOrWhiteSpace(product.Name))
        {
            var name = product.Name.Trim();
            return existingItems.FirstOrDefault(item => string.Equals(item.Name.Trim(), name, StringComparison.OrdinalIgnoreCase));
        }

        return null;
    }
}

public class SyncExternalUrlRequest
{
    public string Url { get; set; } = string.Empty;
}

public class SyncExternalUrlResponse
{
    public int Updated { get; set; }
    public int Added { get; set; }
    public string SourceType { get; set; } = string.Empty;
}