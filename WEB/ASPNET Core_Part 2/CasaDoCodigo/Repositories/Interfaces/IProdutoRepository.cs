using CasaDoCodigo.Models;
namespace CasaDoCodigo.Repositories;
public interface IProdutoRepository
{
    Task SaveProdutosAsync(List<Livro> livros);
    Task<List<Produto>> GetProdutosAsync();
}
