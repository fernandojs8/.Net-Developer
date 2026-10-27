using CasaDoCodigo.Models;
using CasaDoCodigo.Repositories;
namespace CasaDoCodigo;
public class DataService(IProdutoRepository produtos, ILivro livros) : IDataService
{
    public Task SeedAsync() => produtos.SaveProdutosAsync(livros.GetLivros());
}
