using BookwormsOnline.Model;
using BookwormsOnline.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BookwormsOnline.Pages
{
    [Authorize]
    public class LogoutModel : PageModel
    {
        private readonly SignInManager<Member> signInManager;
        private readonly UserManager<Member> userManager;
        private readonly IAuditLogService auditLogService;

        public LogoutModel(SignInManager<Member> signInManager, UserManager<Member> userManager, IAuditLogService auditLogService)
        {
            this.signInManager = signInManager;
            this.userManager = userManager;
            this.auditLogService = auditLogService;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostLogoutAsync()
        {
            // Get current user before signing out
            var user = await userManager.GetUserAsync(User);
            
            if (user != null)
            {
                // Clear session ID
                user.CurrentSessionId = null;
                await userManager.UpdateAsync(user);

                // Log the logout action
                await auditLogService.LogAsync(user.Id, user.Email, "LOGOUT", "User logged out successfully", true);
            }

            // Sign out the user
            await signInManager.SignOutAsync();

            // Clear session
            HttpContext.Session.Clear();

            // Delete session cookie
            Response.Cookies.Delete("SessionId");
            Response.Cookies.Delete(".AspNetCore.Identity.Application");
            Response.Cookies.Delete("MyCookieAuth");

            // Clear any cached data
            Response.Headers.Append("Cache-Control", "no-store, no-cache, must-revalidate");
            Response.Headers.Append("Pragma", "no-cache");
            Response.Headers.Append("Expires", "0");

            return RedirectToPage("Login");
        }

        public async Task<IActionResult> OnPostDontLogoutAsync()
        {
            return RedirectToPage("Index");
        }
    }
}
