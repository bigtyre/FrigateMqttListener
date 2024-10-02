using Newtonsoft.Json;

namespace MqttListener
{
    record FrameEvent(
        string Id,
        string Camera,
        string Label,
        string? Thumbnail,
        [JsonProperty("end_time")] double? EndTime,
        [JsonProperty("frame_time")] double FrameTime,
        [JsonProperty("top_score")] double TopScore,
        [JsonProperty("false_positive")] bool FalsePositive,
        [JsonProperty("has_snapshot")] bool HasSnapshot,
        [JsonProperty("has_clip")] bool HasClip,
        [JsonProperty("stationary")] bool Stationary,
        Dictionary<string, float>? Attributes
    );

    record ReviewEvent(
        string Id,
        string Camera,
        [JsonProperty("start_time")] double StartTime,
        [JsonProperty("end_time")] double? EndTime,
        [JsonProperty("thumb_path")] string Thumbpath,
        ReviewData Data
    );

    record ReviewData(
        List<string> Objects
    );
}