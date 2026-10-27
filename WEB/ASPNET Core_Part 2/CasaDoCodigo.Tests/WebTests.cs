using CasaDoCodigo.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Xunit;
namespace CasaDoCodigo.Tests;
public class SqlFactAttribute : FactAttribute
{
    public SqlFactAttribute() { if (Environment.GetEnvironmentVariable("RUN_INTEGRATION_TESTS") != "true") Skip = "SQL integration disabled"; }
}
public class SqlFixture : IAsyncLifetime
{
    public WebApplicationFactory<Program> Factory;
    public string Connection;
    public async Task InitializeAsync()
    {
        if (Environment.GetEnvironmentVariable("RUN_INTEGRATION_TESTS") != "true") return;
        var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("TEST_SQL")) { InitialCatalog = "CasaTest_" + Guid.NewGuid().ToString("N") };
        Connection = builder.ConnectionString; Factory = NewFactory();
        using var scope = Factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<ApplicationContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<IDataService>().SeedAsync();
    }
    public WebApplicationFactory<Program> NewFactory() => new WebApplicationFactory<Program>().WithWebHostBuilder(web =>
    {
        web.UseEnvironment("Production");
        web.UseContentRoot(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../CasaDoCodigo")));
        web.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string>
        { ["ConnectionStrings:Default"] = Connection, ["LocalHttp"] = "true" }));
    });
    public HttpClient Client() => Factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    public async Task DisposeAsync()
    {
        if (Factory == null) return;
        using (var scope = Factory.Services.CreateScope()) await scope.ServiceProvider.GetRequiredService<ApplicationContext>().Database.EnsureDeletedAsync();
        await Factory.DisposeAsync();
    }
}
[CollectionDefinition("SQL")] public class SqlCollection : ICollectionFixture<SqlFixture> { }
[Collection("SQL")]
public class WebTests(SqlFixture fixture)
{
    private static string Token(string html) => WebUtility.HtmlDecode(Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
    private static string Version(string html) => WebUtility.HtmlDecode(Regex.Match(html, "item-version=\"([^\"]+)\"").Groups[1].Value);
    private static int ItemId(string html) => int.Parse(Regex.Match(html, "item-id=\"([0-9]+)\"").Groups[1].Value);
    private static async Task<string> Cart(HttpClient client) => await client.GetStringAsync("/Pedido/Carrinho");
    private static async Task Add(HttpClient client, string code = "001")
    {
        var catalog = await client.GetStringAsync("/");
        var response = await client.PostAsync("/Pedido/Adicionar", new FormUrlEncodedContent(new Dictionary<string,string> { ["codigo"] = code, ["__RequestVerificationToken"] = Token(catalog) }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }
    private static Task<HttpResponseMessage> Quantity(HttpClient client, string cart, int id, int quantity, string version)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/Pedido/UpdateQuantidadePedido") { Content = JsonContent.Create(new { Id = id, Quantidade = quantity, RowVersion = version }) };
        request.Headers.Add("RequestVerificationToken", Token(cart)); return client.SendAsync(request);
    }
    [SqlFact] public async Task Antiforgery_and_post_only_add_are_enforced()
    {
        using var client = fixture.Client();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/Pedido/Adicionar", new FormUrlEncodedContent(new Dictionary<string,string> { ["codigo"] = "001" }))).StatusCode);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, (await client.GetAsync("/Pedido/Adicionar?codigo=001")).StatusCode);
        var cart = await Cart(client); Assert.DoesNotContain("item-id=", cart);
        await client.GetAsync("/Pedido/Carrinho?codigo=001"); Assert.DoesNotContain("item-id=", await Cart(client));
    }
    [SqlFact] public async Task Missing_products_and_items_return_not_found()
    {
        using var client = fixture.Client(); var catalog = await client.GetStringAsync("/");
        var missing = await client.PostAsync("/Pedido/Adicionar", new FormUrlEncodedContent(new Dictionary<string,string> { ["codigo"] = "MISSING", ["__RequestVerificationToken"] = Token(catalog) }));
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        var cart = await Cart(client);
        Assert.Equal(HttpStatusCode.NotFound, (await Quantity(client, cart, int.MaxValue, 1, Convert.ToBase64String(new byte[8]))).StatusCode);
    }
    [SqlFact] public async Task Other_session_cannot_update_item_and_stale_writes_conflict()
    {
        using var alice = fixture.Client(); using var bob = fixture.Client(); await Add(alice);
        var cart = await Cart(alice); var id = ItemId(cart); var version = Version(cart); var bobCart = await Cart(bob);
        Assert.Equal(HttpStatusCode.NotFound, (await Quantity(bob, bobCart, id, 5, version)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Quantity(alice, cart, id, 3, version)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Quantity(alice, cart, id, 4, version)).StatusCode);
        cart = await Cart(alice);
        Assert.Equal(HttpStatusCode.BadRequest, (await Quantity(alice, cart, id, -1, Version(cart))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Quantity(alice, cart, id, 100, Version(cart))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await alice.PostAsJsonAsync("/Pedido/UpdateQuantidadePedido", new { Id = id, Quantidade = 2, RowVersion = Version(cart) })).StatusCode);
    }
    [SqlFact] public async Task Full_checkout_preserves_validation_and_removes_zero_quantity()
    {
        using var client = fixture.Client(); await Add(client); await Add(client);
        var cart = await Cart(client); Assert.Single(Regex.Matches(cart, "item-id=\""));
        var cadastro = await client.GetStringAsync("/Pedido/Cadastro");
        var invalid = await client.PostAsync("/Pedido/Resumo", new FormUrlEncodedContent(new Dictionary<string,string> { ["Nome"] = "Ana", ["__RequestVerificationToken"] = Token(cadastro) }));
        Assert.Equal(HttpStatusCode.OK, invalid.StatusCode); var invalidHtml = await invalid.Content.ReadAsStringAsync();
        Assert.Contains("Ana", invalidHtml); Assert.Contains("validation-summary-errors", invalidHtml);
        var fields = new Dictionary<string,string> { ["Nome"]="Pessoa Teste", ["Email"]="test@example.invalid", ["Telefone"]="11999999999", ["Endereco"]="Rua Teste", ["Complemento"]="Teste", ["Bairro"]="Centro", ["Municipio"]="Sao Paulo", ["UF"]="SP", ["CEP"]="01001000", ["__RequestVerificationToken"]=Token(invalidHtml) };
        var summary = await client.PostAsync("/Pedido/Resumo", new FormUrlEncodedContent(fields)); Assert.Equal(HttpStatusCode.OK, summary.StatusCode);
        var html = await summary.Content.ReadAsStringAsync(); Assert.Contains("Resumo do Pedido", html); Assert.Contains("Pessoa Teste", html);
        Assert.Equal(HttpStatusCode.OK, (await Quantity(client, cart, ItemId(cart), 0, Version(cart))).StatusCode);
        Assert.DoesNotContain("item-id=", await Cart(client));
    }
    [SqlFact] public async Task Seed_is_repeatable_and_second_instance_reads_same_session()
    {
        using (var scope = fixture.Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationContext>(); var count = await db.Set<Produto>().CountAsync();
            await scope.ServiceProvider.GetRequiredService<IDataService>().SeedAsync(); Assert.Equal(count, await db.Set<Produto>().CountAsync());
        }
        using var client = fixture.Client();
        var response = await client.GetAsync("/Pedido/Carrinho");
        var cookies = response.Headers.GetValues("Set-Cookie").Select(x => x.Split(';')[0]);
        await Add(client); var cart = await Cart(client);
        await using var otherFactory = fixture.NewFactory();
        using var other = otherFactory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false, AllowAutoRedirect = false });
        other.DefaultRequestHeaders.Add("Cookie", string.Join("; ", cookies));
        var restored = await Cart(other); Assert.Equal(ItemId(cart), ItemId(restored));
    }
    [SqlFact] public async Task Health_and_safe_error_route_are_available()
    {
        using var client = fixture.Client();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/live")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/ready")).StatusCode);
        var error = await client.GetAsync("/Home/Error"); Assert.Equal(HttpStatusCode.InternalServerError, error.StatusCode);
        Assert.DoesNotContain("StackTrace", await error.Content.ReadAsStringAsync());
    }
}
