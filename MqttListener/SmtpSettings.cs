namespace MqttListener
{
    class SmtpSettings
    {
        public string? Sender { get; set; }
        public string? Host { get; set; }
        public int Port { get; set; } = 587;
        public bool UseSSL { get; set; } = true;
        public string? Username { get; set; }
        public string? Password { get; set; }
    }
}