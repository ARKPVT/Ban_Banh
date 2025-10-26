using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Ban_Banh.Models.Chat;

namespace Ban_Banh.Helpers
{
    public class ChatProvider
    {
        private readonly IHttpClientFactory _httpFactory;
        private readonly IConfiguration _cfg;

        public ChatProvider(IHttpClientFactory httpFactory, IConfiguration cfg)
        {
            _httpFactory = httpFactory;
            _cfg = cfg;
        }

        public async Task<ChatResponse> SendAsync(ChatRequest request, CancellationToken ct = default)
        {
            // --- Read config ---
            var provider = (_cfg["AI:Provider"] ?? "GOOGLE").ToUpperInvariant(); // GOOGLE | OPENAI
            var model = request.Model
                        ?? _cfg["AI:Model"]
                        ?? (provider == "GOOGLE" ? "gemini-2.5-flash" : "gpt-4o-mini");

            var apiKey = (_cfg["AI:ApiKey"] ?? string.Empty).Trim();
            var apiUrl = (_cfg["AI:ApiUrl"] ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(apiKey))
                throw new Exception("AI:ApiKey is empty. Hãy thiết lập AI:ApiKey trong appsettings hoặc user-secrets.");

            var http = _httpFactory.CreateClient();

            if (provider == "GOOGLE")
            {
                // Map OpenAI-style messages -> Gemini contents
                object? systemParts = null;
                var contents = new List<object>();
                foreach (var m in request.Messages)
                {
                    if (m.Role == "system" && systemParts == null)
                    {
                        systemParts = new { parts = new[] { new { text = m.Content ?? "" } } };
                        continue;
                    }
                    var role = m.Role == "assistant" ? "model" : "user";
                    contents.Add(new { role, parts = new[] { new { text = m.Content ?? "" } } });
                }

                var baseUrl = string.IsNullOrWhiteSpace(apiUrl)
                    ? "https://generativelanguage.googleapis.com/v1beta/models"
                    : apiUrl.TrimEnd('/');

                var endpoint = $"{baseUrl}/{model}:generateContent";

                var body = new Dictionary<string, object?>
                {
                    ["contents"] = contents,
                    ["generationConfig"] = new { temperature = request.Temperature ?? 0.7 }
                };
                if (systemParts != null) body["systemInstruction"] = systemParts;

                var payload = JsonSerializer.Serialize(body);

                // Send key via header (ổn định hơn)
                using var httpReq = new HttpRequestMessage(HttpMethod.Post, endpoint);
                httpReq.Headers.Add("x-goog-api-key", apiKey);
                httpReq.Content = new StringContent(payload, Encoding.UTF8, "application/json");

                using var httpResp = await http.SendAsync(httpReq, ct);
                var json = await httpResp.Content.ReadAsStringAsync(ct);
                if (!httpResp.IsSuccessStatusCode)
                    throw new Exception($"Gemini error: {json}");

                using var doc = JsonDocument.Parse(json);
                var parts = doc.RootElement.GetProperty("candidates")[0]
                                           .GetProperty("content")
                                           .GetProperty("parts");
                var text = string.Join("", parts.EnumerateArray().Select(p => p.GetProperty("text").GetString() ?? ""));
                return new ChatResponse { Reply = text, Raw = JsonSerializer.Deserialize<object>(json) };
            }
            else
            {
                // OPENAI-style
                var url = (apiUrl.Length == 0 ? "https://api.openai.com/v1/chat/completions" : apiUrl).TrimEnd('/');
                http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                var body = new
                {
                    model,
                    messages = request.Messages.Select(m => new { role = m.Role, content = m.Content }),
                    temperature = request.Temperature ?? 0.7,
                    stream = false
                };

                var payload = JsonSerializer.Serialize(body);
                using var resp = await http.PostAsync(url, new StringContent(payload, Encoding.UTF8, "application/json"), ct);
                var json = await resp.Content.ReadAsStringAsync(ct);
                if (!resp.IsSuccessStatusCode)
                    throw new Exception($"OpenAI error: {json}");

                using var doc = JsonDocument.Parse(json);
                var text = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
                return new ChatResponse { Reply = text, Raw = JsonSerializer.Deserialize<object>(json) };
            }
        }
    }
}
