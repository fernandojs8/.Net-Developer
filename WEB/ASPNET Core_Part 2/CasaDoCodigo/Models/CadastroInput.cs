using System.ComponentModel.DataAnnotations;
namespace CasaDoCodigo.Models;
public class CadastroInput
{
        [MinLength(5, ErrorMessage = "Nome deve ter no mínimo 5 caracteres.")]
        [MaxLength(50, ErrorMessage = "Nome deve ter no máximo 50 caracteres.")]
        [Required(ErrorMessage = "Nome é obrigatório.")]
        public string Nome { get; set; } = "";


        [Required(ErrorMessage = "Email é obrigatório.")]
        [EmailAddress]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Telefone é obrigatório.")]
        public string Telefone { get; set; } = "";

        [Required(ErrorMessage = "Endereco é obrigatório.")]
        public string Endereco { get; set; } = "";

        [Required(ErrorMessage = "Complemento é obrigatório.")]
        public string Complemento { get; set; } = "";

        [Required(ErrorMessage = "Bairro é obrigatório.")]
        public string Bairro { get; set; } = "";

        [Required(ErrorMessage = "Municipio é obrigatório.")]
        public string Municipio { get; set; } = "";

        [Required(ErrorMessage = "UF é obrigatório.")]
        public string UF { get; set; } = "";

        [Required(ErrorMessage = "CEP é obrigatório.")]
        public string CEP { get; set; } = "";

public Cadastro ToCadastro() => new Cadastro { Nome = Nome, Email = Email, Telefone = Telefone, Endereco = Endereco, Complemento = Complemento, Bairro = Bairro, Municipio = Municipio, UF = UF, CEP = CEP };
public static CadastroInput From(Cadastro c) => new CadastroInput { Nome = c.Nome, Email = c.Email, Telefone = c.Telefone, Endereco = c.Endereco, Complemento = c.Complemento, Bairro = c.Bairro, Municipio = c.Municipio, UF = c.UF, CEP = c.CEP };
}
