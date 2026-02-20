using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace PalGoAPI.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
            _httpClient = new HttpClient();
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var apiKey = _configuration["ResendSettings:ApiKey"];
            var fromEmail = _configuration["ResendSettings:FromEmail"];
            var fromName = _configuration["ResendSettings:FromName"];

            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", apiKey);

            var payload = new
            {
                from = $"{fromName} <{fromEmail}>",
                to = new[] { toEmail },
                subject = subject,
                html = body
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("https://api.resend.com/emails", content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Email sending failed: {error}");
            }
        }
    }
}


//using System.Net;
//using System.Net.Mail;

//namespace PalGoAPI.Services
//{
//    public class EmailService : IEmailService
//    {
//        private readonly IConfiguration _configuration;

//        public EmailService(IConfiguration configuration)
//        {
//            _configuration = configuration;
//        }

//        public async Task SendEmailAsync(string toEmail, string subject, string body)
//        {
//            var smtpSettings = _configuration.GetSection("SmtpSettings");

//            var client = new SmtpClient(smtpSettings["Host"])
//            {
//                Port = int.Parse(smtpSettings["Port"]),
//                Credentials = new NetworkCredential(
//                    smtpSettings["Username"],
//                    smtpSettings["Password"]
//                ),
//                EnableSsl = true,
//            };

//            var mailMessage = new MailMessage
//            {
//                From = new MailAddress(smtpSettings["FromEmail"], smtpSettings["FromName"]),
//                Subject = subject,
//                Body = body,
//                IsBodyHtml = true,
//            };

//            mailMessage.To.Add(toEmail);
//            await client.SendMailAsync(mailMessage);
//        }
//    }
//}