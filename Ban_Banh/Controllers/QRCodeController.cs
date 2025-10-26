using Ban_Banh.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;

namespace Ban_Banh.Controllers
{
    [Authorize(Policy = "KhoOnly")]
    [Route("[controller]")]
    public class QRCodeController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;

        public QRCodeController(IConfiguration configuration, IWebHostEnvironment env)
        {
            _configuration = configuration;
            _env = env;
        }

        // Gọi URL: /QRCode/Generate?inventoryId=12
        [HttpGet("Generate")]
        public IActionResult Generate(int inventoryId)
        {
            if (inventoryId <= 0)
                return BadRequest("InventoryId không hợp lệ.");

            var qrHelper = new QRHelper(_configuration);
            qrHelper.GenerateQRCodeForBatch(inventoryId);

            return Ok($"Đã tạo QR cho lô hàng {inventoryId}. Thư mục lưu tại wwwroot/QRCode/LO_{inventoryId:D5}");
        }

        // GET /QRCode/List?inventoryId=12
        // Trả về danh sách file QR (url tuyệt đối để hiển thị trong view)
        [HttpGet("List")]
        public IActionResult List(int inventoryId)
        {
            if (inventoryId <= 0) return BadRequest("InventoryId không hợp lệ.");

            var dir = Path.Combine(_env.WebRootPath, "QRCode", $"LO_{inventoryId:D5}");
            if (!Directory.Exists(dir)) return NotFound("Chưa có QR cho lô này.");

            var files = Directory.GetFiles(dir)
                                 .Where(p => p.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                                          || p.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                                          || p.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
                                 .Select(p => new {
                                     name = Path.GetFileName(p),
                                     url = $"/QRCode/LO_{inventoryId:D5}/{Path.GetFileName(p)}"
                                 })
                                 .ToList();

            return Json(files);
        }

        // GET /QRCode/DownloadZip?inventoryId=12
        [HttpGet("DownloadZip")]
        public IActionResult DownloadZip(int inventoryId)
        {
            if (inventoryId <= 0) return BadRequest("InventoryId không hợp lệ.");

            var dir = Path.Combine(_env.WebRootPath, "QRCode", $"LO_{inventoryId:D5}");
            if (!Directory.Exists(dir)) return NotFound("Chưa có QR cho lô này.");

            // Nén thư mục vào stream trả về
            using var ms = new MemoryStream();
            using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
            {
                foreach (var file in Directory.GetFiles(dir))
                {
                    var entry = zip.CreateEntry(Path.GetFileName(file), CompressionLevel.Fastest);
                    using var entryStream = entry.Open();
                    using var fileStream = System.IO.File.OpenRead(file);
                    fileStream.CopyTo(entryStream);
                }
            }
            ms.Position = 0;

            var zipName = $"LO_{inventoryId:D5}_QR.zip";
            return File(ms, "application/zip", zipName);
        }

        // GET /QRCode/Download?inventoryId=12&fileName=QR_001.png
        // Tải từng file riêng lẻ (có kiểm tra an toàn tên file)
        [HttpGet("Download")]
        public IActionResult Download(int inventoryId, string fileName)
        {
            if (inventoryId <= 0) return BadRequest("InventoryId không hợp lệ.");
            if (string.IsNullOrWhiteSpace(fileName)) return BadRequest("Thiếu tên file.");

            // Chặn path traversal
            if (fileName.Contains("..") || fileName.Contains('/') || fileName.Contains('\\'))
                return BadRequest("Tên file không hợp lệ.");

            // Cho phép tên file chỉ gồm chữ, số, gạch dưới, dấu chấm, gạch nối
            var okName = Regex.IsMatch(fileName, @"^[A-Za-z0-9_\-\.]+$");
            if (!okName) return BadRequest("Tên file không hợp lệ.");

            var dir = Path.Combine(_env.WebRootPath, "QRCode", $"LO_{inventoryId:D5}");
            var fullPath = Path.Combine(dir, fileName);

            if (!System.IO.File.Exists(fullPath)) return NotFound("Không tìm thấy file.");

            var contentType = "application/octet-stream";
            if (fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) contentType = "image/png";
            else if (fileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || fileName.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)) contentType = "image/jpeg";

            var bytes = System.IO.File.ReadAllBytes(fullPath);
            return File(bytes, contentType, fileName);
        }
    }
}
