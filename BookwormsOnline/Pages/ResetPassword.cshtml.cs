using BookwormsOnline.Model;
using BookwormsOnline.Services;
using BookwormsOnline.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BookwormsOnline.Pages
{
    public class ResetPasswordModel : PageModel
    {
        private readonly UserManager<Member> _userManager;
        private readonly IAuditLogService _auditLogService;
        private readonly BookwormsDbContext _context;

        [BindProperty]
        public ResetPassword Model { get; set; }

        private const int PasswordHistoryCount = 2;
        private const int MaxPasswordAgeDays = 90;

        public ResetPasswordModel(
            UserManager<Member> userManager,
            IAuditLogService auditLogService,
            BookwormsDbContext context)
        {
            _userManager = userManager;
            _auditLogService = auditLogService;
            _context = context;
        }

        public IActionResult OnGet(string userId, string token)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
            {
                return RedirectToPage("./Login");
            }

            Model = new ResetPassword
            {
                UserId = userId,
                Token = token
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.FindByIdAsync(Model.UserId);
            if (user == null)
            {
                await _auditLogService.LogAsync("", "", "PASSWORD_RESET_ATTEMPT",
                    "Invalid user ID", false);
                return RedirectToPage("./ResetPasswordConfirmation");
            }

            // Check password history (prevent reuse)
            var passwordHasher = new PasswordHasher<Member>();
            var recentPasswords = _context.PasswordHistories
                .Where(ph => ph.UserId == user.Id)
                .OrderByDescending(ph => ph.CreatedDate)
                .Take(PasswordHistoryCount)
                .ToList();

            foreach (var oldPassword in recentPasswords)
            {
                var verificationResult = passwordHasher.VerifyHashedPassword(user, oldPassword.PasswordHash, Model.NewPassword);
                if (verificationResult == PasswordVerificationResult.Success)
                {
                    ModelState.AddModelError(string.Empty,
                        $"You cannot reuse your last {PasswordHistoryCount} passwords. Please choose a different password.");
                    await _auditLogService.LogAsync(user.Id, user.Email, "PASSWORD_RESET_ATTEMPT",
                        "Password reuse detected", false);
                    return Page();
                }
            }

            // Reset password
            var result = await _userManager.ResetPasswordAsync(user, Model.Token, Model.NewPassword);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                await _auditLogService.LogAsync(user.Id, user.Email, "PASSWORD_RESET_ATTEMPT",
                    "Invalid token or password", false);
                return Page();
            }

            // Store old password in history (if exists)
            if (!string.IsNullOrEmpty(user.PasswordHash))
            {
                var passwordHistory = new PasswordHistory
                {
                    UserId = user.Id,
                    PasswordHash = user.PasswordHash,
                    CreatedDate = DateTime.UtcNow
                };
                _context.PasswordHistories.Add(passwordHistory);
            }

            // Update password age tracking
            user.LastPasswordChangedDate = DateTime.UtcNow;
            user.PasswordExpiryDate = DateTime.UtcNow.AddDays(MaxPasswordAgeDays);
            
            // Clear lockout (automatic account recovery)
            await _userManager.SetLockoutEndDateAsync(user, null);
            await _userManager.ResetAccessFailedCountAsync(user);

            await _userManager.UpdateAsync(user);
            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(user.Id, user.Email, "PASSWORD_RESET_SUCCESS",
                "Password reset successfully", true);

            return RedirectToPage("./ResetPasswordConfirmation");
        }
    }
}
