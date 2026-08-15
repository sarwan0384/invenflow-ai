using InvenFlow.Api.Application.DTOs;

namespace InvenFlow.Api.Application.ProductDetails;

public interface IVendorDetailsProvider
{
    string VendorKey { get; }
    bool IsEnabled { get; }
    Task<ProductDetailDto> FetchDetailsAsync(string supplierRealId, string mpn, CancellationToken cancellationToken = default);
}
