using CasaDoCodigo.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
namespace CasaDoCodigo;
public class ApplicationContext(DbContextOptions<ApplicationContext> options) : DbContext(options), IDataProtectionKeyContext
{
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }
    protected override void OnModelCreating(ModelBuilder model)
    {
        base.OnModelCreating(model);
        model.Entity<Produto>().HasIndex(x => x.Codigo).IsUnique();
        model.Entity<Produto>().Property(x => x.Preco).HasPrecision(18, 2);
        model.Entity<Pedido>().HasMany(x => x.Items).WithOne(x => x.Pedido).HasForeignKey("PedidoId");
        model.Entity<Pedido>().HasOne(x => x.Cadastro).WithOne(x => x.Pedido).HasForeignKey<Pedido>("CadastroId");
        model.Entity<ItemPedido>().HasOne(x => x.Produto).WithMany().HasForeignKey("ProdutoId");
        model.Entity<ItemPedido>().Property(x => x.PrecoUnitario).HasPrecision(18, 2);
        model.Entity<ItemPedido>().Property(x => x.RowVersion).IsRowVersion();
        model.Entity<ItemPedido>().HasIndex("PedidoId", "ProdutoId").IsUnique();
        model.Entity<ItemPedido>().ToTable("ItemPedido", table => table.HasCheckConstraint("CK_ItemPedido_Quantidade", "[Quantidade] >= 0 AND [Quantidade] <= 99"));
    }
}
