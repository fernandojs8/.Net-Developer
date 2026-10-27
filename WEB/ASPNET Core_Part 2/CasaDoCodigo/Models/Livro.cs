using System.Text.Json;
namespace CasaDoCodigo.Models;
public class Livro(IWebHostEnvironment environment) : ILivro
{
    public string Codigo { get; set; }
    public string Nome { get; set; }
    public decimal Preco { get; set; }
    public List<Livro> GetLivros()
    {
        // Deserialize a DTO rather than this DI-bound reader.
        var json = File.ReadAllText(Path.Combine(environment.ContentRootPath, "livros.json"));
        return JsonSerializer.Deserialize<List<LivroDados>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            .Select(x => new Livro(environment) { Codigo = x.Codigo, Nome = x.Nome, Preco = x.Preco }).ToList();
    }
    private sealed record LivroDados(string Codigo, string Nome, decimal Preco);
}
