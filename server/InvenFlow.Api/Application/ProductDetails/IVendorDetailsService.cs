using InvenFlow.Api.Application.DTOs;

namespace InvenFlow.Api.Application.ProductDetails;

public enum VendorSelectionStrategy
{
    DirectTargeted,
    Parallel,
    FallbackChain
}

public interface IVendorDetailsService
{
    Task<ProductDetailDto> GetDetailsAsync(
        string? supplierRealId,
        string? mpn,
        string? vendorKey,
        VendorSelectionStrategy? strategy,
        string? preferredProvider,
        CancellationToken cancellationToken = default);
}
