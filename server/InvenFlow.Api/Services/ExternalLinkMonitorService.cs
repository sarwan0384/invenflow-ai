using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using InvenFlow.Core.Entities;
using InvenFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InvenFlow.Api.Services;

public class ExternalLinkMonitorService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExternalLinkMonitorService> _logger;

    public ExternalLinkMonitorService(IServiceScopeFactory scopeFactory, ILogger<ExternalLinkMonitorService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await MonitorAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "External link monitoring cycle failed.");
            }

            await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
        }
    }

    private async Task MonitorAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
        var tenants = await context.Tenants.ToListAsync(stoppingToken);

        foreach (var tenant in tenants)
        {
            var snapshots = await context.Set<ExternalLinkSnapshot>().Where(e => e.TenantId == tenant.Id).ToListAsync(stoppingToken);
            foreach (var snapshot in snapshots)
            {
                var response = await httpClientFactory.CreateClient().GetAsync(snapshot.Url, stoppingToken);
                if (!response.IsSuccessStatusCode)
                {
                    continue;
                }

                var payload = await response.Content.ReadAsStringAsync(stoppingToken);
                var hash = ComputeHash(payload);
                if (string.Equals(hash, snapshot.LastHash, StringComparison.Ordinal))
                {
                    continue;
                }

                var notification = new Notification
                {
                    TenantId = tenant.Id,
                    Title = "External inventory update detected",
                    Message = $"A monitored link changed for {snapshot.Url}.",
                    Category = "ExternalSync",
                    PayloadJson = JsonSerializer.Serialize(new { url = snapshot.Url, payload }),
                    Status = NotificationStatus.PendingSync
                };

                context.Notifications.Add(notification);
                snapshot.LastHash = hash;
                snapshot.LastCheckedAt = DateTime.UtcNow;
                snapshot.LastPayload = payload;
                await context.SaveChangesAsync(stoppingToken);
            }
        }
    }

    private static string ComputeHash(string text)
    {
        using var sha = SHA256.Create();
        return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(text)));
    }
}
