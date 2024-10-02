namespace MqttListener
{
    class AppSettings
    {
        public SmtpSettings Smtp { get; } = new();
        public MqttSettings Mqtt { get; } = new();

        public List<NotificationSchedule> Schedule { get; } = [];

        public int NotificationIntervalSeconds { get; set; } = 300;
    }
}