using Microsoft.AspNetCore.Identity;
using BookwormsOnline.Model;

namespace BookwormsOnline.Middleware
{
    public class PasswordExpirationMiddleware
    {
        private readonly RequestDelegate _next;

        public PasswordExpirationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, UserManager<Member> userManager)
        {
            // Skip for specific pages
            var path = context.Request.Path.Value?.ToLower() ?? "";
            if (path.Contains("/login") || path.Contains("/logout") || 
                path.Contains("/register") || path.Contains("/changepassword") ||
                path.Contains("/error") || path.Contains("/checksession"))
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
                    // Check if password has expired
                    if (user.PasswordExpiryDate.HasValue && user.PasswordExpiryDate.Value < DateTime.UtcNow)
                    {
                        // Password expired - force change
                        context.Response.Redirect("/ChangePassword?passwordExpired=true");
                        return;
                    }
                }
            }

            await _next(context);
        }
    }
}
