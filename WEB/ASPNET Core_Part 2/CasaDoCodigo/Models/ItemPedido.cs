using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace CasaDoCodigo.Models
{
    [DataContract]
    public class ItemPedido : BaseModel
    {
        [Required]
        [DataMember]
        public Pedido Pedido { get; set; }

        [Required]
        [DataMember]
        public Produto Produto { get; set; }

        [Required]
        [DataMember]
        public int Quantidade { get; set; }

        [Required]
        [DataMember]
        public decimal PrecoUnitario { get; set; }

        [DataMember]
        public decimal subTotal => PrecoUnitario * Quantidade;

        public ItemPedido() { }

        public ItemPedido(Pedido pedido, Produto produto, int quantidade, decimal precoUnitario)
        {
            Pedido = pedido;
            Produto = produto;
            ValidarQuantidade(quantidade);
            Quantidade = quantidade;
            PrecoUnitario = precoUnitario;
        }

        [Timestamp]
        public byte[] RowVersion { get; set; }

        public void AtualizarQuantidade(int quantidade)
        {
            ValidarQuantidade(quantidade);
            Quantidade = quantidade;
        }
        public static void ValidarQuantidade(int quantidade)
        {
            if (quantidade < 0 || quantidade > 99) throw new ArgumentOutOfRangeException(nameof(quantidade), "Quantidade deve estar entre 0 e 99.");
        }
    }
}