// MQTT Broker URL (e.g., local broker or cloud broker)
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Globalization;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

var configBuilder = new ConfigurationBuilder();
configBuilder.AddEnvironmentVariables();
configBuilder.AddKeyPerFile("/var/run/secrets", optional: true);
#if DEBUG
configBuilder.AddUserSecrets<Program>();
#endif
var config = configBuilder.Build();
var settings = new AppSettings();
config.Bind(settings);


var mqttSettings = settings.Mqtt;
//bnet.bigtyre.com.au
string brokerAddress = mqttSettings.BrokerAddress ?? throw new Exception($"{nameof(settings.Mqtt)}.{nameof(mqttSettings.BrokerAddress)} not configured.");
string topic = mqttSettings.DetectionTopic ?? throw new Exception($"{nameof(settings.Mqtt)}.{nameof(mqttSettings.DetectionTopic)} not configured.");
var mqttUsername = mqttSettings.Username;
var mqttPassword = mqttSettings.Password;
//string topic = "frigate/events"; // Topic to subscribe to

// Create an MQTT client
var client = new MqttClient(brokerAddress);

// Register to message received event
client.MqttMsgPublishReceived += Client_MqttMsgPublishReceived;
client.ConnectionClosed += Client_ConnectionClosed;
client.MqttMsgUnsubscribed += Client_MqttMsgUnsubscribed;

static void Client_MqttMsgUnsubscribed(object sender, MqttMsgUnsubscribedEventArgs e)
{
    Console.WriteLine("MQTT unsubscribed");
};

static void Client_ConnectionClosed(object sender, EventArgs e)
{
    Console.WriteLine("MQTT connection closed");
}

// Connect to the broker
string clientId = Guid.NewGuid().ToString();
if (mqttUsername != null)
{
    client.Connect(clientId, mqttUsername, mqttPassword ?? "");
}
else
{
    client.Connect(clientId);
}

if (!client.IsConnected)
{
    Console.WriteLine("Failed to connect.");
    client.Disconnect();
    return;
}
else
{
    Console.WriteLine("Connected successfully.");
}

// Subscribe to the specified topic
client.Subscribe([topic], [MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE]);

Console.WriteLine($"Listening to topic: {topic}");

while(true)
{
    await Task.Delay(-1);
}

// Disconnect the client when done
client.Disconnect();

static void Client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
{
    string message = System.Text.Encoding.UTF8.GetString(e.Message);
   // Console.WriteLine($"Message received on topic {e.Topic}: {message}");

    try
    {
        var evt = JsonConvert.DeserializeObject<DetectionEvent>(message);
        if (evt is not null)
        {
            HandleDetection(evt);
        }
    }
    catch (JsonSerializationException)
    {

    }
}

static void HandleDetection(DetectionEvent detection)
{
    var evt = detection.After;

    var camera = evt.Camera;
    var label = evt.Label;
    var timestamp = evt.FrameTime;

    var time = FromFractionalUnixTimes(timestamp).ToOffset(TimeSpan.FromHours(10));

    var textInfo = CultureInfo.CurrentCulture.TextInfo;

    var labelInTitleCase = textInfo.ToTitleCase(label);

    Console.WriteLine($"{time} {labelInTitleCase} detected on {camera}");
}

static DateTimeOffset FromFractionalUnixTimes(double time)
{
    // Separate the integer part (seconds) and the fractional part (milliseconds)
    long seconds = (long)time;
    double fractionalSeconds = time - seconds;

    // Convert seconds to DateTimeOffset
    DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(seconds);

    // Add the fractional seconds (converted to milliseconds)
    dateTimeOffset = dateTimeOffset.AddMilliseconds(fractionalSeconds * 1000);

    return dateTimeOffset;
}

record DetectionEvent(FrameEvent Before, FrameEvent After);

record FrameEvent(
    string Camera,
    [JsonProperty("frame_time")]
    double FrameTime, 
    string Label, 
    [JsonProperty("top_score")]
    double TopScore,
    [JsonProperty("false_positive")]
    bool FalsePositive
);

class MqttSettings
{
    public string? BrokerAddress { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string DetectionTopic { get; set; } = "frigate/events";
}

class AppSettings
{
    public MqttSettings Mqtt { get; } = new();
}