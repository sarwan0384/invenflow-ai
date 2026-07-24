using System.Text.Json;
using Google.GenAI;
using Google.GenAI.Types;

namespace InvenFlow.Api.Services;

public class ExtractedInvoiceData
{
    public string VendorName { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public double ConfidenceScore { get; set; }
    public List<ExtractedLineItem> LineItems { get; set; } = new();
}

public class ExtractedLineItem
{
    public string Sku { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class GeminiInvoiceService
{
    private readonly string _apiKey;

    public GeminiInvoiceService(IConfiguration configuration, ILogger<GeminiInvoiceService> logger)
    {
        var keyFromConfig = configuration["Gemini:ApiKey"];
        var keyFromEnv = configuration["GOOGLE_API_KEY"];
        var apiKey = keyFromConfig ?? keyFromEnv;

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "Gemini API key is missing. Set Gemini:ApiKey in appsettings.Local.json or the Gemini__ApiKey / GOOGLE_API_KEY environment variable.");
        }

        if (!string.IsNullOrWhiteSpace(keyFromConfig))
        {
            logger.LogInformation("Using Gemini API key from configuration path Gemini:ApiKey.");
        }
        else
        {
            logger.LogInformation("Using Gemini API key from environment variable GOOGLE_API_KEY.");
        }

        _apiKey = apiKey;
    }

    public async Task<ExtractedInvoiceData?> ProcessInvoiceAsync(string filePath)
    {
        var client = new Client(apiKey: _apiKey);

        // Explicitly use System.IO.File to prevent namespace collision with Google.GenAI.Types.File
        byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
        string mimeType = filePath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)
            ? "application/pdf"
            : "image/jpeg";

        var promptText = @"
        Analyze this invoice document and extract the following structured JSON details:
        - vendorName: Name of the vendor/supplier
        - invoiceNumber: Invoice or receipt ID/number
        - totalAmount: Grand total price numeric value
        - confidenceScore: Your overall confidence score between 0.0 and 1.0
        - lineItems: Array of items listed containing (sku, itemName, quantity, unitPrice)

        Return ONLY valid raw JSON with no markdown formatting or backticks.
        ";

        var contents = new List<Content>
        {
            new Content
            {
                Role = "user",
                Parts = new List<Part>
                {
                    new Part { Text = promptText },
                    new Part { InlineData = new Blob { MimeType = mimeType, Data = fileBytes } }
                }
            }
        };

        var response = await client.Models.GenerateContentAsync(
            model: "gemini-2.5-flash",
            contents: contents
        );

        string rawJson = response.Text ?? string.Empty;

        // Clean any markdown formatting wrappers returned by LLM
        rawJson = rawJson.Replace("```json", "").Replace("```", "").Trim();

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return JsonSerializer.Deserialize<ExtractedInvoiceData>(rawJson, options);
    }

    private Client CreateClient()
    {
        return new Client(apiKey: _apiKey);
    }

    public async Task<List<ExtractedProduct>> ExtractProductsFromHtmlAsync(string html, string sourceUrl)
    {
        var client = CreateClient();
        var promptText = $@"
Analyze this HTML page from {sourceUrl} and extract all product/inventory items into valid JSON.
Return only a JSON array of objects with these fields:
- sku
- name
- quantityOnHand
- unitPrice
- category
If a field is missing, use an empty string for sku/category and 0 for quantityOnHand/unitPrice.
Example:
[
  {{ ""sku"": ""ABC123"", ""name"": ""Widget"", ""quantityOnHand"": 10, ""unitPrice"": 12.34, ""category"": ""Hardware"" }}
]
Return only JSON, with no markdown fences or extraneous text.
";

        var contents = new List<Content>
        {
            new Content
            {
                Role = "user",
                Parts = new List<Part>
                {
                    new Part { Text = promptText },
                    new Part { Text = html }
                }
            }
        };

        var response = await client.Models.GenerateContentAsync(
            model: "gemini-2.5-flash",
            contents: contents
        );

        string rawJson = response.Text ?? string.Empty;
        rawJson = rawJson.Replace("```json", "").Replace("```", "").Trim();

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        try
        {
            return JsonSerializer.Deserialize<List<ExtractedProduct>>(rawJson, options) ?? new List<ExtractedProduct>();
        }
        catch
        {
            return new List<ExtractedProduct>();
        }
    }
}

public class ExtractedProduct
{
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int? QuantityOnHand { get; set; }
    public decimal? UnitPrice { get; set; }
    public string Category { get; set; } = string.Empty;
}