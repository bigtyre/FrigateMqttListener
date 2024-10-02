namespace MqttListener
{
    class MqttSettings
    {
        public string? BrokerAddress { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string DetectionTopic { get; set; } = "frigate/events";
    }
}