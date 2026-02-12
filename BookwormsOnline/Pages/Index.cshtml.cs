using BookwormsOnline.Model;
using BookwormsOnline.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BookwormsOnline.Pages
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly UserManager<Member> _userManager;
        private readonly SignInManager<Member> _signInManager;
        private readonly IAuditLogService _auditLogService;
        private readonly IConfiguration _configuration;

        public Member CurrentUser { get; set; }
        public string DecryptedCreditCard { get; set; }

        public IndexModel(ILogger<IndexModel> logger, UserManager<Member> userManager, SignInManager<Member> signInManager, IAuditLogService auditLogService, IConfiguration configuration)
        {
            _logger = logger;
            _userManager = userManager;
            _signInManager = signInManager;
            _auditLogService = auditLogService;
            _configuration = configuration;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            // Prevent caching to ensure every refresh sends a request
            Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
            Response.Headers.Append("Pragma", "no-cache");
            Response.Headers.Append("Expires", "0");
            
            // Get current user
            var user = await _userManager.GetUserAsync(User);
            
            if (user == null)
            {
                return RedirectToPage("Login");
            }

            // Validate session
            var sessionId = Request.Cookies["SessionId"];
            if (string.IsNullOrEmpty(sessionId) || sessionId != user.CurrentSessionId)
            {
                // Session invalid - force logout
                await _signInManager.SignOutAsync();
                await _auditLogService.LogAsync(user.Id, user.Email, "SESSION_INVALID", "Session mismatch detected - user logged out", false);
                return RedirectToPage("Login");
            }

            CurrentUser = user;

            // Decrypt credit card for display (mask it)
            try
            {
                var secretKey = _configuration["SecretKey"];
                var dataProtectionProvider = DataProtectionProvider.Create("EncryptData");
                var protector = dataProtectionProvider.CreateProtector(secretKey);
                
                var decrypted = protector.Unprotect(user.CreditCardNo);
                // Mask credit card - show only last 4 digits
                DecryptedCreditCard = $"****-****-****-{decrypted.Substring(decrypted.Length - 4)}";
            }
            catch
            {
                DecryptedCreditCard = "****-****-****-****";
            }

            // Log page access
            await _auditLogService.LogAsync(user.Id, user.Email, "PAGE_ACCESS", "Accessed home page", true);

            return Page();
        }
    }
}
