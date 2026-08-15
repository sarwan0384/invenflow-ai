using InvenFlow.Api.Application.ProductDetails;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InvenFlow.Api.Controllers;

[ApiController]
[Route("api/v1/products")]
public class ProductsController : ControllerBase
{
    private readonly IVendorDetailsService _vendorDetailsService;

    public ProductsController(IVendorDetailsService vendorDetailsService)
    {
        _vendorDetailsService = vendorDetailsService;
    }

    [HttpGet("details")]
    [AllowAnonymous]
    public async Task<IActionResult> GetDetails(
        [FromQuery] string? supplierRealId,
        [FromQuery] string? mpn,
        [FromQuery] string? vendorKey,
        [FromQuery] string? strategy,
        [FromQuery] string? preferredProvider,
        CancellationToken cancellationToken)
    {
        var decodedSupplierRealId = DecodeQueryValue(supplierRealId);
        var decodedMpn = DecodeQueryValue(mpn);
        var decodedVendorKey = DecodeQueryValue(vendorKey);
        var decodedPreferredProvider = DecodeQueryValue(preferredProvider);

        if (string.IsNullOrWhiteSpace(decodedMpn))
        {
            return BadRequest(new { message = "mpn is required." });
        }

        if (!TryParseStrategy(strategy, out var parsedStrategy))
        {
            return BadRequest(new { message = "strategy must be one of DirectTargeted, Parallel, or FallbackChain." });
        }

        var details = await _vendorDetailsService.GetDetailsAsync(
            decodedSupplierRealId,
            decodedMpn,
            decodedVendorKey,
            parsedStrategy,
            decodedPreferredProvider,
            cancellationToken);
        return Ok(details);
    }

    private static string DecodeQueryValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Trim();

        try
        {
            return Uri.UnescapeDataString(normalized);
        }
        catch (UriFormatException)
        {
            return normalized;
        }
    }

    private static bool TryParseStrategy(string? value, out VendorSelectionStrategy? strategy)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            strategy = null;
            return true;
        }

        var normalized = value.Trim().Replace("-", string.Empty, StringComparison.Ordinal).Replace("_", string.Empty, StringComparison.Ordinal);
        if (Enum.TryParse<VendorSelectionStrategy>(normalized, ignoreCase: true, out var parsed))
        {
            strategy = parsed;
            return true;
        }

        strategy = null;
        return false;
    }
}
