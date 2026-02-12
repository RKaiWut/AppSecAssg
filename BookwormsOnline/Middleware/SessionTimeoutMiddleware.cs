using Microsoft.AspNetCore.Identity;
using BookwormsOnline.Model;

namespace BookwormsOnline.Middleware
{
    public class SessionTimeoutMiddleware
    {
        private readonly RequestDelegate _next;

        public SessionTimeoutMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, UserManager<Member> userManager, SignInManager<Member> signInManager)
        {
            // Skip for login, logout, and API endpoints
            var path = context.Request.Path.Value?.ToLower() ?? "";
            if (path.Contains("/login") || path.Contains("/logout") || 
                path.Contains("/register") || path.Contains("/checksession") ||
                path.Contains("/error"))
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
                    // Validate session ID
                    var sessionId = context.Request.Cookies["SessionId"];
                    if (string.IsNullOrEmpty(sessionId) || sessionId != user.CurrentSessionId)
                    {
                        // Session invalid - sign out and redirect
                        await signInManager.SignOutAsync();
                        context.Response.Redirect("/Login?sessionExpired=true");
                        return;
                    }
                }
            }

            await _next(context);
        }
    }
}
