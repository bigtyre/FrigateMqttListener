namespace MqttListener
{
    class NotificationSchedule
    {
        public string Cameras { get; set; } = ".*";

        public List<DayOfWeek> WeekDays { get; set; } = [];

        public int StartHour { get; set; } = 0;
        public int StartMinute { get; set; } = 0;

        public int EndHour { get; set; } = 24;
        public int EndMinute { get; set; } = 0;

        public NotificationSettings Notifications { get; } = new();
    }
}