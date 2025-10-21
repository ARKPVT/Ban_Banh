using Ban_Banh.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Ban_Banh.Controllers
{
    public class QRCodeController : Controller
    {
        private readonly IConfiguration _configuration;

        public QRCodeController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // Gọi URL: /QRCode/Generate?inventoryId=12
        [HttpGet]
        public IActionResult Generate(int inventoryId)
        {
            if (inventoryId <= 0)
                return BadRequest("InventoryId không hợp lệ.");

            var qrHelper = new QRHelper(_configuration);
            qrHelper.GenerateQRCodeForBatch(inventoryId);

            return Ok($"Đã tạo QR cho lô hàng {inventoryId}. Thư mục lưu tại wwwroot/QRCode/LO_{inventoryId:D5}");
        }
    }
}
