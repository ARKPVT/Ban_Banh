using Ban_Banh.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;

namespace Ban_Banh.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class SupplierController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SupplierController> _logger;
        private readonly IWebHostEnvironment _env;

        public SupplierController(IConfiguration configuration, ILogger<SupplierController> logger, IWebHostEnvironment env)
        {
            _configuration = configuration;
            _logger = logger;
            _env = env;
        }

        // ============================================================
        // DANH SÁCH
        // ============================================================
        public IActionResult Index()
        {
            var suppliers = new List<Supplier>();
            string connectionString = _configuration.GetConnectionString("BanBanhDB");

            try
            {
                using (var conn = new SqlConnection(connectionString))
                using (var cmd = conn.CreateCommand())
                {
                    conn.Open();
                    cmd.CommandText = @"
                        SELECT s.Id, s.TenNhaCungCap, s.DiaChi, s.SoDienThoai, s.Email, s.Website, s.GhiChu,
                               b.TenBanh, sp.GiaNhap, sp.NgayNhap
                        FROM Supplier s
                        LEFT JOIN SupplierProduct sp ON s.Id = sp.SupplierId
                        LEFT JOIN Banh b ON sp.BanhId = b.Id
                        ORDER BY s.Id DESC";

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            suppliers.Add(new Supplier
                            {
                                Id = reader.GetInt32(0),
                                TenNhaCungCap = reader.IsDBNull(1) ? null : reader.GetString(1),
                                DiaChi = reader.IsDBNull(2) ? null : reader.GetString(2),
                                SoDienThoai = reader.IsDBNull(3) ? null : reader.GetString(3),
                                Email = reader.IsDBNull(4) ? null : reader.GetString(4),
                                Website = reader.IsDBNull(5) ? null : reader.GetString(5),
                                GhiChu = reader.IsDBNull(6) ? null : reader.GetString(6),
                                TenBanh = reader.IsDBNull(7) ? null : reader.GetString(7),
                                GiaNhap = reader.IsDBNull(8) ? (decimal?)null : reader.GetDecimal(8),
                                NgayNhap = reader.IsDBNull(9) ? (DateTime?)null : reader.GetDateTime(9)
                            });
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Lỗi SQL khi load danh sách nhà cung cấp. Mã lỗi: {ErrorCode}", ex.Number);
                TempData["Error"] = _env.IsDevelopment()
                    ? $"Lỗi CSDL (Code {ex.Number}): {ex.Message}"
                    : "Không thể tải danh sách nhà cung cấp. Vui lòng thử lại.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi hệ thống khi load danh sách nhà cung cấp.");
                TempData["Error"] = _env.IsDevelopment()
                    ? ex.Message
                    : "Hệ thống đang gặp sự cố. Vui lòng thử lại sau.";
            }

            return View(suppliers);
        }

        // ============================================================
        // GET: CREATE
        // ============================================================
        public IActionResult Create() => View();

        // ============================================================
        // POST: CREATE
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Supplier model)
        {
            if (!ModelState.IsValid)
                return View(model);

            string connectionString = _configuration.GetConnectionString("BanBanhDB");

            try
            {
                using (var conn = new SqlConnection(connectionString))
                using (var cmd = conn.CreateCommand())
                {
                    conn.Open();
                    cmd.CommandText = @"
                        INSERT INTO Supplier (TenNhaCungCap, DiaChi, SoDienThoai, Email, Website, GhiChu)
                        VALUES (@TenNhaCungCap, @DiaChi, @SoDienThoai, @Email, @Website, @GhiChu)";

                    cmd.Parameters.AddWithValue("@TenNhaCungCap", (object?)model.TenNhaCungCap ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@DiaChi", (object?)model.DiaChi ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SoDienThoai", (object?)model.SoDienThoai ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Email", (object?)model.Email ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Website", (object?)model.Website ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@GhiChu", (object?)model.GhiChu ?? DBNull.Value);

                    cmd.ExecuteNonQuery();
                }

                TempData["Message"] = "✅ Thêm nhà cung cấp thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL Error khi thêm nhà cung cấp. Code: {ErrorCode}", ex.Number);

                string message = ex.Number switch
                {
                    2627 or 2601 => "❌ Tên nhà cung cấp hoặc email đã tồn tại trong hệ thống.",
                    515 => "⚠️ Một số trường bắt buộc chưa được nhập.",
                    547 => "❌ Dữ liệu không hợp lệ hoặc vi phạm ràng buộc khóa ngoại.",
                    _ => _env.IsDevelopment() ? $"Lỗi SQL (Code {ex.Number}): {ex.Message}" : "Không thể thêm dữ liệu vào hệ thống."
                };

                ModelState.AddModelError("", message);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi hệ thống khi thêm nhà cung cấp.");
                ModelState.AddModelError("", _env.IsDevelopment() ? ex.Message : "Hệ thống đang gặp sự cố. Vui lòng thử lại.");
                return View(model);
            }
        }

        // ============================================================
        // GET: EDIT
        // ============================================================
        // ===============================
        // 2️⃣ Chi tiết & Sửa Nhà Cung Cấp
        // ===============================
        public IActionResult Edit(int id)
        {
            Supplier supplier = null;
            string connectionString = _configuration.GetConnectionString("BanBanhDB");

            try
            {
                using var conn = new SqlConnection(connectionString);
                conn.Open();

                // 🔹 1. Lấy thông tin nhà cung cấp
                string sqlSupplier = @"
            SELECT Id, TenNhaCungCap, DiaChi, SoDienThoai, Email, Website, GhiChu
            FROM Supplier
            WHERE Id = @Id";
                using (var cmd = new SqlCommand(sqlSupplier, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    using var reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        supplier = new Supplier
                        {
                            Id = reader.GetInt32(0),
                            TenNhaCungCap = reader.GetString(1),
                            DiaChi = reader.IsDBNull(2) ? "" : reader.GetString(2),
                            SoDienThoai = reader.IsDBNull(3) ? "" : reader.GetString(3),
                            Email = reader.IsDBNull(4) ? "" : reader.GetString(4),
                            Website = reader.IsDBNull(5) ? "" : reader.GetString(5),
                            GhiChu = reader.IsDBNull(6) ? "" : reader.GetString(6),
                            SupplierProducts = new List<SupplierProduct>()
                        };
                    }
                }

                if (supplier == null)
                {
                    TempData["Error"] = "Không tìm thấy nhà cung cấp.";
                    return RedirectToAction(nameof(Index));
                }

                // 🔹 2. Lấy danh sách bánh mà nhà cung cấp này đang cung cấp
                string sqlProducts = @"
            SELECT 
                sp.Id, sp.SupplierId, sp.BanhId, b.TenBanh, sp.GiaNhap, sp.NgayNhap
            FROM SupplierProduct sp
            JOIN Banh b ON sp.BanhId = b.Id
            WHERE sp.SupplierId = @SupplierId
            ORDER BY sp.NgayNhap DESC";
                using (var cmd2 = new SqlCommand(sqlProducts, conn))
                {
                    cmd2.Parameters.AddWithValue("@SupplierId", id);
                    using var reader2 = cmd2.ExecuteReader();
                    while (reader2.Read())
                    {
                        supplier.SupplierProducts.Add(new SupplierProduct
                        {
                            Id = reader2.GetInt32(0),
                            SupplierId = reader2.GetInt32(1),
                            BanhId = reader2.GetInt32(2),
                            TenBanh = reader2.GetString(3),
                            GiaNhap = reader2.GetDecimal(4),
                            NgayNhap = reader2.GetDateTime(5)
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi truy vấn nhà cung cấp Id={Id}", id);
                TempData["Error"] = _env.IsDevelopment() ? ex.Message : "Không thể tải thông tin nhà cung cấp.";
            }

            return View(supplier);
        }


        // ============================================================
        // POST: EDIT
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Supplier model)
        {
            if (!ModelState.IsValid)
                return View(model);

            string connectionString = _configuration.GetConnectionString("BanBanhDB");

            try
            {
                using var conn = new SqlConnection(connectionString);
                using var cmd = conn.CreateCommand();
                conn.Open();

                cmd.CommandText = @"
            UPDATE Supplier
            SET TenNhaCungCap=@TenNhaCungCap,
                DiaChi=@DiaChi,
                SoDienThoai=@SoDienThoai,
                Email=@Email,
                Website=@Website,
                GhiChu=@GhiChu
            WHERE Id=@Id";
                cmd.Parameters.AddWithValue("@Id", model.Id);
                cmd.Parameters.AddWithValue("@TenNhaCungCap", model.TenNhaCungCap ?? "");
                cmd.Parameters.AddWithValue("@DiaChi", model.DiaChi ?? "");
                cmd.Parameters.AddWithValue("@SoDienThoai", model.SoDienThoai ?? "");
                cmd.Parameters.AddWithValue("@Email", model.Email ?? "");
                cmd.Parameters.AddWithValue("@Website", model.Website ?? "");
                cmd.Parameters.AddWithValue("@GhiChu", model.GhiChu ?? "");

                cmd.ExecuteNonQuery();

                TempData["Message"] = "✅ Cập nhật thông tin nhà cung cấp thành công!";
                return RedirectToAction(nameof(Edit), new { id = model.Id });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Lỗi SQL khi cập nhật nhà cung cấp Id={Id}", model.Id);
                TempData["Error"] = _env.IsDevelopment() ? ex.Message : "Không thể cập nhật dữ liệu.";
            }

            return View(model);
        }


        // ============================================================
        // DELETE
        // ============================================================
        public IActionResult Delete(int id)
        {
            string connectionString = _configuration.GetConnectionString("BanBanhDB");

            try
            {
                using (var conn = new SqlConnection(connectionString))
                using (var cmd = conn.CreateCommand())
                {
                    conn.Open();
                    cmd.CommandText = "DELETE FROM Supplier WHERE Id=@Id";
                    cmd.Parameters.AddWithValue("@Id", id);

                    int rows = cmd.ExecuteNonQuery();

                    if (rows == 0)
                        TempData["Error"] = "Không tìm thấy nhà cung cấp để xóa.";
                    else
                        TempData["Message"] = "🗑️ Đã xóa nhà cung cấp thành công!";
                }
            }
            catch (SqlException ex) when (ex.Number == 547)
            {
                _logger.LogWarning(ex, "Không thể xóa supplier {Id} vì vi phạm ràng buộc khóa ngoại.", id);
                TempData["Error"] = "❌ Không thể xóa vì có dữ liệu liên quan (bánh, phiếu nhập, ...).";
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Lỗi SQL khi xóa nhà cung cấp {Id}. Code: {Code}", id, ex.Number);
                TempData["Error"] = _env.IsDevelopment()
                    ? $"SQL Error (Code {ex.Number}): {ex.Message}"
                    : "Không thể xóa dữ liệu. Vui lòng thử lại.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi hệ thống khi xóa nhà cung cấp {Id}.", id);
                TempData["Error"] = _env.IsDevelopment()
                    ? ex.Message
                    : "Hệ thống đang gặp sự cố. Vui lòng thử lại.";
            }

            return RedirectToAction(nameof(Index));
        }
        // ============================================================
        // SUPPLIER PRODUCT CRUD
        // ============================================================

        // GET: AddProduct
        public IActionResult AddProduct(int supplierId)
        {
            ViewBag.SupplierId = supplierId;
            ViewBag.BanhList = GetAllBanh(); // ✅ load danh sách bánh để hiển thị dropdown
            return View("AddProduct");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddProduct(SupplierProduct model)
        {
            if (model.BanhId <= 0)
                ModelState.AddModelError("", "Vui lòng chọn bánh hợp lệ.");
            if (model.GiaNhap <= 0)
                ModelState.AddModelError("", "Giá nhập phải lớn hơn 0.");

            if (!ModelState.IsValid)
            {
                ViewBag.SupplierId = model.SupplierId;
                ViewBag.BanhList = GetAllBanh();
                return View(model);
            }

            string connectionString = _configuration.GetConnectionString("BanBanhDB");

            try
            {
                using var conn = new SqlConnection(connectionString);
                using var cmd = conn.CreateCommand();
                conn.Open();

                cmd.CommandText = @"
            INSERT INTO SupplierProduct (SupplierId, BanhId, GiaNhap)
            VALUES (@SupplierId, @BanhId, @GiaNhap)";
                cmd.Parameters.AddWithValue("@SupplierId", model.SupplierId);
                cmd.Parameters.AddWithValue("@BanhId", model.BanhId);
                cmd.Parameters.AddWithValue("@GiaNhap", model.GiaNhap);

                cmd.ExecuteNonQuery();
                TempData["Message"] = "✅ Đã thêm bánh vào nhà cung cấp!";
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Lỗi SQL khi thêm SupplierProduct.");
                TempData["Error"] = _env.IsDevelopment() ? ex.Message : "Không thể thêm bánh.";
            }

            return RedirectToAction(nameof(Edit), new { id = model.SupplierId });
        }


        // GET: EditProduct
        public IActionResult EditProduct(int id)
        {
            SupplierProduct sp = null;
            string connectionString = _configuration.GetConnectionString("BanBanhDB");

            try
            {
                using var conn = new SqlConnection(connectionString);
                using var cmd = conn.CreateCommand();
                conn.Open();

                cmd.CommandText = @"
            SELECT sp.Id, sp.SupplierId, sp.BanhId, b.TenBanh, sp.GiaNhap, sp.NgayNhap
            FROM SupplierProduct sp
            JOIN Banh b ON sp.BanhId = b.Id
            WHERE sp.Id = @Id";
                cmd.Parameters.AddWithValue("@Id", id);

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    sp = new SupplierProduct
                    {
                        Id = reader.GetInt32(0),
                        SupplierId = reader.GetInt32(1),
                        BanhId = reader.GetInt32(2),
                        TenBanh = reader.GetString(3),
                        GiaNhap = reader.GetDecimal(4),
                        NgayNhap = reader.GetDateTime(5)
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy SupplierProduct Id={Id}", id);
                TempData["Error"] = _env.IsDevelopment() ? ex.Message : "Không thể tải dữ liệu.";
            }

            if (sp == null)
            {
                TempData["Error"] = "Không tìm thấy dữ liệu.";
                return RedirectToAction(nameof(Index));
            }

            return View("EditProduct", sp);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditProduct(SupplierProduct model)
        {
            if (model.GiaNhap <= 0)
            {
                ModelState.AddModelError("", "Giá nhập phải lớn hơn 0");
                return View(model);
            }

            string connectionString = _configuration.GetConnectionString("BanBanhDB");
            try
            {
                using var conn = new SqlConnection(connectionString);
                using var cmd = conn.CreateCommand();
                conn.Open();

                cmd.CommandText = @"
            UPDATE SupplierProduct
            SET GiaNhap=@GiaNhap
            WHERE Id=@Id";
                cmd.Parameters.AddWithValue("@Id", model.Id);
                cmd.Parameters.AddWithValue("@GiaNhap", model.GiaNhap);

                cmd.ExecuteNonQuery();
                TempData["Message"] = "✅ Cập nhật giá nhập thành công!";
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL Error khi cập nhật SupplierProduct Id={Id}", model.Id);
                TempData["Error"] = _env.IsDevelopment() ? ex.Message : "Không thể cập nhật.";
            }

            return RedirectToAction(nameof(Edit), new { id = model.SupplierId });
        }


        // DELETE PRODUCT
        public IActionResult DeleteProduct(int id, int supplierId)
        {
            string connectionString = _configuration.GetConnectionString("BanBanhDB");

            try
            {
                using var conn = new SqlConnection(connectionString);
                using var cmd = conn.CreateCommand();
                conn.Open();

                cmd.CommandText = "DELETE FROM SupplierProduct WHERE Id=@Id";
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.ExecuteNonQuery();

                TempData["Message"] = "🗑️ Đã xóa bánh khỏi nhà cung cấp!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa SupplierProduct Id={Id}", id);
                TempData["Error"] = _env.IsDevelopment() ? ex.Message : "Không thể xóa dữ liệu.";
            }

            return RedirectToAction(nameof(Edit), new { id = supplierId });
        }


        // ==========================================
        // HÀM PHỤ: Lấy danh sách bánh
        // ==========================================
        private List<Banh> GetAllBanh()
        {
            var list = new List<Banh>();
            string connectionString = _configuration.GetConnectionString("BanBanhDB");

            try
            {
                using var conn = new SqlConnection(connectionString);
                using var cmd = conn.CreateCommand();
                conn.Open();

                cmd.CommandText = "SELECT Id, TenBanh FROM Banh ORDER BY TenBanh";
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new Banh
                    {
                        Id = reader.GetInt32(0),
                        TenBanh = reader.GetString(1)
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể load danh sách bánh.");
            }

            return list;
        }

    }
}
