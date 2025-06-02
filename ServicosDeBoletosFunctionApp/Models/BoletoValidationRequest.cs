using System.ComponentModel.DataAnnotations;

namespace ServicosDeBoletos.Models
{
    public class BoletoValidationRequest
    {
        [Required(ErrorMessage = "A linha digitável do boleto é obrigatória.")]
        [StringLength(48, MinimumLength = 47, ErrorMessage = "A linha digitável deve ter 47 ou 48 caracteres.")]
        [RegularExpression("^[0-9]*$", ErrorMessage = "A linha digitável deve conter apenas números.")]
        public string LinhaDigitavel { get; set; } = string.Empty;
        public decimal? ValorEsperado { get; set; }
        public DateTime? DataVencimentoEsperada { get; set; }
    }
}