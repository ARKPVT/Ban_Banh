using Ban_Banh.Data;
using Ban_Banh.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ban_Banh.Controllers
{
    // Form nhận từ client (multipart/form-data)
    public class FeedbackForm
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public int Rating { get; set; } = 5;
        public string Message { get; set; } = "";
        public List<IFormFile>? Files { get; set; } = new();
    }

    [ApiController]
    [Route("api/[controller]")] // => /api/feedback
    public class FeedbackController : ControllerBase
    {
        private readonly FeedbackDbContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<FeedbackController> _logger;

        public FeedbackController(
            FeedbackDbContext db,
            IWebHostEnvironment env,
            ILogger<FeedbackController> logger)
        {
            _db = db;
            _env = env;
            _logger = logger;
        }

        // GET /api/feedback/debug (để kiểm tra nhanh DB)
        [HttpGet("debug")]
        public async Task<IActionResult> Debug()
        {
            var count = await _db.Feedback.CountAsync();
            var imgCount = await _db.FeedbackImage.CountAsync();
            return Ok(new { ok = true, feedback = count, images = imgCount });
        }

        // POST /api/feedback
        [HttpPost]
        [RequestSizeLimit(20 * 1024 * 1024)]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Post([FromForm] FeedbackForm form)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(form?.Message))
                    return BadRequest(new { error = "Message is required" });

                var fb = new Feedback
                {
                    Name = form.Name?.Trim(),
                    Email = form.Email?.Trim(),
                    Rating = form.Rating <= 0 ? 5 : form.Rating,
                    Message = form.Message?.Trim(),
                    CreatedAt = DateTime.UtcNow,
                };

                // Thư mục lưu ảnh
                var root = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var saveDir = Path.Combine(root, "uploads", "feedback");
                Directory.CreateDirectory(saveDir);

                var files = form.Files ?? new List<IFormFile>();
                foreach (var f in files)
                {
                    if (f.Length == 0) continue;
                    if (!f.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)) continue;

                    var ext = Path.GetExtension(f.FileName);
                    var safeName = $"{Guid.NewGuid():N}{ext}";
                    var fullPath = Path.Combine(saveDir, safeName);

                    using (var fs = System.IO.File.Create(fullPath))
                        await f.CopyToAsync(fs);

                    var relPath = $"/uploads/feedback/{safeName}";
                    fb.Images.Add(new FeedbackImage
                    {
                        FileName = Path.GetFileName(f.FileName),
                        FilePath = relPath,            // << thay Url bằng FilePath
                        FileSize = f.Length,
                        ContentType = f.ContentType
                    });
                }

                _db.Feedback.Add(fb);
                await _db.SaveChangesAsync();

                return Ok(new { ok = true, id = fb.Id, saved = fb.Images.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "POST /api/feedback failed");
                return StatusCode(500, new { error = ex.Message, stack = ex.StackTrace });
            }
        }
    }
}
