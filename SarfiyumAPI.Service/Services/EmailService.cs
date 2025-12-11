using MailKit.Net.Smtp; // DİKKAT: System.Net.Mail DEĞİL
using MailKit.Security;
using MimeKit;
using MimeKit.Text;
using SarfiyumAPI.Core.Interfaces;

namespace SarfiyumAPI.Service.Services
{
    public class EmailService : IEmailService
    {
        private readonly ISettingsService _settingsService;

        public EmailService(ISettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string messageHtml, bool isHtml = true)
        {
            // 1. Ayarları Çek
            var config = await _settingsService.GetConfigInternalAsync();
            if (config == null)
                throw new Exception("SMTP ayarları bulunamadı.");



            // 2. HTML Template (Modern & Professional - #CB840B Palette)
            string template = @"
<!DOCTYPE html>
<html lang=""tr"">
<head>
<meta charset=""UTF-8"">
<meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
<title>Şifre Sıfırlama</title>
<style>
    body { margin: 0; padding: 0; background-color: #F4F4F4; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; }
    .email-wrapper { width: 100%; background-color: #F4F4F4; padding: 40px 0; }
    .email-content { max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 10px rgba(0,0,0,0.05); }
    .header { background-color: #CB840B; padding: 30px; text-align: center; }
    .body-content { padding: 40px 30px; color: #333333; line-height: 1.6; }
    .button-container { text-align: center; margin: 30px 0; }
    .cta-button { background-color: #CB840B; color: #ffffff !important; padding: 14px 28px; text-decoration: none; font-weight: bold; border-radius: 6px; display: inline-block; font-size: 16px; transition: background 0.3s; }
    .cta-button:hover { background-color: #a86b09; }
    .footer { background-color: #f9f9f9; padding: 20px; text-align: center; font-size: 12px; color: #888888; border-top: 1px solid #eeeeee; }
    .link-fallback { font-size: 13px; color: #666666; word-break: break-all; margin-top: 20px; background: #fafafa; padding: 10px; border-radius: 4px; }
    h1 { margin: 0; color: #ffffff; font-size: 24px; font-weight: 600; }
    p { margin-bottom: 15px; }
</style>
</head>
<body>

<div class=""email-wrapper"">
    <table role=""presentation"" border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"">
        <tr>
            <td align=""center"">
                <div class=""email-content"">
                    
                    <div class=""header"">
                        <svg width=""48"" height=""48"" viewBox=""0 0 24 24"" fill=""none"" xmlns=""http://www.w3.org/2000/svg"">
                            <path d=""M12 15V17M6 10V8C6 4.68629 8.68629 2 12 2C15.3137 2 18 4.68629 18 8V10M9 10C7.89543 10 7 10.8954 7 12V19C7 20.1046 7.89543 21 9 21H15C16.1046 21 17 20.1046 17 19V12C17 10.8954 16.1046 10 15 10H9Z"" stroke=""#ffffff"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round""/>
                        </svg>
                        <h1 style=""margin-top:10px;"">Şifre Sıfırlama</h1>
                    </div>

                    <div class=""body-content"">

                        <div>
                            {{CONTENT}}
                        </div>
                    </div>

                    <div class=""footer"">
                        <p style=""margin:0;"">Bu talebi siz oluşturmadıysanız, lütfen bu e-postayı dikkate almayın. Hesabınız güvendedir.</p>
                        <p style=""margin:10px 0 0 0;"">&copy; 2025 Sarfiyum. Tüm hakları saklıdır.</p>
                    </div>
                </div>
            </td>
        </tr>
    </table>
</div>

</body>
</html>
";

            // 3. İçeriği yerleştir (Placeholder'ları değiştiriyoruz)
            // messageHtml parametresinin sadece LINK olduğunu varsayarak yerleştiriyoruz.
            string finalHtml = template.Replace("{{CONTENT}}", messageHtml ?? "#");

            // 4. Email mesajını oluştur
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(config.SmtpDisplayName, config.SmtpEmail));
            email.To.Add(new MailboxAddress("", toEmail));

            // Konu boşsa default bir konu atayalım
            email.Subject = string.IsNullOrEmpty(subject) ? "Şifre Sıfırlama Talebi" : subject;

            email.Body = new TextPart(TextFormat.Html) { Text = finalHtml };

            // 5. SMTP Gönderimi
            using var smtp = new SmtpClient();
            try
            {
                // SSL sertifika hatalarını development ortamında yoksaymak isterseniz:
                // smtp.CheckCertificateRevocation = false; 

                await smtp.ConnectAsync(config.SmtpHost, config.SmtpPort, SecureSocketOptions.Auto);
                await smtp.AuthenticateAsync(config.SmtpEmail, config.SmtpPassword);
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                // Loglama mekanizmanız varsa buraya ekleyin
                throw new Exception($"Mail gönderilemedi: {ex.Message}");
            }
        }
    }
}