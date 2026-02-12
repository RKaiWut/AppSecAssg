namespace BookwormsOnline.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string message);
        Task SendPasswordResetEmailAsync(string toEmail, string resetLink);
    }
}
