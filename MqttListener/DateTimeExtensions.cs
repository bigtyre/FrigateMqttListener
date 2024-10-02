namespace MqttListener
{
    public static class DateTimeExtensions
    {
        public static DateTimeOffset FromFractionalUnixTimes(double time)
        {
            // Separate the integer part (seconds) and the fractional part (milliseconds)
            long seconds = (long)time;
            //double fractionalSeconds = time - seconds;

            // Convert seconds to DateTimeOffset
            DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(seconds);

            // Add the fractional seconds (converted to milliseconds)
            //dateTimeOffset = dateTimeOffset.AddMilliseconds(fractionalSeconds * 1000);

            return dateTimeOffset;
        }
    }
}