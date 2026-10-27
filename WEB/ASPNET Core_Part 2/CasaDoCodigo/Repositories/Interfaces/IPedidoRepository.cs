using CasaDoCodigo.Models;
namespace CasaDoCodigo.Repositories;
public interface IPedidoRepository
{
    Task<Pedido> GetPedidoAsync();
    Task AddItemAsync(string codigo);
    Task<object> UpdateQuantidadeAsync(QuantidadeInput input);
    Task<Pedido> UpdateCadastroAsync(CadastroInput cadastro);
}
