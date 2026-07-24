namespace InvenFlow.Api.Services;

public interface ICurrentTenantService
{
    Guid? CurrentTenantId { get; set; }
}

public class CurrentTenantService : ICurrentTenantService
{
    public Guid? CurrentTenantId { get; set; }
}
