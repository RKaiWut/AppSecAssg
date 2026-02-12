using BookwormsOnline.Model;
using BookwormsOnline.Services;
using BookwormsOnline.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;

namespace BookwormsOnline.Pages
{
    public class LoginModel : PageModel
    {
        [BindProperty]
        public Login LModel { get; set; }

        private readonly string _recaptchaSecret;
        public string RecaptchaSiteKey { get; set; }
        public string RecaptchaErrorMessage { get; set; }

        private readonly SignInManager<Member> signInManager;
        private readonly UserManager<Member> userManager;
        private readonly IAuditLogService auditLogService;

        public LoginModel(IConfiguration configuration, SignInManager<Member> signInManager, UserManager<Member> userManager, IAuditLogService auditLogService)
        {
            this.signInManager = signInManager;
            this.userManager = userManager;
            this.auditLogService = auditLogService;
            _recaptchaSecret = configuration["RecaptchaSettings:SecretKey"];
            RecaptchaSiteKey = configuration["RecaptchaSettings:SiteKey"];
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Input validation - check for null or empty
            if (string.IsNullOrWhiteSpace(LModel?.Email) || string.IsNullOrWhiteSpace(LModel?.Password))
            {
                ModelState.AddModelError("", "Email and password are required.");
                return Page();
            }

            // Sanitize email input - remove dangerous characters
            LModel.Email = SanitizeInput(LModel.Email);

            // 1. Get token
            var recaptchaResponse = Request.Form["g-recaptcha-response"];

            // 2. Verify token with Google
            if (_recaptchaSecret == null)
            {
                RecaptchaErrorMessage = "reCAPTCHA secret key is not configured.";
                ModelState.AddModelError("", "reCAPTCHA configuration error");
                await auditLogService.LogAsync("", LModel.Email, "LOGIN_ATTEMPT", "reCAPTCHA configuration error", false);
                return Page();
            }
            if (!await VerifyRecaptcha(recaptchaResponse))
            {
                RecaptchaErrorMessage = "Please verify that you are not a robot.";
                ModelState.AddModelError("", "reCAPTCHA validation failed");
                await auditLogService.LogAsync("", LModel.Email, "LOGIN_ATTEMPT", "reCAPTCHA validation failed", false);
                return Page();
            }

            // 3. Normal login flow
            if (ModelState.IsValid)
            {
                // Use email - sanitize to prevent email alias abuse
                string cleanEmail = Regex.Replace(LModel.Email, @"\+.*?(?=@)", "");
                
                // Find user
                var user = await userManager.FindByNameAsync(cleanEmail);
                
                if (user != null)
                {
                    // Check if user is locked out
                    if (await userManager.IsLockedOutAsync(user))
                    {
                        var lockoutEnd = await userManager.GetLockoutEndDateAsync(user);
                        var remainingTime = lockoutEnd.HasValue 
                            ? (lockoutEnd.Value - DateTimeOffset.UtcNow).TotalSeconds 
                            : 0;
                        
                        ModelState.AddModelError("", $"Account is locked out. Please try again in {Math.Ceiling(remainingTime)} seconds.");
                        await auditLogService.LogAsync(user.Id, cleanEmail, "LOGIN_ATTEMPT", "Account locked out", false);
                        return Page();
                    }

                    // Attempt sign in
                    var identityResult = await signInManager.PasswordSignInAsync(
                        cleanEmail,
                        LModel.Password,
                        LModel.RememberMe,
                        true); // Enable lockout

                    if (identityResult.Succeeded)
                    {
                        // Generate new session ID
                        var newSessionId = Guid.NewGuid().ToString();
                        
                        // Invalidate previous session if exists
                        if (!string.IsNullOrEmpty(user.CurrentSessionId))
                        {
                            // Update security stamp to invalidate old sessions
                            await userManager.UpdateSecurityStampAsync(user);
                            await auditLogService.LogAsync(user.Id, cleanEmail, "SESSION_INVALIDATED", "Previous session invalidated due to new login", true);
                        }

                        // Update user session info
                        user.CurrentSessionId = newSessionId;
                        user.LastLoginDate = DateTime.UtcNow;
                        
                        // Set password expiry if not already set
                        if (!user.LastPasswordChangedDate.HasValue)
                        {
                            user.LastPasswordChangedDate = DateTime.UtcNow;
                            user.PasswordExpiryDate = DateTime.UtcNow.AddMinutes(2);
                        }
                        
                        await userManager.UpdateAsync(user);

                        // Store session ID in cookie
                        Response.Cookies.Append("SessionId", newSessionId, new CookieOptions
                        {
                            HttpOnly = true,
                            Secure = true,
                            SameSite = SameSiteMode.Strict,
                            Expires = DateTimeOffset.UtcNow.AddMinutes(1) // Match session timeout
                        });

                        await auditLogService.LogAsync(user.Id, cleanEmail, "LOGIN_SUCCESS", "User logged in successfully", true);
                        
                        // Check if user needs to setup 2FA (first login)
                        if (!user.TwoFactorEnabled)
                        {
                            await auditLogService.LogAsync(user.Id, cleanEmail, "REDIRECT_2FA_SETUP", "First login - redirecting to 2FA setup", true);
                            return RedirectToPage("./Setup2FA", new { mandatory = true });
                        }
                        
                        return RedirectToPage("Index");
                    }
                    else if (identityResult.RequiresTwoFactor)
                    {
                        // Redirect to 2FA verification
                        return RedirectToPage("./Verify2FA", new { rememberMe = LModel.RememberMe });
                    }
                    else if (identityResult.IsLockedOut)
                    {
                        ModelState.AddModelError("", "Account is locked out due to multiple failed login attempts.");
                        await auditLogService.LogAsync(user.Id, cleanEmail, "LOGIN_ATTEMPT", "Account locked out after failed attempts", false);
                    }
                    else if (identityResult.IsNotAllowed)
                    {
                        ModelState.AddModelError("", "Login not allowed.");
                        await auditLogService.LogAsync(user.Id, cleanEmail, "LOGIN_ATTEMPT", "Login not allowed", false);
                    }
                    else
                    {
                        ModelState.AddModelError("", "Email or Password incorrect");
                        await auditLogService.LogAsync(user.Id, cleanEmail, "LOGIN_ATTEMPT", "Invalid credentials", false);
                    }
                }
                else
                {
                    // User not found - still log attempt
                    ModelState.AddModelError("", "Email or Password incorrect");
                    await auditLogService.LogAsync("", cleanEmail, "LOGIN_ATTEMPT", "User not found", false);
                }
                
                return Page();
            }

            return Page();
        }

        // ---------------------------------------
        // SANITIZE INPUT TO PREVENT XSS
        // ---------------------------------------
        private string SanitizeInput(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // HTML encode to prevent XSS
            return HttpUtility.HtmlEncode(input).Trim();
        }

        // ---------------------------------------
        // VERIFY GOOGLE RECAPTCHA
        // ---------------------------------------
        private async Task<bool> VerifyRecaptcha(string recaptchaResponse)
        {
            if (string.IsNullOrEmpty(recaptchaResponse))
                return false;

            using var client = new HttpClient();

            var result = await client.GetStringAsync(
                $"https://www.google.com/recaptcha/api/siteverify?secret={_recaptchaSecret}&response={recaptchaResponse}");

            var json = JsonSerializer.Deserialize<RecaptchaVerifyResponse>(result);

            return json.success;
        }

        public class RecaptchaVerifyResponse
        {
            public bool success { get; set; }
            public double score { get; set; }
            public string action { get; set; }
            public List<string> error_codes { get; set; }
        }
    }
}
