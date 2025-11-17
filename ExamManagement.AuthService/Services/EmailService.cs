using System.Net;
using System.Net.Mail;
using System.Text;

namespace ExamManagement.AuthService.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> SendInvitationEmailAsync(string email, string role, string invitationToken, string baseUrl)
    {
        try
        {
            var smtpHost = _configuration["EmailSettings:SmtpHost"] ?? "smtp.gmail.com";
            var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
            var smtpUsername = _configuration["EmailSettings:SmtpUsername"] ?? "";
            var smtpPassword = _configuration["EmailSettings:SmtpPassword"] ?? "";
            var fromEmail = _configuration["EmailSettings:FromEmail"] ?? smtpUsername;
            var fromName = _configuration["EmailSettings:FromName"] ?? "Exam Management System";

            _logger.LogInformation("Attempting to send email. SMTP Host: {Host}, Port: {Port}, Username: {Username}, From: {FromEmail}, To: {ToEmail}", 
                smtpHost, smtpPort, smtpUsername, fromEmail, email);

            if (string.IsNullOrEmpty(smtpUsername) || string.IsNullOrEmpty(smtpPassword))
            {
                _logger.LogWarning("Email settings not configured. Skipping email send. Email: {Email}, Role: {Role}", email, role);
                return false;
            }

            // Link should point to frontend route, not API route
            var invitationLink = $"{baseUrl}/accept-invite?token={invitationToken}";

            var subject = $"Lời mời tham gia hệ thống quản lý thi - {role}";
            var body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #4CAF50; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .button {{ display: inline-block; padding: 12px 24px; background-color: #4CAF50; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
        .footer {{ text-align: center; padding: 20px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Lời mời tham gia hệ thống</h1>
        </div>
        <div class='content'>
            <p>Xin chào,</p>
            <p>Bạn đã được mời tham gia hệ thống quản lý thi với vai trò <strong>{role}</strong>.</p>
            <p>Vui lòng nhấp vào nút bên dưới để tạo tài khoản và mật khẩu của bạn:</p>
            <p style='text-align: center;'>
                <a href='{invitationLink}' class='button'>Chấp nhận lời mời</a>
            </p>
            <p>Hoặc sao chép và dán liên kết sau vào trình duyệt của bạn:</p>
            <p style='word-break: break-all; color: #0066cc;'>{invitationLink}</p>
            <p><strong>Lưu ý:</strong> Liên kết này sẽ hết hạn sau 7 ngày.</p>
        </div>
        <div class='footer'>
            <p>Email này được gửi tự động từ hệ thống quản lý thi. Vui lòng không trả lời email này.</p>
        </div>
    </div>
</body>
</html>";

            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                EnableSsl = true,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(smtpUsername, smtpPassword),
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            using var message = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            message.To.Add(email);

            await client.SendMailAsync(message);
            _logger.LogInformation("Invitation email sent successfully to {Email} for role {Role}", email, role);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send invitation email to {Email}", email);
            return false;
        }
    }
}

