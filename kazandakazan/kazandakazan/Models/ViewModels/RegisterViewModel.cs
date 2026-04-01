using System.ComponentModel.DataAnnotations;

namespace kazandakazan.Models.ViewModels;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Kullanıcı adı gerekli.")]
    [Display(Name = "Kullanıcı adı")]
    [StringLength(64, MinimumLength = 2)]
    public string UserName { get; set; } = "";

    [Required(ErrorMessage = "E-posta gerekli.")]
    [EmailAddress]
    [Display(Name = "E-posta")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Şifre gerekli.")]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "En az 8 karakter.")]
    public string Password { get; set; } = "";

    [DataType(DataType.Password)]
    [Display(Name = "Şifre tekrar")]
    [Compare(nameof(Password), ErrorMessage = "Şifreler eşleşmiyor.")]
    public string ConfirmPassword { get; set; } = "";

    [Display(Name = "Telefon")]
    [StringLength(32)]
    public string? PhoneNumber { get; set; }

    [Display(Name = "Koşullar")]
    public bool AcceptTerms { get; set; }
}
