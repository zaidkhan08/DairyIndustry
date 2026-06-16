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
                <div style='font-family:Arial,sans-serif;max-width:600px;padding:24px'>
 
                    <div style='background:#2e7d32;padding:20px 24px;border-radius:8px 8px 0 0'>
                        <h2 style='color:#fff;margin:0'>Registration Approved!</h2>
                    </div>
 
                    <div style='border:1px solid #e0e0e0;border-top:none;padding:24px;border-radius:0 0 8px 8px'>
                        <p>Your registration at the collection center has been <b>approved</b>.</p>
                        <p>Here are your login credentials:</p>
 
                        <table style='width:100%;background:#f5f5f5;border-radius:8px;padding:16px;border-collapse:collapse'>
                            <tr>
                                <td style='padding:8px 12px;font-weight:bold;color:#555'>Farmer Code</td>
                                <td style='padding:8px 12px;font-size:20px;font-weight:bold;color:#1976d2;letter-spacing:2px'>{farmerCode}</td>
                            </tr>
                            <tr>
                                <td style='padding:8px 12px;font-weight:bold;color:#555'>Default Password</td>
                                <td style='padding:8px 12px;font-size:20px;font-weight:bold;color:#1976d2;letter-spacing:2px'>{defaultPassword}</td>
                            </tr>
                        </table>
 
                        <p style='margin-top:20px'>
                            <b>Important:</b> Your default password is the last 4 digits of your registered mobile number.
                            You will be asked to set a new password the first time you log in.
                        </p>
 
                        <div style='text-align:center;margin:28px 0'>
                            <a href='{loginUrl}'
                               style='display:inline-block;background:#2e7d32;color:#fff;text-decoration:none;
                                      padding:14px 32px;border-radius:6px;font-size:16px;font-weight:bold'>
                                Log In &amp; Change Password
                            </a>
                        </div>
 
                        <p style='color:#999;font-size:12px;border-top:1px solid #eee;padding-top:12px;margin-top:0'>
                            If you did not register with this dairy management system, please ignore this email.
                        </p>
                    </div>
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
    }
}


//using MailKit.Net.Smtp;
//using MimeKit;
//using MailKit.Security;

//namespace DairyIndustry.Services
//{
//    public class EmailService
//    {
//        private readonly IConfiguration _config;

//        public EmailService(IConfiguration config)
//        {
//            _config = config;
//        }

//        // ─────────────────────────────────────────────────────────────
//        // EXISTING — OTP email 
//        // ─────────────────────────────────────────────────────────────
//        public void SendOtpEmail(string toEmail, string otp)
//        {
//            var message = new MimeMessage();
//            message.From.Add(MailboxAddress.Parse(_config["EmailSettings:SenderEmail"]));
//            message.To.Add(MailboxAddress.Parse(toEmail));
//            message.Subject = "Email Verification OTP";

//            message.Body = new TextPart(MimeKit.Text.TextFormat.Html)
//            {
//                Text = $@"
//                <div style='font-family:Arial; padding:10px'>
//                    <h2 style='color:#2e7d32'>Email Verification</h2>
//                    <p>Your OTP for verification is:</p>
//                    <h1 style='color:#1976d2; letter-spacing:3px'>{otp}</h1>
//                    <p>This OTP is valid for <b>10 minutes</b>.</p>
//                    <hr/>
//                    <small>If you did not request this, please ignore this email.</small>
//                </div>"
//            };

//            Send(message);
//        }


//        public void SendApprovalEmail(
//            string toEmail,
//            string farmerCode,
//            string defaultPassword,
//            string loginUrl)
//        {
//            var message = new MimeMessage();
//            message.From.Add(MailboxAddress.Parse(_config["EmailSettings:SenderEmail"]));
//            message.To.Add(MailboxAddress.Parse(toEmail));
//            message.Subject = "Your Farmer Registration Has Been Approved";

//            message.Body = new TextPart(MimeKit.Text.TextFormat.Html)
//            {
//                Text = $@"
//                <div style='font-family:Arial,sans-serif;max-width:600px;padding:24px'>

//                    <div style='background:#2e7d32;padding:20px 24px;border-radius:8px 8px 0 0'>
//                        <h2 style='color:#fff;margin:0'>Registration Approved!</h2>
//                    </div>

//                    <div style='border:1px solid #e0e0e0;border-top:none;padding:24px;border-radius:0 0 8px 8px'>
//                        <p>Your registration at the collection center has been <b>approved</b>.</p>
//                        <p>Here are your login credentials:</p>

//                        <table style='width:100%;background:#f5f5f5;border-radius:8px;padding:16px;border-collapse:collapse'>
//                            <tr>
//                                <td style='padding:8px 12px;font-weight:bold;color:#555'>Farmer Code</td>
//                                <td style='padding:8px 12px;font-size:20px;font-weight:bold;color:#1976d2;letter-spacing:2px'>{farmerCode}</td>
//                            </tr>
//                            <tr>
//                                <td style='padding:8px 12px;font-weight:bold;color:#555'>Default Password</td>
//                                <td style='padding:8px 12px;font-size:20px;font-weight:bold;color:#1976d2;letter-spacing:2px'>{defaultPassword}</td>
//                            </tr>
//                        </table>

//                        <p style='margin-top:20px'>
//                            <b>Important:</b> Your default password is the last 4 digits of your registered mobile number.
//                            You will be asked to set a new password the first time you log in.
//                        </p>

//                        <div style='text-align:center;margin:28px 0'>
//                            <a href='{loginUrl}'
//                               style='display:inline-block;background:#2e7d32;color:#fff;text-decoration:none;
//                                      padding:14px 32px;border-radius:6px;font-size:16px;font-weight:bold'>
//                                Log In &amp; Change Password
//                            </a>
//                        </div>

//                        <p style='color:#999;font-size:12px;border-top:1px solid #eee;padding-top:12px;margin-top:0'>
//                            If you did not register with this dairy management system, please ignore this email.
//                        </p>
//                    </div>
//                </div>"
//            };

//            Send(message);
//        }

//        // ─────────────────────────────────────────────────────────────
//        // Shared SMTP send — keeps connection logic in one place
//        // ─────────────────────────────────────────────────────────────
//        private void Send(MimeMessage message)
//        {
//            using var smtp = new SmtpClient();
//            smtp.ServerCertificateValidationCallback = (s, c, h, e) => true;
//            smtp.Connect(
//                _config["EmailSettings:SmtpHost"],
//                Convert.ToInt32(_config["EmailSettings:SmtpPort"]),
//                SecureSocketOptions.StartTls);
//            smtp.Authenticate(_config["EmailSettings:SenderEmail"], _config["EmailSettings:AppPassword"]);
//            smtp.Send(message);
//            smtp.Disconnect(true);
//        }


//    }
//}