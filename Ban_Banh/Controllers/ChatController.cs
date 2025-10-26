using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace Ban_Banh.Controllers;

public record ChatMessage(string role, string content);
public record ChatRequest(List<ChatMessage> messages, string? model, double? temperature);

[ApiController]
[Route("chat")]
public class ChatController : ControllerBase
{
    private readonly IHttpClientFactory _http;
    private readonly IConfiguration _cfg;

    public ChatController(IHttpClientFactory http, IConfiguration cfg)
    {
        _http = http; _cfg = cfg;
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] ChatRequest req)
    {
        if (req?.messages is null || req.messages.Count == 0)
            return BadRequest(new { error = "messages phải là mảng [{role, content}]" });

        var provider = (_cfg["AI:Provider"] ?? "GOOGLE").ToUpperInvariant();
        var model = req.model ?? _cfg["AI:Model"] ?? (provider == "GOOGLE" ? "gemini-1.5-flash" : "gpt-4o-mini");
        var apiKey = _cfg["AI:ApiKey"];
        var apiUrl = _cfg["AI:ApiUrl"];

        if (string.IsNullOrWhiteSpace(apiKey))
            return StatusCode(500, new { error = "Chưa thiết lập AI:ApiKey trong appsettings" });

        var http = _http.CreateClient();

        if (provider == "GOOGLE")
        {
            // Map OpenAI-style -> Gemini
            object? systemParts = null;
            var contents = new List<object>();
            foreach (var m in req.messages)
            {
                if (m.role == "system" && systemParts == null)
                {
                    systemParts = new { parts = new[] { new { text = m.content ?? "" } } };
                    continue;
                }
                var role = m.role == "assistant" ? "model" : "user";
                contents.Add(new { role, parts = new[] { new { text = m.content ?? "" } } });
            }

            var url = $"{(apiUrl ?? throw new InvalidOperationException("AI:ApiUrl is not configured")).TrimEnd('/')}/{model}:generateContent?key={apiKey}";
            var body = new Dictionary<string, object?>
            {
                ["contents"] = contents,
                ["generationConfig"] = new { temperature = req.temperature ?? 0.7 }
            };
            if (systemParts != null) body["systemInstruction"] = systemParts;

            var str = JsonSerializer.Serialize(body);
            var resp = await http.PostAsync(url, new StringContent(str, Encoding.UTF8, "application/json"));
            var json = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) return StatusCode((int)resp.StatusCode, new { error = "Upstream error", detail = json });

            using var doc = JsonDocument.Parse(json);
            var parts = doc.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts");
            var reply = string.Join("", parts.EnumerateArray().Select(p => p.GetProperty("text").GetString() ?? ""));
            return Ok(new { reply, raw = JsonDocument.Parse(json).RootElement });
        }
        else
        {
            // OPENAI-style
            var url = $"{apiUrl?.TrimEnd('/') ?? "https://api.openai.com/v1/chat/completions"}";
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var body = new
            {
                model,
                messages = req.messages,
                temperature = req.temperature ?? 0.7,
                stream = false
            };
            var str = JsonSerializer.Serialize(body);
            var resp = await http.PostAsync(url, new StringContent(str, Encoding.UTF8, "application/json"));
            var json = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) return StatusCode((int)resp.StatusCode, new { error = "Upstream error", detail = json });

            using var doc = JsonDocument.Parse(json);
            var reply = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
            return Ok(new { reply, raw = JsonDocument.Parse(json).RootElement });
        }
    }
}
