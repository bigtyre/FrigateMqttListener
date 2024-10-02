using System.Net.Mail;

namespace MqttListener
{
    class NotificationSender(int minNotificationIntervalSeconds, SmtpMailSender smtpSender)
    {
        public readonly TimeSpan MinInterval = TimeSpan.FromSeconds(minNotificationIntervalSeconds);
        private readonly Dictionary<string, DateTimeOffset> _lastNotificationTimes = [];

        public void SendNotification(DetectionEvent detection)
        {
            var camera = detection.After.Camera;
            if (camera is null)
            {
                Console.WriteLine($"Skipping sending notification. Camera was null.");
                return;
            }

            var now = DateTimeOffset.Now;
            if (_lastNotificationTimes.TryGetValue(camera, out var lastTime) && lastTime > now - MinInterval)
            {
                Console.WriteLine($"Skipping sending notification. Previous notification time was less than {MinInterval.TotalSeconds} seconds ago.");
                return;
            }

            _lastNotificationTimes[camera] = now;
            Console.WriteLine($"Sending notification for {camera}.");

            var mailMessage = CreateDetectionEmail(detection);

            smtpSender.Send(mailMessage);
        }

        private MailMessage CreateDetectionEmail(DetectionEvent detection)
        {
            throw new NotImplementedException();
        }
    }
}