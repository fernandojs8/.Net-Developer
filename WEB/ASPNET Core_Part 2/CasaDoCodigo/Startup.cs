using CasaDoCodigo.Models;
using CasaDoCodigo.Repositories;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using Azure.Monitor.OpenTelemetry.AspNetCore;
namespace CasaDoCodigo;
public class Startup(IConfiguration configuration)
{
    public void ConfigureServices(IServiceCollection services)
    {
        var connection = configuration.GetConnectionString("Default") ?? "";
        services.AddControllersWithViews();
        var telemetry = services.AddOpenTelemetry().ConfigureResource(resource => resource.AddService("CasaDoCodigo"))
            .WithTracing(trace => trace.AddAspNetCoreInstrumentation().AddSqlClientInstrumentation())
            .WithMetrics(metrics => metrics.AddAspNetCoreInstrumentation());
        var insights = configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
        if (!string.IsNullOrWhiteSpace(insights)) telemetry.UseAzureMonitor(options => options.ConnectionString = insights);
        else if (!string.IsNullOrWhiteSpace(configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
            telemetry.WithTracing(trace => trace.AddOtlpExporter()).WithMetrics(metrics => metrics.AddOtlpExporter());
        services.AddDbContext<ApplicationContext>(options => options.UseSqlServer(connection));
        services.AddHttpContextAccessor();
        services.AddDistributedSqlServerCache(options =>
        {
            options.ConnectionString = connection; options.SchemaName = "dbo"; options.TableName = "SessionCache";
        });
        services.AddDataProtection().SetApplicationName("CasaDoCodigo").PersistKeysToDbContext<ApplicationContext>();
        services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromMinutes(30);
            options.Cookie.Name = ".CasaDoCodigo.Session"; options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true; options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = configuration.GetValue<bool>("LocalHttp") ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
        });
        services.AddAntiforgery(options => options.HeaderName = "RequestVerificationToken");
        services.AddScoped<ILivro, Livro>(); services.AddScoped<IDataService, DataService>();
        services.AddScoped<IPedidoRepository, PedidoRepository>(); services.AddScoped<IProdutoRepository, ProdutoRepository>();
        services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("sql", tags: new[] { "ready" });
        var proxies = configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? Array.Empty<string>();
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            foreach (var proxy in proxies) options.KnownProxies.Add(System.Net.IPAddress.Parse(proxy));
        });
    }
    public void Configure(IApplicationBuilder app, IWebHostEnvironment environment, ILogger<Startup> logger)
    {
        app.UseForwardedHeaders();
        if (environment.IsDevelopment()) app.UseDeveloperExceptionPage();
        else { app.UseExceptionHandler("/Home/Error"); app.UseHsts(); }
        if (!configuration.GetValue<bool>("LocalHttp")) app.UseHttpsRedirection();
        app.Use(async (context, next) =>
        {
            var started = System.Diagnostics.Stopwatch.StartNew();
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["X-Frame-Options"] = "DENY";
            try { await next(); }
            finally { logger.LogInformation("HTTP {Method} {Path} {StatusCode} {ElapsedMs}ms TraceId {TraceId}", context.Request.Method, context.Request.Path.Value, context.Response.StatusCode, started.ElapsedMilliseconds, context.TraceIdentifier); }
        });
        app.UseStaticFiles(); app.UseRouting(); app.UseSession();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllerRoute("default", "{controller=Pedido}/{action=Carrossel}/{codigo?}");
            endpoints.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
            endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });
        });
    }
}
