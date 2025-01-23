using System.ComponentModel.DataAnnotations;

namespace YoklamaAlma.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Kullanıcı adı zorunludur")]
        [Display(Name = "Kullanıcı Adı")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Şifre zorunludur")]
        [DataType(DataType.Password)]
        [Display(Name = "Şifre")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Kullanıcı tipi seçiniz")]
        [Display(Name = "Kullanıcı Tipi")]
        public UserRole UserType { get; set; }
    }
} 