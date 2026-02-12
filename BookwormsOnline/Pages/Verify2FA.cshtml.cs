using BookwormsOnline.Model;
using BookwormsOnline.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BookwormsOnline.Pages
{
    public class Verify2FAModel : PageModel
    {
        private readonly SignInManager<Member> _signInManager;
        private readonly UserManager<Member> _userManager;
        private readonly IAuditLogService _auditLogService;

        [BindProperty]
        public string TwoFactorCode { get; set; }

        [BindProperty]
        public bool RememberMachine { get; set; }

        [BindProperty]
        public bool UseRecoveryCode { get; set; }

        public bool RememberMe { get; set; }

        public Verify2FAModel(
            SignInManager<Member> signInManager,
            UserManager<Member> userManager,
            IAuditLogService auditLogService)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _auditLogService = auditLogService;
        }

        public async Task<IActionResult> OnGetAsync(bool rememberMe = false)
        {
            // Ensure the user has gone through the username & password screen first
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();

            if (user == null)
            {
                return RedirectToPage("./Login");
            }

            RememberMe = rememberMe;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(bool rememberMe = false)
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Unable to load two-factor authentication user.");
                return Page();
            }

            var authenticatorCode = TwoFactorCode.Replace(" ", string.Empty).Replace("-", string.Empty);

            Microsoft.AspNetCore.Identity.SignInResult result;

            if (UseRecoveryCode)
            {
                result = await _signInManager.TwoFactorRecoveryCodeSignInAsync(authenticatorCode);
                
                if (result.Succeeded)
                {
                    await _auditLogService.LogAsync(user.Id, user.Email, "2FA_LOGIN_RECOVERY", "Logged in with recovery code", true);
                }
                else
                {
                    await _auditLogService.LogAsync(user.Id, user.Email, "2FA_LOGIN_FAILED", "Invalid recovery code", false);
                }
            }
            else
            {
                result = await _signInManager.TwoFactorAuthenticatorSignInAsync(authenticatorCode, rememberMe, RememberMachine);
                
                if (result.Succeeded)
                {
                    await _auditLogService.LogAsync(user.Id, user.Email, "2FA_LOGIN_SUCCESS", "Logged in with 2FA", true);
                }
                else
                {
                    await _auditLogService.LogAsync(user.Id, user.Email, "2FA_LOGIN_FAILED", "Invalid authenticator code", false);
                }
            }

            if (result.Succeeded)
            {
                // Update session info
                var member = await _userManager.FindByIdAsync(user.Id);
                if (member != null)
                {
                    var newSessionId = Guid.NewGuid().ToString();
                    
                    // Invalidate previous session if exists
                    if (!string.IsNullOrEmpty(member.CurrentSessionId))
                    {
                        await _userManager.UpdateSecurityStampAsync(member);
                        await _auditLogService.LogAsync(member.Id, member.Email, "SESSION_INVALIDATED", "Previous session invalidated due to new login", true);
                    }

                    member.CurrentSessionId = newSessionId;
                    member.LastLoginDate = DateTime.UtcNow;
                    
                    // Set password expiry if not already set
                    if (!member.LastPasswordChangedDate.HasValue)
                    {
                        member.LastPasswordChangedDate = DateTime.UtcNow;
                        member.PasswordExpiryDate = DateTime.UtcNow.AddMinutes(2);
                    }
                    
                    await _userManager.UpdateAsync(member);

                    // Store session ID in cookie
                    Response.Cookies.Append("SessionId", newSessionId, new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Strict,
                        Expires = DateTimeOffset.UtcNow.AddMinutes(1)
                    });
                }

                return RedirectToPage("./Index");
            }
            else if (result.IsLockedOut)
            {
                await _auditLogService.LogAsync(user.Id, user.Email, "2FA_LOGIN_LOCKOUT", "Account locked out", false);
                ModelState.AddModelError(string.Empty, "Account locked out.");
                return Page();
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Invalid authenticator code.");
                return Page();
            }
        }
    }
}
