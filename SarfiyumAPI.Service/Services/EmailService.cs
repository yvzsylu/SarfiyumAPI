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

        public async Task SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true)
        {
            // 1. Ayarları Çek
            var config = await _settingsService.GetConfigInternalAsync();
            if (config == null)
                throw new Exception("SMTP ayarları bulunamadı.");

            // 2. Email Mesajını Oluştur (MimeKit kullanır)
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(config.SmtpDisplayName, config.SmtpEmail));
            email.To.Add(new MailboxAddress("", toEmail));
            email.Subject = subject;
            email.Body = new TextPart(isHtml ? TextFormat.Html : TextFormat.Plain) { Text = body };

            // 3. SMTP Client ile Gönder (MailKit kullanır)
            using var smtp = new SmtpClient();
            
            try 
            {
                // A. Bağlantı Kur
                // Port 587 genelde StartTls kullanır. 465 ise SslOnConnect.
                // SecureSocketOptions.Auto genelde doğruyu bulur ama hata alırsan StartTls dene.
                await smtp.ConnectAsync(config.SmtpHost, config.SmtpPort, SecureSocketOptions.Auto);

                // B. Kimlik Doğrulama
                await smtp.AuthenticateAsync(config.SmtpEmail, config.SmtpPassword);

                // C. Gönder
                await smtp.SendAsync(email);
                
                // D. Bağlantıyı Kes
                await smtp.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                // Hata detayını görmek için
                throw new Exception($"Mail gönderilemedi: {ex.Message}");
            }
        }
    }
}