using CasaDoCodigo.Models;
using CasaDoCodigo.Models.ViewModels;
using Xunit;
namespace CasaDoCodigo.Tests;
public class CartTests
{
    [Theory][InlineData(-1)][InlineData(100)][InlineData(int.MaxValue)]
    public void Quantity_rejects_values_outside_bounds(int quantity)
        => Assert.Throws<ArgumentOutOfRangeException>(() => new ItemPedido().AtualizarQuantidade(quantity));
    [Theory][InlineData(0)][InlineData(1)][InlineData(99)]
    public void Quantity_accepts_zero_and_valid_bounds(int quantity)
    {
        var item = new ItemPedido(); item.AtualizarQuantidade(quantity); Assert.Equal(quantity, item.Quantidade);
    }
    [Fact] public void Subtotal_and_total_use_decimal_arithmetic()
    {
        var items = new List<ItemPedido> { new(null, null, 3, 10.10m), new(null, null, 2, 5.25m) };
        Assert.Equal(30.30m, items[0].subTotal);
        Assert.Equal(40.80m, new CarrinhoViewModel(items).Total);
    }
    [Fact] public void Empty_cart_total_is_zero() => Assert.Equal(0, new CarrinhoViewModel(new List<ItemPedido>()).Total);
}
