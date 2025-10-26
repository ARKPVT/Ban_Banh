using System;
using System.Data;
using System.Drawing;
using System.IO;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using QRCoder; // cần cài gói: dotnet add package QRCoder

namespace Ban_Banh.Helpers
{
    public class QRHelper
    {
        private readonly string _connectionString;
        private readonly string _rootPath;

        // ✅ Constructor 1: dùng IConfiguration (chuẩn ASP.NET Core)
        public QRHelper(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("BanBanhDB");
            _rootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "QRCode");
        }

        // ✅ Constructor 2: dùng connection string trực tiếp (linh hoạt)
        public QRHelper(string connectionString)
        {
            _connectionString = connectionString;
            _rootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "QRCode");
        }

        /// <summary>
        /// Tạo thư mục theo mã lô (InventoryId) và sinh QR cho từng ProductInstance tương ứng.
        /// </summary>
        public void GenerateQRCodeForBatch(int inventoryId)
        {
            // Đảm bảo thư mục gốc tồn tại
            Directory.CreateDirectory(_rootPath);

            // Tạo thư mục riêng cho lô hàng
            string batchFolder = Path.Combine(_rootPath, $"LO_{inventoryId:D5}");
            Directory.CreateDirectory(batchFolder);

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                string query = "SELECT Id, SerialNumber FROM ProductInstance WHERE InventoryId = @InventoryId";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@InventoryId", inventoryId);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            long productId = reader.GetInt64(0);
                            string serial = reader.GetString(1);

                            string filePath = Path.Combine(batchFolder, $"{serial}.jpg");
                            GenerateQRCodeImage(serial, filePath);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Sinh ảnh QR code từ nội dung và lưu ra file.
        /// </summary>
        private void GenerateQRCodeImage(string content, string savePath)
        {
            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            using (QRCodeData qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q))
            using (QRCode qrCode = new QRCode(qrCodeData))
            using (Bitmap qrCodeImage = qrCode.GetGraphic(20))
            {
                qrCodeImage.Save(savePath, System.Drawing.Imaging.ImageFormat.Jpeg);
            }
        }
    }
}
