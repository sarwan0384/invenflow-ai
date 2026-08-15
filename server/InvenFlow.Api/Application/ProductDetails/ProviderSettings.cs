namespace InvenFlow.Api.Application.ProductDetails;

public class ProviderSettings
{
    public string StrategyMode { get; set; } = "FallbackChain";
    public string TargetProvider { get; set; } = string.Empty;
    public List<string> FallbackSequence { get; set; } = new();
    public List<string> ActiveParallelProviders { get; set; } = new();
}