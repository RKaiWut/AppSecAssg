using BookwormsOnline.Model;
using Microsoft.EntityFrameworkCore;

namespace BookwormsOnline.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly BookwormsDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditLogService(BookwormsDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogAsync(string userId, string userEmail, string action, string details, bool isSuccessful = true)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var ipAddress = httpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown";
            var userAgent = httpContext?.Request?.Headers["User-Agent"].ToString() ?? "Unknown";

            var auditLog = new AuditLog
            {
                UserId = userId ?? "Anonymous",
                UserEmail = userEmail ?? "Anonymous",
                Action = action,
                Details = details,
                IpAddress = ipAddress,
                UserAgent = userAgent?.Length > 500 ? userAgent.Substring(0, 500) : userAgent,
                Timestamp = DateTime.UtcNow,
                IsSuccessful = isSuccessful
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }
    }
}
