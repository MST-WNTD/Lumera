using System.Net.Mail;
using System.Net;

namespace Lumera.Services
{
    public class EmailService : IEmailService
    {
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _smtpUsername;
        private readonly string _smtpPassword;
        private readonly bool _enableSsl;

        public EmailService(IConfiguration configuration)
        {
            _smtpServer = configuration["EmailSettings:SmtpServer"] ?? "smtp.gmail.com";
            _smtpPort = int.Parse(configuration["EmailSettings:SmtpPort"] ?? "587");
            _smtpUsername = configuration["EmailSettings:SmtpUsername"] ?? "";
            _smtpPassword = configuration["EmailSettings:SmtpPassword"] ?? "";
            _enableSsl = bool.Parse(configuration["EmailSettings:EnableSsl"] ?? "true");
        }

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                using var client = new SmtpClient(_smtpServer, _smtpPort)
                {
                    Credentials = new NetworkCredential(_smtpUsername, _smtpPassword),
                    EnableSsl = _enableSsl
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_smtpUsername, "Lumera"),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };
                mailMessage.To.Add(toEmail);

                await client.SendMailAsync(mailMessage);
                return true;
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Email sending failed: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendWelcomeEmailAsync(string toEmail, string userName)
        {
            var subject = "Welcome to Lumera!";
            var body = $@"
                <h2>Welcome to Lumera, {userName}!</h2>
                <p>Thank you for joining our platform. We're excited to have you on board!</p>
                <p>Start exploring events and services today.</p>
                <br>
                <p>Best regards,<br>The Lumera Team</p>";

            return await SendEmailAsync(toEmail, subject, body);
        }

        public async Task<bool> SendBookingConfirmationAsync(string toEmail, string eventName, DateTime eventDate)
        {
            var subject = "Booking Confirmation - Lumera";
            var body = $@"
                <h2>Booking Confirmed!</h2>
                <p>Your booking for <strong>{eventName}</strong> has been confirmed.</p>
                <p>Event Date: {eventDate:MMMM dd, yyyy 'at' hh:mm tt}</p>
                <p>Thank you for choosing Lumera!</p>";

            return await SendEmailAsync(toEmail, subject, body);
        }

        public async Task<bool> SendBookingStatusUpdateAsync(string toEmail, string eventName, string status)
        {
            var subject = $"Booking Status Update - {status}";
            var body = $@"
                <h2>Booking Status Updated</h2>
                <p>Your booking for <strong>{eventName}</strong> has been updated to: <strong>{status}</strong></p>
                <p>Please check your dashboard for more details.</p>";

            return await SendEmailAsync(toEmail, subject, body);
        }

        public async Task<bool> SendPasswordResetEmailAsync(string toEmail, string resetLink)
        {
            var subject = "Password Reset Request - Lumera";
            var body = $@"
                <h2>Password Reset</h2>
                <p>You requested to reset your password. Click the link below to set a new password:</p>
                <p><a href='{resetLink}' style='background-color: #4CAF50; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Reset Password</a></p>
                <p>If you didn't request this, please ignore this email.</p>
                <p><small>This link will expire in 1 hour.</small></p>";

            return await SendEmailAsync(toEmail, subject, body);
        }

        public async Task<bool> SendApprovalNotificationAsync(string toEmail, string userName, string role)
        {
            var subject = "Account Approved - Lumera";
            var body = $@"
                <h2>Account Approved!</h2>
                <p>Dear {userName},</p>
                <p>Your {role} account has been approved by our admin team.</p>
                <p>You can now access all features of your dashboard.</p>
                <p>Welcome aboard!</p>";

            return await SendEmailAsync(toEmail, subject, body);
        }
    }
}