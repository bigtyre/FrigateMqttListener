namespace MqttListener
{
    class AppSettings
    {
        public SmtpSettings Smtp { get; } = new();
        public MqttSettings Mqtt { get; } = new();

        public List<NotificationSchedule> Schedule { get; } = [];

        public int NotificationIntervalSeconds { get; set; } = 300;

        public NotificationSettings Notifications { get; } = new();

        public FrigateSettings Frigate { get; } = new();
    }

    class NotificationSettings
    {
        public List<string> Email { get; set; } = [];
    }

    class FrigateSettings
    {
        public string? Url { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
    }
}