using System.ComponentModel.DataAnnotations;
namespace CasaDoCodigo.Models;
public class QuantidadeInput
{
    [Range(1, int.MaxValue)] public int Id { get; set; }
    [Range(0, 99)] public int Quantidade { get; set; }
    [Required] public string RowVersion { get; set; }
}
