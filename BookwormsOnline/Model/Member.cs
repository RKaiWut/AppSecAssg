using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;

namespace BookwormsOnline.Model
{
    public class Member : IdentityUser
    {
        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Required]
        [Display(Name = "Credit Card No")]
        [DataType(DataType.CreditCard)]
        public string CreditCardNo { get; set; } // Encrypted and validated in controller

        [Required]
        [Display(Name = "Mobile No")]
        [Phone]
        public string MobileNo { get; set; }

        [Required]
        [Display(Name = "Billing Address")]
        public string BillingAddress { get; set; }

        [Required]
        [Display(Name = "Shipping Address")]
        public string ShippingAddress { get; set; }  // Allows all special characters by default

        [Required]
        [Display(Name = "Photo (.JPG only)")]
        public string Photo { get; set; }

        // Session management for single device login
        public string? CurrentSessionId { get; set; }
        public DateTime? LastLoginDate { get; set; }

        // Password age tracking
        public DateTime? LastPasswordChangedDate { get; set; }
        public DateTime? PasswordExpiryDate { get; set; }
    }
}
