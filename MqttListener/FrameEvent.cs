using Newtonsoft.Json;

namespace MqttListener
{
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
}