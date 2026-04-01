using System.ComponentModel.DataAnnotations;

namespace kazandakazan.Models.ViewModels;

public class LoginViewModel
{
    [Required]
    [EmailAddress]
    [Display(Name = "E-posta")]
    public string Email { get; set; } = "";

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Display(Name = "Beni hatırla")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}
