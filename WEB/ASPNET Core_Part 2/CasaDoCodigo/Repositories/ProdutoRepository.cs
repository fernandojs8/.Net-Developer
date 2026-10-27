using CasaDoCodigo.Models;
using Microsoft.EntityFrameworkCore;
namespace CasaDoCodigo.Repositories;
public class ProdutoRepository(ApplicationContext db) : IProdutoRepository
{
    public Task<List<Produto>> GetProdutosAsync() => db.Set<Produto>().AsNoTracking().OrderBy(x => x.Id).ToListAsync();
    public async Task SaveProdutosAsync(List<Livro> livros)
    {
        foreach (var livro in livros)
        {
            if (!await db.Set<Produto>().AnyAsync(x => x.Codigo == livro.Codigo))
                db.Add(new Produto(livro.Codigo, livro.Nome, livro.Preco));
        }
        await db.SaveChangesAsync();
    }
}
