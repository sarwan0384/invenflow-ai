namespace InvenFlow.Core.Entities;

public interface ITenantOwned
{
    Guid TenantId { get; set; }
}
