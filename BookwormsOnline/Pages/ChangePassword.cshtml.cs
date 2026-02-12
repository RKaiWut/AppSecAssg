using BookwormsOnline.Model;
using BookwormsOnline.Services;
using BookwormsOnline.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BookwormsOnline.Pages
{
    [Authorize]
    public class ChangePasswordModel : PageModel
    {
        private readonly UserManager<Member> _userManager;
        private readonly SignInManager<Member> _signInManager;
        private readonly IAuditLogService _auditLogService;
        private readonly BookwormsDbContext _context;

        [BindProperty]
        public ChangePassword Model { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        // Password policy constants
        private static readonly TimeSpan MinPasswordAge = TimeSpan.FromMinutes(1); // Cannot change password within 1 min
        private static readonly TimeSpan MaxPasswordAge = TimeSpan.FromMinutes(2); // Must change password after 2 minutes
        private const int PasswordHistoryCount = 2; // Keep last 2 passwords

        public ChangePasswordModel(
            UserManager<Member> userManager,
            SignInManager<Member> signInManager,
            IAuditLogService auditLogService,
            BookwormsDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _auditLogService = auditLogService;
            _context = context;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("./Login");
            }

            // Check if password is expired (max age exceeded)
            if (user.PasswordExpiryDate.HasValue && user.PasswordExpiryDate.Value < DateTime.UtcNow)
            {
                StatusMessage = $"Your password expired on {user.PasswordExpiryDate.Value:yyyy-MM-dd HH:mm:ss}. Please change it now.";
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("./Login");
            }

            // Check minimum password age (cannot change too soon)
            if (user.LastPasswordChangedDate.HasValue)
            {
                var timeSinceLastChange = DateTime.UtcNow - user.LastPasswordChangedDate.Value;
                if (timeSinceLastChange < MinPasswordAge)
                {
                    var remainingTime = (int)Math.Ceiling((MinPasswordAge - timeSinceLastChange).TotalSeconds);
                    ModelState.AddModelError(string.Empty,
                        $"You cannot change your password yet. Please wait {remainingTime} second(s).");
                    await _auditLogService.LogAsync(user.Id, user.Email, "PASSWORD_CHANGE_ATTEMPT",
                        "Minimum password age not met", false);
                    return Page();
                }
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
                    await _auditLogService.LogAsync(user.Id, user.Email, "PASSWORD_CHANGE_ATTEMPT",
                        "Password reuse detected", false);
                    return Page();
                }
            }

            // Change password
            var changePasswordResult = await _userManager.ChangePasswordAsync(user, Model.CurrentPassword, Model.NewPassword);
            if (!changePasswordResult.Succeeded)
            {
                foreach (var error in changePasswordResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                await _auditLogService.LogAsync(user.Id, user.Email, "PASSWORD_CHANGE_ATTEMPT",
                    "Invalid current password", false);
                return Page();
            }

            // Store old password in history
            var currentPasswordHash = user.PasswordHash;
            var passwordHistory = new PasswordHistory
            {
                UserId = user.Id,
                PasswordHash = currentPasswordHash,
                CreatedDate = DateTime.UtcNow
            };
            _context.PasswordHistories.Add(passwordHistory);

            // Update password age tracking
            user.LastPasswordChangedDate = DateTime.UtcNow;
            user.PasswordExpiryDate = DateTime.UtcNow.Add(MaxPasswordAge);
            await _userManager.UpdateAsync(user);

            await _context.SaveChangesAsync();

            // Sign in again to refresh security stamp
            await _signInManager.RefreshSignInAsync(user);

            await _auditLogService.LogAsync(user.Id, user.Email, "PASSWORD_CHANGED",
                "Password changed successfully", true);

            StatusMessage = "Your password has been changed successfully.";

            return RedirectToPage("./Index");
        }
    }
}
