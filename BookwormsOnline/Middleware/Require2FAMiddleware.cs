using Microsoft.AspNetCore.Identity;
using BookwormsOnline.Model;

namespace BookwormsOnline.Middleware
{
    public class Require2FAMiddleware
    {
        private readonly RequestDelegate _next;

        public Require2FAMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, UserManager<Member> userManager)
        {
            // Skip for specific pages
            var path = context.Request.Path.Value?.ToLower() ?? "";
            if (path.Contains("/login") || path.Contains("/logout") || 
                path.Contains("/register") || path.Contains("/setup2fa") ||
                path.Contains("/verify2fa") || path.Contains("/show2farecoverycodes") ||
                path.Contains("/changepassword") || path.Contains("/error") || 
                path.Contains("/checksession"))
            {
                await _next(context);
                return;
            }

            // Check if user is authenticated
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                var user = await userManager.GetUserAsync(context.User);
                
                if (user != null)
                {
                    // Check if 2FA is enabled
                    if (!user.TwoFactorEnabled)
                    {
                        // Redirect to 2FA setup
                        context.Response.Redirect("/Setup2FA?mandatory=true");
                        return;
                    }
                }
            }

            await _next(context);
        }
    }
}
