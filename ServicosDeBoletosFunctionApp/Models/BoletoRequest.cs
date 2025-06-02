using System.ComponentModel.DataAnnotations;

namespace ServicosDeBoletos.Models
{
    public class BoletoRequest
    {
        [Required(ErrorMessage = "O CPF/CNPJ do sacado é obrigatório.")]
        [StringLength(18, MinimumLength = 11, ErrorMessage = "CPF/CNPJ inválido.")]
        public string CpfCnpjSacado { get; set; } = string.Empty;

        [Required(ErrorMessage = "O nome do sacado é obrigatório.")]
        [StringLength(100, ErrorMessage = "O nome do sacado deve ter no máximo 100 caracteres.")]
        public string NomeSacado { get; set; } = string.Empty;

        [Required(ErrorMessage = "O valor do boleto é obrigatório.")]
        [Range(0.01, 9999999.99, ErrorMessage = "O valor do boleto deve ser positivo.")]
        public decimal Valor { get; set; }

        [Required(ErrorMessage = "A data de vencimento é obrigatória.")]
        public DateTime DataVencimento { get; set; }

        public string? DescricaoServico { get; set; }
    }
}