using MailKit.Net.Smtp;
using MimeKit;
using MailKit.Security;

namespace DairyIndustry.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        // ─────────────────────────────────────────────────────────────
        // OTP email
        // ─────────────────────────────────────────────────────────────
        public async Task SendOtpEmailAsync(string toEmail, string otp)
        {
            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(_config["EmailSettings:SenderEmail"]));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = "Email Verification OTP";

            message.Body = new TextPart(MimeKit.Text.TextFormat.Html)
            {
                Text = $@"
                <div style='font-family:Arial; padding:10px'>
                    <h2 style='color:#2e7d32'>Email Verification</h2>
                    <p>Your OTP for verification is:</p>
                    <h1 style='color:#1976d2; letter-spacing:3px'>{otp}</h1>
                    <p>This OTP is valid for <b>10 minutes</b>.</p>
                    <hr/>
                    <small>If you did not request this, please ignore this email.</small>
                </div>"
            };

            await SendAsync(message);
        }

        // ─────────────────────────────────────────────────────────────
        // Approval email
        // ─────────────────────────────────────────────────────────────
        public async Task SendApprovalEmailAsync(
            string toEmail,
            string farmerCode,
            string defaultPassword,
            string loginUrl)
        {
            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(_config["EmailSettings:SenderEmail"]));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = "Your Farmer Registration Has Been Approved";

            message.Body = new TextPart(MimeKit.Text.TextFormat.Html)
            {
                Text = $@"
                    <div style='font-family:Arial,sans-serif;padding:20px;max-width:600px;'>
                        <h2 style='color:#2e7d32;'>Registration Approved</h2>
                        <p>Dear Farmer,</p>
                        <p>Your registration has been approved successfully.</p>
                        <p><strong>Farmer Code:</strong> {farmerCode}</p>
                        <p><strong>Password:</strong> {defaultPassword}</p>
                        <p>
                            Please log in using the above credentials and change your password after your first login.
                        </p>
                        <p>
                            <a href='{loginUrl}'>Click here to Login</a>
                        </p>
                        <br/>
                        <p>Regards,<br/>Dairy Management Team</p>

                    </div>"
            };

            await SendAsync(message);
        }

        // ─────────────────────────────────────────────────────────────
        // Shared async SMTP send — keeps connection logic in one place
        // ─────────────────────────────────────────────────────────────
        private async Task SendAsync(MimeMessage message)
        {
            using var smtp = new SmtpClient();
            smtp.ServerCertificateValidationCallback = (s, c, h, e) => true;
            await smtp.ConnectAsync(
                _config["EmailSettings:SmtpHost"],
                Convert.ToInt32(_config["EmailSettings:SmtpPort"]),
                SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(
                _config["EmailSettings:SenderEmail"],
                _config["EmailSettings:AppPassword"]);
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);
        }


        //For ForgotPassword - Farmer Portal
        public async Task SendForgotPasswordOtpAsync(string toEmail, string farmerName, string otp)
        {
            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(_config["EmailSettings:SenderEmail"]));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = "Password Reset OTP - Dairy Management System";

            message.Body = new TextPart(MimeKit.Text.TextFormat.Html)
            {
                Text = $@"
                <div style='font-family:Arial,sans-serif;max-width:480px;margin:0 auto;padding:24px;color:#333'>
 
                    <p style='margin:0 0 8px'>Hello <b>{farmerName}</b>,</p>
                    <p style='margin:0 0 24px;color:#555'>Use the OTP below to reset your password.</p>
 
                    <div style='text-align:center;padding:20px;background:#f5f5f5;border-radius:8px;margin-bottom:24px'>
                        <span style='font-size:32px;font-weight:bold;letter-spacing:10px;color:#1a1a1a'>{otp}</span>
                        <p style='margin:8px 0 0;font-size:13px;color:#888'>Valid for 10 minutes</p>
                    </div>
 
                    <p style='font-size:13px;color:#888;margin:0'>
                        If you did not request this, ignore this email. Your password will remain unchanged.
                    </p>
 
                    <hr style='border:none;border-top:1px solid #eee;margin:20px 0'/>
                    <p style='font-size:12px;color:#bbb;margin:0'>Dairy Management System</p>
                </div>"
            };

            await SendAsync(message);
        }
    }
}
