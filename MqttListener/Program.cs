using Microsoft.Extensions.Configuration;
using MqttListener;
using Newtonsoft.Json;
using System.Globalization;
using System.Text.RegularExpressions;
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
string brokerAddress = mqttSettings.BrokerAddress ?? throw new Exception($"{nameof(settings.Mqtt)}.{nameof(mqttSettings.BrokerAddress)} not configured.");
string topic = mqttSettings.DetectionTopic ?? throw new Exception($"{nameof(settings.Mqtt)}.{nameof(mqttSettings.DetectionTopic)} not configured.");
var mqttUsername = mqttSettings.Username;
var mqttPassword = mqttSettings.Password;

var smtpSender = new SmtpMailSender(settings.Smtp);

var frigateSettings = settings.Frigate;
var frigateUrl = frigateSettings.Url ?? throw new Exception("Frigate API Url not configured.");
var frigateUsername = frigateSettings.Username ?? throw new Exception("Frigate API username not configured.");
var frigatePassword = frigateSettings.Password ?? throw new Exception("Frigate API password not configured.");
var frigateApiClient = new FrigateApiClient(frigateUrl, frigateUsername, frigatePassword);

var notificationSender = new NotificationSender(
    settings.NotificationIntervalSeconds, 
    smtpSender, 
    settings.Notifications, 
    smtpSender.DefaultSender, 
    frigateApiClient
);


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


while (true)
{
    Console.WriteLine("MQTT Connecting");

    try
    {
        // Connect to the broker
        string clientId = "frigate-event-listener-" + Guid.NewGuid().ToString();
        if (mqttUsername != null)
        {
            client.Connect(clientId, mqttUsername, mqttPassword ?? "");
        }
        else
        {
            client.Connect(clientId);
        }

        // Subscribe to the specified topic
        client.Subscribe([topic], [MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE]);

        Console.WriteLine("MQTT Connected.");
        Console.WriteLine($"Listening to topic: {topic}");

        while (client.IsConnected)
        {
            await Task.Delay(500);
        }

        client.Disconnect();
    }
    catch (Exception)
    {

    }

    Console.WriteLine($"Connection lost. Waiting 2 seconds before reconnecting");
    await Task.Delay(TimeSpan.FromSeconds(2));
}

// Disconnect the client when done

void Client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
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

async void HandleDetection(DetectionEvent detection)
{
    try
    {
        var evt = detection.After;
        var camera = evt.Camera;

        var detectionType = detection.Type;
        Console.WriteLine($"Detection: {detectionType}.");
        if (detection.Type != "end")
        {
            return;
        }

        var eventId = evt.Id;
        var endTime = evt.EndTime;
        if (endTime is null)
        {
            Console.WriteLine($"Event ID {eventId} in progress on camera '{camera}'. End time not published");
            return;
        }

        Console.WriteLine($"Event {eventId} ended on '{camera}'. End time {endTime}. Snapshot: {evt.Thumbpath}");


        var label = string.Join(", ", evt.Data.Objects);
        var timestamp = evt.StartTime;

        var time = DateTimeExtensions.FromFractionalUnixTimes(timestamp).ToOffset(TimeSpan.FromHours(10));

        var textInfo = CultureInfo.CurrentCulture.TextInfo;

        var labelInTitleCase = textInfo.ToTitleCase(label);

        var schedule = settings.Schedule;
        bool shouldNotify = true;
        IEnumerable<NotificationSchedule> matchedScheduleTimes = [];
        if (schedule.Count > 0)
        {
            matchedScheduleTimes = GetMatchedNotificationPeriods(camera, time);
            shouldNotify = matchedScheduleTimes.Any();
        }

        Console.WriteLine($"{time} {labelInTitleCase} detected on {camera} with confidence. Notify: {(shouldNotify ? "Yes" : "No")}");

        if (!shouldNotify)
            return;

        await notificationSender.SendNotificationAsync(detection, matchedScheduleTimes);
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.ToString());
    }
}

IEnumerable<NotificationSchedule> GetMatchedNotificationPeriods(string camera, DateTimeOffset time)
{
    var scheduleRows = settings.Schedule;

    foreach (var schedule in scheduleRows)
    {
        if (Regex.IsMatch(camera, schedule.Cameras) is false)
            continue;

        if (schedule.WeekDays.Count > 0 && schedule.WeekDays.Contains(time.DayOfWeek) is false)
            continue;

        if (time.Hour < schedule.StartHour)
            continue;

        if (time.Hour == schedule.StartHour && time.Minute < schedule.StartMinute)
            continue;

        if (time.Hour > schedule.EndHour)
            continue;

        if (time.Hour == schedule.EndHour && time.Minute > schedule.EndMinute)
            continue;

        yield return schedule;
    }
}

