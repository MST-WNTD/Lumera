namespace Lumera.Services
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(string toEmail, string subject, string body);
        Task<bool> SendWelcomeEmailAsync(string toEmail, string userName);
        Task<bool> SendBookingConfirmationAsync(string toEmail, string eventName, DateTime eventDate);
        Task<bool> SendBookingStatusUpdateAsync(string toEmail, string eventName, string status);
        Task<bool> SendPasswordResetEmailAsync(string toEmail, string resetLink);
        Task<bool> SendApprovalNotificationAsync(string toEmail, string userName, string role);
    }
}