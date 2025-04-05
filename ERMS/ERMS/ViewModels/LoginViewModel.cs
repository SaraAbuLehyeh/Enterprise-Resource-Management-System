// ViewModels/LoginViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace ERMS.ViewModels
{
    /// <summary>
    /// View model for user login.
    /// </summary>
    public class LoginViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }
    }
}
