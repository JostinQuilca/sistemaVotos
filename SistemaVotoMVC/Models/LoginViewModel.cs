using System.ComponentModel.DataAnnotations;
namespace SistemaVotoMVC.Models
{
    public class LoginViewModel
    {
        [Required]
        [StringLength(10)]
        public string Cedula { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}
