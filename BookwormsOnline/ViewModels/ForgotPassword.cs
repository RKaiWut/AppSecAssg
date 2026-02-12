using System.ComponentModel.DataAnnotations;

namespace BookwormsOnline.ViewModels
{
    public class ForgotPassword
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [Display(Name = "Email Address")]
        public string Email { get; set; }
    }
}
