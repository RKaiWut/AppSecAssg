using BookwormsOnline.Model;
using BookwormsOnline.Services;
using BookwormsOnline.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Encodings.Web;

namespace BookwormsOnline.Pages
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<Member> _userManager;
        private readonly IEmailService _emailService;
        private readonly IAuditLogService _auditLogService;

        [BindProperty]
        public ForgotPassword Model { get; set; }

        public ForgotPasswordModel(
            UserManager<Member> userManager,
            IEmailService emailService,
            IAuditLogService auditLogService)
        {
            _userManager = userManager;
            _emailService = emailService;
            _auditLogService = auditLogService;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.FindByEmailAsync(Model.Email);
            
            // Don't reveal if user exists or not (security best practice)
            // Always show success message
            TempData["Message"] = "If an account exists with that email, a password reset link has been sent.";

            if (user == null)
            {
                await _auditLogService.LogAsync("", Model.Email, "PASSWORD_RESET_REQUEST", 
                    "User not found", false);
                return RedirectToPage("./ForgotPasswordConfirmation");
            }

            // Check if account is locked
            if (await _userManager.IsLockedOutAsync(user))
            {
                await _auditLogService.LogAsync(user.Id, user.Email, "PASSWORD_RESET_REQUEST",
                    "Account locked", false);
                return RedirectToPage("./ForgotPasswordConfirmation");
            }

            // Generate password reset token
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            
            // Create reset link
            var callbackUrl = Url.Page(
                "/ResetPassword",
                pageHandler: null,
                values: new { userId = user.Id, token = token },
                protocol: Request.Scheme);

            // Send email
            try
            {
                await _emailService.SendPasswordResetEmailAsync(user.Email, callbackUrl);
                
                await _auditLogService.LogAsync(user.Id, user.Email, "PASSWORD_RESET_EMAIL_SENT",
                    "Password reset email sent successfully", true);
            }
            catch (Exception ex)
            {
                // Log error but don't reveal to user
                await _auditLogService.LogAsync(user.Id, user.Email, "PASSWORD_RESET_EMAIL_FAILED",
                    $"Failed to send email: {ex.Message}", false);
            }

            return RedirectToPage("./ForgotPasswordConfirmation");
        }
    }
}
