using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace Narodnici.Services
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string email, string subject, string htmlMessage);
    }

    public class EmailSender : IEmailSender
    {
        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // ЗАБЕЛЕЖКА: За реално пращане на имейли, тук се въвеждат данните от SMTP сървъра (напр. Brevo, SendGrid или Gmail)
            // За целите на разработката, кодът няма да гърми, дори да няма реални данни, но няма и да прати реално писмо, 
            // докато не въведеш валидни Credentials по-долу.

            try
            {
                var mail = new MailMessage();
                mail.From = new MailAddress("noreply@projectlibrary.com", "Narodnici");
                mail.To.Add(email);
                mail.Subject = subject;
                mail.Body = htmlMessage;
                mail.IsBodyHtml = true;

                // Примерни настройки (Трябва да се сменят с истински за продукция)
                using var smtp = new SmtpClient("smtp.your-email-provider.com", 587)
                {
                    Credentials = new NetworkCredential("твоят-имейл@domain.com", "твоята-парола"),
                    EnableSsl = true
                };

                await smtp.SendMailAsync(mail);
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine("ГРЕШКА ПРИ ПРАЩАНЕ НА ИМЕЙЛ: " + ex.Message);
                // Заглушаваме грешката при разработка, за да можеш да тестваш интерфейса!
            }
        }
    }
}