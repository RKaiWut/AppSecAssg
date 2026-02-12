using BookwormsOnline.Model;
using BookwormsOnline.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text;
using System.Text.Encodings.Web;

namespace BookwormsOnline.Pages
{
    [Authorize]
    public class Setup2FAModel : PageModel
    {
        private readonly UserManager<Member> _userManager;
        private readonly SignInManager<Member> _signInManager;
        private readonly IAuditLogService _auditLogService;
        private readonly UrlEncoder _urlEncoder;

        [BindProperty]
        public string VerificationCode { get; set; }

        public string SharedKey { get; set; }
        public string AuthenticatorUri { get; set; }
        public string QrCodeData { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        public bool IsMandatory { get; set; }

        private const string AuthenticatorUriFormat = "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6";

        public Setup2FAModel(
            UserManager<Member> userManager,
            SignInManager<Member> signInManager,
            IAuditLogService auditLogService,
            UrlEncoder urlEncoder)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _auditLogService = auditLogService;
            _urlEncoder = urlEncoder;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("./Login");
            }

            // Check if coming from mandatory setup
            IsMandatory = Request.Query.ContainsKey("mandatory");

            await LoadSharedKeyAndQrCodeUriAsync(user);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("./Login");
            }

            if (!ModelState.IsValid)
            {
                await LoadSharedKeyAndQrCodeUriAsync(user);
                return Page();
            }

            // Strip spaces and hyphens
            var verificationCode = VerificationCode.Replace(" ", string.Empty).Replace("-", string.Empty);

            var is2faTokenValid = await _userManager.VerifyTwoFactorTokenAsync(
                user, _userManager.Options.Tokens.AuthenticatorTokenProvider, verificationCode);

            if (!is2faTokenValid)
            {
                ModelState.AddModelError("VerificationCode", "Verification code is invalid.");
                await LoadSharedKeyAndQrCodeUriAsync(user);
                await _auditLogService.LogAsync(user.Id, user.Email, "2FA_SETUP_FAILED", "Invalid verification code", false);
                return Page();
            }

            // Enable 2FA for user
            await _userManager.SetTwoFactorEnabledAsync(user, true);
            await _userManager.UpdateAsync(user);

            await _auditLogService.LogAsync(user.Id, user.Email, "2FA_ENABLED", "Two-factor authentication enabled", true);

            StatusMessage = "Your authenticator app has been verified successfully.";

            // Generate recovery codes
            var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
            TempData["RecoveryCodes"] = string.Join(",", recoveryCodes);

            return RedirectToPage("./Show2FARecoveryCodes");
        }

        private async Task LoadSharedKeyAndQrCodeUriAsync(Member user)
        {
            // Load the authenticator key & QR code URI
            var unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
            if (string.IsNullOrEmpty(unformattedKey))
            {
                await _userManager.ResetAuthenticatorKeyAsync(user);
                unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
            }

            SharedKey = FormatKey(unformattedKey);

            var email = await _userManager.GetEmailAsync(user);
            AuthenticatorUri = GenerateQrCodeUri(email, unformattedKey);
        }

        private string FormatKey(string unformattedKey)
        {
            var result = new StringBuilder();
            int currentPosition = 0;
            while (currentPosition + 4 < unformattedKey.Length)
            {
                result.Append(unformattedKey.AsSpan(currentPosition, 4)).Append(' ');
                currentPosition += 4;
            }
            if (currentPosition < unformattedKey.Length)
            {
                result.Append(unformattedKey.AsSpan(currentPosition));
            }

            return result.ToString().ToLowerInvariant();
        }

        private string GenerateQrCodeUri(string email, string unformattedKey)
        {
            return string.Format(
                AuthenticatorUriFormat,
                _urlEncoder.Encode("BookwormsOnline"),
                _urlEncoder.Encode(email),
                unformattedKey);
        }
    }
}
