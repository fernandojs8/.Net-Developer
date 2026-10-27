using CasaDoCodigo.Models;
using CasaDoCodigo.Models.ViewModels;
using CasaDoCodigo.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace CasaDoCodigo.Controllers;
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class PedidoController(IProdutoRepository produtos, IPedidoRepository pedidos) : Controller
{
    [HttpGet] public async Task<IActionResult> Carrossel() => View(await produtos.GetProdutosAsync());
    [HttpGet] public async Task<IActionResult> Carrinho() => View(new CarrinhoViewModel((await pedidos.GetPedidoAsync()).Items));
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Adicionar(string codigo)
    {
        try { await pedidos.AddItemAsync(codigo); return RedirectToAction(nameof(Carrinho)); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (DbUpdateException) { return Conflict("Carrinho alterado simultaneamente. Atualize a página."); }
    }
    [HttpGet] public async Task<IActionResult> Cadastro()
    {
        var pedido = await pedidos.GetPedidoAsync();
        if (pedido.Items.Count == 0) return RedirectToAction(nameof(Carrinho));
        return View(CadastroInput.From(pedido.Cadastro));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Resumo(CadastroInput cadastro)
    {
        if (!ModelState.IsValid) return View("Cadastro", cadastro);
        try { return View(await pedidos.UpdateCadastroAsync(cadastro)); }
        catch (ArgumentException ex) { ModelState.AddModelError("", ex.Message); return View("Cadastro", cadastro); }
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateQuantidadePedido([FromBody] QuantidadeInput input)
    {
        if (input == null || !ModelState.IsValid) return BadRequest(ModelState);
        try { return Ok(await pedidos.UpdateQuantidadeAsync(input)); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (DbUpdateConcurrencyException) { return Conflict("Item alterado em outra requisição. Atualize o carrinho."); }
    }
}
