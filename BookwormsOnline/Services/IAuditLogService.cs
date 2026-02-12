namespace BookwormsOnline.Services
{
    public interface IAuditLogService
    {
        Task LogAsync(string userId, string userEmail, string action, string details, bool isSuccessful = true);
    }
}
