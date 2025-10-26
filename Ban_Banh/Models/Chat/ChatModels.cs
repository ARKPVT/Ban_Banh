using System.Text.Json.Serialization;

namespace Ban_Banh.Models.Chat
{
    public class ChatMessage
    {
        // Hỗ trợ JSON từ frontend: { "role": "...", "content": "..." }
        [JsonPropertyName("role")] public string Role { get; set; } = "user";
        [JsonPropertyName("content")] public string Content { get; set; } = "";
    }

    public class ChatRequest
    {
        // Mảng messages là bắt buộc
        [JsonPropertyName("messages")] public List<ChatMessage> Messages { get; set; } = new();
        [JsonPropertyName("model")] public string? Model { get; set; }
        [JsonPropertyName("temperature")] public double? Temperature { get; set; }
    }

    public class ChatResponse
    {
        public string Reply { get; set; } = "";
        public object? Raw { get; set; }
    }
}
