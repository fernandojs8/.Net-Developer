using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
namespace CasaDoCodigo;
public class DatabaseHealthCheck(ApplicationContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await db.Database.ExecuteSqlRawAsync("SELECT TOP (0) [Value] FROM [dbo].[SessionCache]", cancellationToken);
            await db.Set<Models.Produto>().AnyAsync(cancellationToken);
            await db.DataProtectionKeys.AnyAsync(cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch { return HealthCheckResult.Unhealthy("Database/schema not ready"); }
    }
}
