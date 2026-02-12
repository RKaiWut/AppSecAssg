using System.ComponentModel.DataAnnotations;

namespace BookwormsOnline.ViewModels
{
    public class Login
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        [MinLength(12, ErrorMessage = "Password must be at least 12 characters.")]
        public string Password { get; set; }

        public bool RememberMe { get; set; } = false;
    }
}
