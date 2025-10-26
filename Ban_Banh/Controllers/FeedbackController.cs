using System.Text.RegularExpressions;
using Ban_Banh.Data;
using Ban_Banh.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ban_Banh.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FeedbackController : ControllerBase
{
    private readonly FeedbackDbContext _db;
    private readonly IWebHostEnvironment _env;

    // config
    private const int MAX_FILES = 5;
    private const int MAX_MB_PER_FILE = 5;

    public FeedbackController(FeedbackDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    public class FeedbackCreateDto
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public int? Rating { get; set; }
        public string? Message { get; set; }
        public List<IFormFile>? Files { get; set; }
    }

    [HttpPost]
    [RequestSizeLimit(20_000_000)] // 20 MB
    public async Task<IActionResult> Post([FromForm] FeedbackCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Message))
            return BadRequest(new { error = "Vui lòng nhập nội dung góp ý." });

        var rating = dto.Rating ?? 5;
        if (rating < 1 || rating > 5) rating = 5;

        var fb = new Feedback
        {
            Name = dto.Name?.Trim(),
            Email = dto.Email?.Trim(),
            Rating = rating,
            Message = dto.Message!.Trim()
        };
        _db.Feedbacks.Add(fb);
        await _db.SaveChangesAsync();

        // Save images
        var files = dto.Files ?? new List<IFormFile>();
        if (files.Count > MAX_FILES)
            return BadRequest(new { error = $"Chỉ được chọn tối đa {MAX_FILES} ảnh." });

        var savedImages = new List<FeedbackImage>();
        var webRoot = _env.WebRootPath ?? "wwwroot";
        var uploadRoot = Path.Combine(webRoot, "uploads", "feedback", fb.Id.ToString());
        Directory.CreateDirectory(uploadRoot);

        foreach (var file in files)
        {
            if (file.Length == 0) continue;
            if (file.Length > MAX_MB_PER_FILE * 1024L * 1024L)
                return BadRequest(new { error = $"Ảnh {file.FileName} vượt quá {MAX_MB_PER_FILE}MB." });

            if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { error = $"Tập tin {file.FileName} không phải ảnh." });

            var ext = Path.GetExtension(file.FileName);
            var safeName = MakeSafeFileName(Path.GetFileNameWithoutExtension(file.FileName));
            var newName = $"{safeName}_{Guid.NewGuid():N}{ext}";
            var savePath = Path.Combine(uploadRoot, newName);

            using (var stream = System.IO.File.Create(savePath))
                await file.CopyToAsync(stream);

            var url = $"/uploads/feedback/{fb.Id}/{newName}";
            savedImages.Add(new FeedbackImage
            {
                FeedbackId = fb.Id,
                FileName = newName,
                ContentType = file.ContentType,
                Size = file.Length,
                Url = url
            });
        }

        if (savedImages.Count > 0)
        {
            _db.FeedbackImages.AddRange(savedImages);
            await _db.SaveChangesAsync();
        }

        return Ok(new
        {
            id = fb.Id,
            name = fb.Name,
            email = fb.Email,
            rating = fb.Rating,
            message = fb.Message,
            createdAt = fb.CreatedAt,
            images = savedImages.Select(i => new { i.Url, i.FileName, i.Size, i.ContentType })
        });
    }

    [HttpGet]
    public async Task<IActionResult> Count() => Ok(new { ok = true, count = await _db.Feedbacks.CountAsync() });

    private static string MakeSafeFileName(string name)
    {
        name = name.Trim();
        name = Regex.Replace(name, @"[^a-zA-Z0-9\-_]+", "-");
        if (string.IsNullOrEmpty(name)) name = "img";
        return name.Length > 40 ? name[..40] : name;
    }
}
