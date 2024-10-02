using System.Net.Mail;
using System.Net;

namespace MqttListener
{
    class SmtpMailSender(Func<SmtpClient> clientFactory, MailAddress defaultSender)
    {
        public Func<SmtpClient> SmtpClientFactory { get; } = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));

        public MailAddress DefaultSender { get; } = defaultSender ?? throw new ArgumentNullException(nameof(defaultSender));

        public SmtpMailSender(SmtpSettings settings) : this(() => CreateSmtpClient(
            settings ?? throw new ArgumentNullException(nameof(settings))),
            new MailAddress(settings.Sender ?? throw new Exception("SMTP sender not configured."))
        )
        {

        }

        private static SmtpClient CreateSmtpClient(SmtpSettings settings)
        {
            var smtpClient = new SmtpClient()
            {
                EnableSsl = settings.UseSSL,
                Host = settings.Host ?? throw new Exception("SMTP host not configured."),
                Port = settings.Port,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(
                    settings.Username,
                    settings.Password
                ),
            };

            return smtpClient;
        }

        public void Send(MailMessage mail)
        {
            ArgumentNullException.ThrowIfNull(mail);

            using var client = SmtpClientFactory();

            EnsureSenderIsSet(mail);
            EnsureFromIsSet(mail);
            client.Send(mail);
        }

        private void EnsureSenderIsSet(MailMessage mail)
        {
            if (mail.Sender != null) return;
            if (DefaultSender == null)
            {
                // This should never be triggered, as we check for null in the constructor
                throw new ArgumentException("Sender is not set and there is no default sender available.");
            }

            mail.Sender = DefaultSender;
        }

        private void EnsureFromIsSet(MailMessage mail)
        {
            if (mail.From != null) return;
            if (DefaultSender == null)
            {
                // This should never be triggered, as we check for null in the constructor
                throw new ArgumentException("Sender is not set and there is no default sender available.");
            }

            mail.From = DefaultSender;
        }

        public async Task SendAsync(MailMessage mail, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(mail);

            using var client = SmtpClientFactory();
            EnsureSenderIsSet(mail);
            EnsureFromIsSet(mail);

            await client.SendMailAsync(mail, cancellationToken).ConfigureAwait(false);
        }
    }
}