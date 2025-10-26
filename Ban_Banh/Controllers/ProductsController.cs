using Ban_Banh.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ban_Banh.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class ProductsController : Controller
    {
        private readonly string _connectionString;
        private readonly ILogger<ProductsController> _logger;
        private readonly bool _showDetailedErrors; // cấu hình: chỉ bật trong dev

        public ProductsController(IConfiguration configuration, ILogger<ProductsController> logger)
        {
            _connectionString = configuration.GetConnectionString("BanBanhDB");
            _logger = logger;
            // Add "ShowDetailedSqlErrors": true cho appsettings.Development.json khi cần debug
            _showDetailedErrors = configuration.GetValue<bool>("ShowDetailedSqlErrors", false);
        }

        // 📋 Hiển thị danh sách sản phẩm
        public IActionResult Index()
        {
            var products = new List<Banh>();

            string lastSql = null;
            var lastParams = new List<KeyValuePair<string, object>>();

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    lastSql = @"
                        SELECT b.Id, b.TenBanh, b.MoTa, b.Gia, b.HinhAnh,
                               b.CategoryId, c.TenDanhMuc AS CategoryName
                        FROM Banh b
                        LEFT JOIN Category c ON b.CategoryId = c.Id
                        ORDER BY b.Id DESC";

                    using (SqlCommand cmd = new SqlCommand(lastSql, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            products.Add(new Banh
                            {
                                Id = (int)reader["Id"],
                                TenBanh = reader["TenBanh"].ToString(),
                                MoTa = reader["MoTa"]?.ToString(),
                                Gia = (decimal)reader["Gia"],
                                HinhAnh = reader["HinhAnh"]?.ToString(),
                                CategoryId = reader["CategoryId"] == DBNull.Value ? null : (int?)reader["CategoryId"],
                                CategoryName = reader["CategoryName"]?.ToString()
                            });
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                // Tạo chuỗi chi tiết lỗi
                var details = FormatSqlException(ex, lastSql, lastParams);
                // Log đầy đủ (luôn log chi tiết để tra cứu)
                _logger.LogError(ex, "SQL error when loading products. Details: {Details}", details);

                if (_showDetailedErrors)
                {
                    // Hiển thị chi tiết (chỉ bật trong dev)
                    ViewBag.Error = "❌ Lỗi truy vấn SQL: " + ex.Message;
                    ViewBag.SqlErrorDetails = details;
                }
                else
                {
                    // Hiển thị message gọn cho user + tạo mã tham chiếu để lookup trong log
                    var refId = Guid.NewGuid().ToString("N");
                    ViewBag.Error = $"❌ Lỗi truy vấn SQL. Mã tham chiếu: {refId}";
                    // Log cùng refId để operator có thể tìm chi tiết
                    _logger.LogError("ReferenceId: {RefId} - {Details}", refId, details);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error when loading products.");
                ViewBag.Error = "❌ Lỗi tải danh sách sản phẩm: " + ex.Message;
            }

            return View(products);
        }

        // 💾 Thêm hoặc sửa sản phẩm
        [HttpPost]
        public IActionResult Save(Banh model)
        {
            // --- NEW: xử lý ModelState chi tiết thay vì thông báo chung chung ---
            if (!ModelState.IsValid)
            {
                var errorList = ModelState
                    .Where(kvp => kvp.Value.Errors.Count > 0)
                    .SelectMany(kvp => kvp.Value.Errors.Select(err => new
                    {
                        Field = kvp.Key,
                        // nếu ErrorMessage rỗng thì lấy Exception message (nếu có)
                        Message = string.IsNullOrWhiteSpace(err.ErrorMessage) ? (err.Exception?.Message ?? "Lỗi không rõ") : err.ErrorMessage
                    }))
                    .ToList();

                var sb = new StringBuilder();
                sb.AppendLine("❌ Dữ liệu không hợp lệ. Vui lòng kiểm tra các trường sau:");
                foreach (var e in errorList)
                {
                    sb.AppendLine($"• {e.Field}: {e.Message}");
                }

                // Lưu vào TempData["Error"] để view hiện lên như trước (chỉ khác ở nội dung)
                TempData["Error"] = sb.ToString().Trim();
                return RedirectToAction("Index");
            }
            // --- END NEW ---

            string lastSql = null;
            var lastParams = new List<KeyValuePair<string, object>>();

            try
            {
                // Kiểm tra dữ liệu trống
                if (string.IsNullOrWhiteSpace(model.TenBanh))
                    throw new Exception("Tên bánh không được để trống.");

                if (model.Gia <= 0)
                    throw new Exception("Giá sản phẩm phải lớn hơn 0.");

                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    if (model.Id == 0)
                    {
                        lastSql = @"
                            INSERT INTO Banh (TenBanh, MoTa, Gia, HinhAnh, CategoryId)
                            VALUES (@TenBanh, @MoTa, @Gia, @HinhAnh, @CategoryId)";

                        using (SqlCommand cmd = new SqlCommand(lastSql, conn))
                        {
                            AddParameter(cmd, "@TenBanh", model.TenBanh, lastParams);
                            AddParameter(cmd, "@MoTa", (object?)model.MoTa ?? DBNull.Value, lastParams);
                            AddParameter(cmd, "@Gia", model.Gia, lastParams);
                            AddParameter(cmd, "@HinhAnh", (object?)model.HinhAnh ?? DBNull.Value, lastParams);
                            AddParameter(cmd, "@CategoryId", (object?)model.CategoryId ?? DBNull.Value, lastParams);

                            int rows = cmd.ExecuteNonQuery();
                            if (rows == 0)
                                throw new Exception("Không thể thêm sản phẩm (0 dòng bị ảnh hưởng).");
                        }

                        TempData["Success"] = "✅ Thêm sản phẩm thành công!";
                    }
                    else
                    {
                        lastSql = @"
                            UPDATE Banh
                            SET TenBanh = @TenBanh,
                                MoTa = @MoTa,
                                Gia = @Gia,
                                HinhAnh = @HinhAnh,
                                CategoryId = @CategoryId
                            WHERE Id = @Id";

                        using (SqlCommand cmd = new SqlCommand(lastSql, conn))
                        {
                            AddParameter(cmd, "@Id", model.Id, lastParams);
                            AddParameter(cmd, "@TenBanh", model.TenBanh, lastParams);
                            AddParameter(cmd, "@MoTa", (object?)model.MoTa ?? DBNull.Value, lastParams);
                            AddParameter(cmd, "@Gia", model.Gia, lastParams);
                            AddParameter(cmd, "@HinhAnh", (object?)model.HinhAnh ?? DBNull.Value, lastParams);
                            AddParameter(cmd, "@CategoryId", (object?)model.CategoryId ?? DBNull.Value, lastParams);

                            int rows = cmd.ExecuteNonQuery();
                            if (rows == 0)
                                throw new Exception($"Không tìm thấy sản phẩm có ID = {model.Id} để cập nhật.");
                        }

                        TempData["Success"] = "✅ Cập nhật sản phẩm thành công!";
                    }
                }
            }
            catch (SqlException ex)
            {
                var details = FormatSqlException(ex, lastSql, lastParams);
                _logger.LogError(ex, "SQL error when saving product. Details: {Details}", details);

                if (_showDetailedErrors)
                {
                    TempData["Error"] = "❌ Lỗi SQL: " + ex.Message;
                    TempData["SqlErrorDetails"] = details;
                }
                else
                {
                    var refId = Guid.NewGuid().ToString("N");
                    TempData["Error"] = $"❌ Lỗi SQL. Mã tham chiếu: {refId}";
                    _logger.LogError("ReferenceId: {RefId} - {Details}", refId, details);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error when saving product.");
                TempData["Error"] = "❌ " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        // 🗑️ Xóa sản phẩm
        [HttpPost]
        public IActionResult Delete(int id)
        {
            string lastSql = null;
            var lastParams = new List<KeyValuePair<string, object>>();

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    lastSql = "DELETE FROM Banh WHERE Id = @Id";

                    using (SqlCommand cmd = new SqlCommand(lastSql, conn))
                    {
                        AddParameter(cmd, "@Id", id, lastParams);
                        int rows = cmd.ExecuteNonQuery();

                        if (rows == 0)
                            throw new Exception($"Không tìm thấy sản phẩm có ID = {id} để xóa.");
                    }

                    TempData["Success"] = "🗑️ Đã xóa sản phẩm thành công!";
                }
            }
            catch (SqlException ex)
            {
                var details = FormatSqlException(ex, lastSql, lastParams);
                _logger.LogError(ex, "SQL error when deleting product. Details: {Details}", details);

                if (_showDetailedErrors)
                {
                    TempData["Error"] = "❌ Lỗi SQL: " + ex.Message;
                    TempData["SqlErrorDetails"] = details;
                }
                else
                {
                    var refId = Guid.NewGuid().ToString("N");
                    TempData["Error"] = $"❌ Lỗi SQL. Mã tham chiếu: {refId}";
                    _logger.LogError("ReferenceId: {RefId} - {Details}", refId, details);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error when deleting product.");
                TempData["Error"] = "❌ " + ex.Message;
            }

            return RedirectToAction("Index");
        }
        // 📋 Xem chi tiết của một bánh
        public IActionResult Details(int id)
        {
            var details = new List<BanhChiTiet>();
            string lastSql = null;
            var lastParams = new List<KeyValuePair<string, object>>();

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    lastSql = @"
                SELECT ct.Id, ct.BanhId, ct.MoTaChiTiet, ct.NguyenLieu, ct.HuongVi, ct.KichThuoc,
                       b.TenBanh
                FROM BanhChiTiet ct
                INNER JOIN Banh b ON b.Id = ct.BanhId
                WHERE ct.BanhId = @BanhId";

                    using (SqlCommand cmd = new SqlCommand(lastSql, conn))
                    {
                        AddParameter(cmd, "@BanhId", id, lastParams);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                details.Add(new BanhChiTiet
                                {
                                    Id = (int)reader["Id"],
                                    BanhId = (int)reader["BanhId"],
                                    MoTaChiTiet = reader["MoTaChiTiet"]?.ToString(),
                                    NguyenLieu = reader["NguyenLieu"]?.ToString(),
                                    HuongVi = reader["HuongVi"]?.ToString(),
                                    KichThuoc = reader["KichThuoc"]?.ToString(),
                                    Banh = new Banh
                                    {
                                        TenBanh = reader["TenBanh"].ToString()
                                    }
                                });
                            }
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                var detailsTxt = FormatSqlException(ex, lastSql, lastParams);
                _logger.LogError(ex, "SQL error when loading BanhChiTiet. Details: {Details}", detailsTxt);
                ViewBag.Error = _showDetailedErrors ? ex.Message : "❌ Lỗi truy vấn chi tiết bánh.";
                ViewBag.SqlErrorDetails = detailsTxt;
            }

            ViewBag.BanhId = id;
            return View(details); // View: Views/Products/Details.cshtml
        }
        [HttpPost]
        public IActionResult SaveDetail(BanhChiTiet model)
        {
            string lastSql = null;
            var lastParams = new List<KeyValuePair<string, object>>();

            try
            {
                if (model.BanhId <= 0)
                    throw new Exception("Thiếu thông tin Bánh liên kết (BanhId).");

                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    if (model.Id == 0)
                    {
                        lastSql = @"
                    INSERT INTO BanhChiTiet (BanhId, MoTaChiTiet, NguyenLieu, HuongVi, KichThuoc)
                    VALUES (@BanhId, @MoTaChiTiet, @NguyenLieu, @HuongVi, @KichThuoc)";

                        using (SqlCommand cmd = new SqlCommand(lastSql, conn))
                        {
                            AddParameter(cmd, "@BanhId", model.BanhId, lastParams);
                            AddParameter(cmd, "@MoTaChiTiet", (object?)model.MoTaChiTiet ?? DBNull.Value, lastParams);
                            AddParameter(cmd, "@NguyenLieu", (object?)model.NguyenLieu ?? DBNull.Value, lastParams);
                            AddParameter(cmd, "@HuongVi", (object?)model.HuongVi ?? DBNull.Value, lastParams);
                            AddParameter(cmd, "@KichThuoc", (object?)model.KichThuoc ?? DBNull.Value, lastParams);
                            cmd.ExecuteNonQuery();
                        }

                        TempData["Success"] = "✅ Thêm chi tiết bánh thành công!";
                    }
                    else
                    {
                        lastSql = @"
                    UPDATE BanhChiTiet
                    SET MoTaChiTiet = @MoTaChiTiet,
                        NguyenLieu = @NguyenLieu,
                        HuongVi = @HuongVi,
                        KichThuoc = @KichThuoc
                    WHERE Id = @Id";

                        using (SqlCommand cmd = new SqlCommand(lastSql, conn))
                        {
                            AddParameter(cmd, "@Id", model.Id, lastParams);
                            AddParameter(cmd, "@MoTaChiTiet", (object?)model.MoTaChiTiet ?? DBNull.Value, lastParams);
                            AddParameter(cmd, "@NguyenLieu", (object?)model.NguyenLieu ?? DBNull.Value, lastParams);
                            AddParameter(cmd, "@HuongVi", (object?)model.HuongVi ?? DBNull.Value, lastParams);
                            AddParameter(cmd, "@KichThuoc", (object?)model.KichThuoc ?? DBNull.Value, lastParams);
                            cmd.ExecuteNonQuery();
                        }

                        TempData["Success"] = "✅ Cập nhật chi tiết bánh thành công!";
                    }
                }
            }
            catch (SqlException ex)
            {
                var details = FormatSqlException(ex, lastSql, lastParams);
                _logger.LogError(ex, "SQL error when saving BanhChiTiet. Details: {Details}", details);
                TempData["Error"] = _showDetailedErrors ? "❌ Lỗi SQL: " + ex.Message : "❌ Lỗi lưu chi tiết bánh.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error when saving BanhChiTiet.");
                TempData["Error"] = "❌ " + ex.Message;
            }

            return RedirectToAction("Details", new { id = model.BanhId });
        }
        [HttpPost]
        public IActionResult DeleteDetail(int id, int banhId)
        {
            string lastSql = null;
            var lastParams = new List<KeyValuePair<string, object>>();

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    lastSql = "DELETE FROM BanhChiTiet WHERE Id = @Id";

                    using (SqlCommand cmd = new SqlCommand(lastSql, conn))
                    {
                        AddParameter(cmd, "@Id", id, lastParams);
                        cmd.ExecuteNonQuery();
                    }

                    TempData["Success"] = "🗑️ Đã xóa chi tiết bánh thành công!";
                }
            }
            catch (SqlException ex)
            {
                var details = FormatSqlException(ex, lastSql, lastParams);
                _logger.LogError(ex, "SQL error when deleting BanhChiTiet. Details: {Details}", details);
                TempData["Error"] = _showDetailedErrors ? "❌ Lỗi SQL: " + ex.Message : "❌ Lỗi xóa chi tiết bánh.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error when deleting BanhChiTiet.");
                TempData["Error"] = "❌ " + ex.Message;
            }

            return RedirectToAction("Details", new { id = banhId });
        }

        #region Helpers

        // Thêm param và ghi lại để hiển thị/ghi log khi lỗi
        private void AddParameter(SqlCommand cmd, string name, object value, List<KeyValuePair<string, object>> paramLog)
        {
            var valueToAdd = value ?? DBNull.Value;
            cmd.Parameters.AddWithValue(name, valueToAdd);
            // store original (NULL vs DBNull.Value) -> dùng null cho Nulls để hiển thị rõ ràng
            var recorded = value == DBNull.Value ? null : value;
            paramLog.Add(new KeyValuePair<string, object>(name, recorded));
        }

        // Format chi tiết SqlException (số, errors collection, query, params, stack trace)
        private string FormatSqlException(SqlException ex, string lastSql, List<KeyValuePair<string, object>> lastParams)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== SqlException details ===");
            sb.AppendLine($"Message: {ex.Message}");
            sb.AppendLine($"Number: {ex.Number}, State: {ex.State}, Class: {ex.Class}");
            sb.AppendLine($"Server: {ex.Server}, Procedure: {ex.Procedure}, LineNumber: {ex.LineNumber}");
            sb.AppendLine();

            if (ex.Errors != null && ex.Errors.Count > 0)
            {
                sb.AppendLine("--- Error collection ---");
                int i = 1;
                foreach (SqlError err in ex.Errors)
                {
                    sb.AppendLine($"[{i}] Number={err.Number}, Message={err.Message}, Procedure={err.Procedure}, Line={err.LineNumber}, Source={err.Source}, Class={err.Class}");
                    i++;
                }
                sb.AppendLine();
            }

            if (!string.IsNullOrEmpty(lastSql))
            {
                sb.AppendLine("--- Executed SQL ---");
                sb.AppendLine(lastSql);
                sb.AppendLine();
            }

            if (lastParams != null && lastParams.Count > 0)
            {
                sb.AppendLine("--- Parameters ---");
                foreach (var p in lastParams)
                {
                    var display = p.Value == null ? "NULL" : p.Value.ToString();
                    sb.AppendLine($"{p.Key} = {display}");
                }
                sb.AppendLine();
            }

            sb.AppendLine("--- StackTrace ---");
            sb.AppendLine(ex.StackTrace);

            return sb.ToString();
        }

        #endregion
    }
}
