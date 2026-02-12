using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace BookwormsOnline.ViewModels
{
    public class Register
    {
        [Required(ErrorMessage = "First name is required.")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last name is required.")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Credit card number is required.")]
        [Display(Name = "Credit Card No")]
        [DataType(DataType.CreditCard)]
        [RegularExpression(@"^[0-9]{13,19}$", ErrorMessage = "Credit card must be 13-19 digits.")]
        public string CreditCardNo { get; set; }

        [Required(ErrorMessage = "Mobile number is required.")]
        [Display(Name = "Mobile No")]
        [Phone(ErrorMessage = "Phone number format is invalid.")]
        public string MobileNo { get; set; }

        [Required(ErrorMessage = "Billing address is required.")]
        [Display(Name = "Billing Address")]
        public string BillingAddress { get; set; }

        [Required(ErrorMessage = "Shipping address is required.")]
        [Display(Name = "Shipping Address")]
        public string ShippingAddress { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [Display(Name = "Email Address")]
        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        [MinLength(12, ErrorMessage = "Password must be at least 12 characters long.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{12,}$", 
            ErrorMessage = "Password must contain uppercase, lowercase, digit, and special character.")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Confirm password is required.")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Password and Confirmation Password do not match.")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Photo is required.")]
        [Display(Name = "Photo (.JPG only)")]
        public IFormFile Photo { get; set; }
    }
}
