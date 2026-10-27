using CasaDoCodigo.Models;
using Microsoft.EntityFrameworkCore;
namespace CasaDoCodigo.Repositories;
public class PedidoRepository(ApplicationContext db, IHttpContextAccessor accessor) : IPedidoRepository
{
    private ISession Session => accessor.HttpContext.Session;
    private IQueryable<Pedido> Pedidos => db.Set<Pedido>().Include(x => x.Items).ThenInclude(x => x.Produto).Include(x => x.Cadastro);
    public async Task<Pedido> GetPedidoAsync()
    {
        await Session.LoadAsync();
        var id = Session.GetInt32("pedidoId");
        var pedido = id.HasValue ? await Pedidos.SingleOrDefaultAsync(x => x.Id == id) : null;
        if (pedido != null) return pedido;
        pedido = new Pedido(); db.Add(pedido); await db.SaveChangesAsync();
        Session.SetInt32("pedidoId", pedido.Id);
        return pedido;
    }
    public async Task AddItemAsync(string codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo)) throw new ArgumentException("Código obrigatório.");
        var produto = await db.Set<Produto>().SingleOrDefaultAsync(x => x.Codigo == codigo)
            ?? throw new KeyNotFoundException("Produto não encontrado.");
        var pedido = await GetPedidoAsync();
        if (pedido.Items.Any(x => x.Produto.Id == produto.Id)) return;
        db.Add(new ItemPedido(pedido, produto, 1, produto.Preco));
        await db.SaveChangesAsync();
    }
    public async Task<object> UpdateQuantidadeAsync(QuantidadeInput input)
    {
        ItemPedido.ValidarQuantidade(input.Quantidade);
        await Session.LoadAsync();
        var pedidoId = Session.GetInt32("pedidoId");
        if (!pedidoId.HasValue) throw new KeyNotFoundException("Item não encontrado.");
        var item = await db.Set<ItemPedido>().SingleOrDefaultAsync(x => x.Id == input.Id && x.Pedido.Id == pedidoId)
            ?? throw new KeyNotFoundException("Item não encontrado.");
        byte[] version;
        try { version = Convert.FromBase64String(input.RowVersion ?? ""); }
        catch (FormatException) { throw new ArgumentException("Versão do item inválida."); }
        if (version.Length != 8) throw new ArgumentException("Versão do item inválida.");
        db.Entry(item).Property(x => x.RowVersion).OriginalValue = version;
        item.AtualizarQuantidade(input.Quantidade);
        if (input.Quantidade == 0) db.Remove(item);
        await db.SaveChangesAsync();
        var items = await db.Set<ItemPedido>().Where(x => x.Pedido.Id == pedidoId).AsNoTracking().ToListAsync();
        return new { itemPedido = new { item.Id, item.Quantidade, item.subTotal, RowVersion = Convert.ToBase64String(item.RowVersion) },
            carrinhoViewModel = new { items = items.Select(x => new { x.Id }), total = items.Sum(x => x.subTotal) } };
    }
    public async Task<Pedido> UpdateCadastroAsync(CadastroInput input)
    {
        var pedido = await GetPedidoAsync();
        if (pedido.Items.Count == 0) throw new ArgumentException("Adicione um produto antes de continuar.");
        pedido.Cadastro.Update(input.ToCadastro()); await db.SaveChangesAsync(); return pedido;
    }
}
