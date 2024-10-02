using System;
using System.Globalization;
using System.Net.Mail;
using System.Text;

namespace MqttListener
{
    class NotificationSender(
        int minNotificationIntervalSeconds,
        SmtpMailSender smtpSender,
        NotificationSettings defaultNotificationSettings,
        MailAddress emailSenderAddress,
        FrigateApiClient frigateApiClient
    )
    {
        public readonly TimeSpan MinInterval = TimeSpan.FromSeconds(minNotificationIntervalSeconds);
        private readonly Dictionary<string, DateTimeOffset> _lastNotificationTimes = [];

        public async Task SendNotificationAsync(DetectionEvent detection, IEnumerable<NotificationSchedule> matchedScheduleTimes)
        {
            var camera = detection.Before.Camera;
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

            var emailAddresses = new HashSet<string>(); // Using a HashSet here for auto de-deduplication

            foreach (var emailAddress in defaultNotificationSettings.Email)
            {
                emailAddresses.Add(emailAddress);
            }

            foreach (var scheduledTime in matchedScheduleTimes)
            {
                var scheduleNotifications = scheduledTime.Notifications;
                if (scheduleNotifications is null)
                    continue;

                foreach (var emailAddress in scheduleNotifications.Email)
                {
                    if (emailAddresses.Contains(emailAddress))
                        continue;

                    emailAddresses.Add(emailAddress);
                }
            }



            string? dataUri = null;
            try
            {
                var reviewId = detection.After.Id;

                var gifBytes = await frigateApiClient.FetchPreviewGifAsync(reviewId);

                // Convert GIF to base64 string
                var base64Gif = Convert.ToBase64String(gifBytes);

                // Create a data URI for the GIF
                dataUri = $"data:image/gif;base64,{base64Gif}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to get preview gif: {ex.Message}");
            }

            foreach (var emailAddress in emailAddresses)
            {
                try
                {
                    var address = new MailAddress(emailAddress);
                    var mailMessage = CreateDetectionEmail(detection, address, dataUri);

                    Console.WriteLine($"Sending email notification to {emailAddress}.");
                    smtpSender.Send(mailMessage);
                    Console.WriteLine($"Sent email notification to {emailAddress}.");

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to send email to {emailAddress}: {ex.Message}");
                }
            }
        }

        private MailMessage CreateDetectionEmail(DetectionEvent detection, MailAddress address, string? previewImageDataUri)
        {
            var evt = detection.Before;

            var textInfo = CultureInfo.CurrentCulture.TextInfo;


            var camera = evt.Camera;
            var label = textInfo.ToTitleCase(string.Join(", ", evt.Data.Objects));
            var time = DateTimeExtensions.FromFractionalUnixTimes(evt.StartTime).ToOffset(TimeSpan.FromHours(10));

            var cameraName = textInfo.ToTitleCase(camera.SnakeCaseToWords());

            var subject = $"{label} detected on {cameraName}";

            var timeText = $"{time:h:mm tt}";
            var dateText = $"{time:d MMM yyyy}";
            // var timezoneText = time.ToString("K", CultureInfo.InvariantCulture); // This is throwing a format exception. Can't be bothered trying to sort it out, I tried a bit but it was too much trouble.

            var body = new StringBuilder();
            body.AppendLine($"<p>Frigate detected a <b>{label}</b> on the camera '<b>{cameraName}</b>' at {timeText}, {dateText}.</p>");
            if (previewImageDataUri is not null)
            {
                body.AppendLine($"<img src=\"{previewImageDataUri}\" />");
            }
            body.AppendLine($"<p>Review the camera feed at <a href=\"https://frigate.bigtyre.com.au/\">frigate.bigtyre.com.au</a>.</p>");

            var message = new MailMessage
            {
                From = emailSenderAddress,
                Subject = subject,
                IsBodyHtml = true,
                Body = body.ToString()
            };
            message.To.Add(address);

            return message;
        }
    }
}