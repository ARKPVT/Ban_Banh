using Microsoft.AspNetCore.Mvc;
using Ban_Banh.Helpers;
using Ban_Banh.Models.Chat;  // <-- đảm bảo có dòng này

namespace Ban_Banh.Controllers
{
    // Controllers/ChatApiController.cs
    [ApiController]
    [Route("api/chat")]       // ✅ cố định path
    public class ChatApiController : ControllerBase
    {
        private readonly ChatProvider _provider;
        public ChatApiController(ChatProvider provider) => _provider = provider;

        [HttpPost]             // ✅ duy nhất 1 action POST
        public async Task<ActionResult<ChatResponse>> Post([FromBody] Ban_Banh.Models.Chat.ChatRequest request)
        {
            if (request?.Messages == null || request.Messages.Count == 0)
                return BadRequest(new { error = "messages is required" });

            var result = await _provider.SendAsync(request);
            return Ok(result);
        }
    }
}
