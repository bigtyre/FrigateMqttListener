namespace MqttListener
{
    record DetectionEvent(ReviewEvent Before, ReviewEvent After, string Type);
}