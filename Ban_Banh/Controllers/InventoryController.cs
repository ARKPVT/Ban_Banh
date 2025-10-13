using Ban_Banh.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Ban_Banh.Controllers
{
    public class InventoryController : Controller
    {
        private readonly string _connectionString;
        private readonly ILogger<InventoryController> _logger;
        private readonly bool _showDetailedErrors;

        public InventoryController(IConfiguration configuration, ILogger<InventoryController> logger)
        {
            _connectionString = configuration.GetConnectionString("BanBanhDB");
            _logger = logger;
            _showDetailedErrors = configuration.GetValue<bool>("ShowDetailedSqlErrors", false);
        }

        // 📦 Hiển thị danh sách tồn kho
        public IActionResult Index()
        {
            var list = new List<Inventory>();
            string sql = @"
                SELECT i.Id, i.BanhId, b.TenBanh, i.SoLuong, i.LastUpdated,
                       i.NgaySanXuat, i.HanSuDung,
                       s.Id AS SupplierId, s.TenNhaCungCap,
                       w.Id AS WarehouseLocationId, w.MaViTri, w.TenKhu
                FROM Inventory i
                JOIN Banh b ON i.BanhId = b.Id
                LEFT JOIN Supplier s ON i.SupplierId = s.Id
                LEFT JOIN WarehouseLocation w ON i.WarehouseLocationId = w.Id
                ORDER BY b.Id ASC, i.NgaySanXuat DESC";

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new Inventory
                            {
                                Id = reader.GetInt32(0),
                                BanhId = reader.GetInt32(1),
                                TenBanh = reader.GetString(2),
                                SoLuong = reader.GetInt32(3),
                                LastUpdated = reader.GetDateTime(4),
                                NgaySanXuat = reader.IsDBNull(5) ? (DateTime?)null : reader.GetDateTime(5),
                                HanSuDung = reader.IsDBNull(6) ? (DateTime?)null : reader.GetDateTime(6),
                                SupplierId = reader.IsDBNull(7) ? (int?)null : reader.GetInt32(7),
                                TenNhaCungCap = reader.IsDBNull(8) ? null : reader.GetString(8),
                                WarehouseLocationId = reader.IsDBNull(9) ? (int?)null : reader.GetInt32(9),
                                MaViTri = reader.IsDBNull(10) ? null : reader.GetString(10),
                                TenKhu = reader.IsDBNull(11) ? null : reader.GetString(11)
                            });
                        }
                    }
                }

                ViewBag.BanhList = GetBanhSelectList();
                ViewBag.SupplierList = GetSupplierSelectList();
                ViewBag.LocationList = GetLocationSelectList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách kho");
                if (_showDetailedErrors) ViewBag.Error = ex.Message;
            }

            return View(list);
        }

        // ==========================
        // 🔹 Lấy danh sách chọn: Bánh
        // ==========================
        private List<SelectListItem> GetBanhSelectList()
        {
            var items = new List<SelectListItem>();
            string sql = "SELECT Id, TenBanh FROM Banh ORDER BY TenBanh";

            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        items.Add(new SelectListItem
                        {
                            Value = reader["Id"].ToString(),
                            Text = reader["TenBanh"].ToString()
                        });
                    }
                }
            }
            return items;
        }

        // ==========================
        // 🔹 Lấy danh sách chọn: Nhà cung cấp
        // ==========================
        private List<SelectListItem> GetSupplierSelectList()
        {
            var items = new List<SelectListItem>();
            string sql = "SELECT Id, TenNhaCungCap FROM Supplier ORDER BY TenNhaCungCap";

            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        items.Add(new SelectListItem
                        {
                            Value = reader["Id"].ToString(),
                            Text = reader["TenNhaCungCap"].ToString()
                        });
                    }
                }
            }
            return items;
        }

        // ==========================
        // 🔹 Lấy danh sách chọn: Vị trí kho
        // ==========================
        private List<SelectListItem> GetLocationSelectList()
        {
            var items = new List<SelectListItem>();
            string sql = "SELECT Id, MaViTri FROM WarehouseLocation ORDER BY MaViTri";

            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        items.Add(new SelectListItem
                        {
                            Value = reader["Id"].ToString(),
                            Text = reader["MaViTri"].ToString()
                        });
                    }
                }
            }
            return items;
        }

        // ==========================
        // 🧺 POST: Nhập thêm hàng
        // ==========================
        [HttpPost]
        public IActionResult Import(int banhId, int soLuong, DateTime? ngaySanXuat, DateTime? hanSuDung, int? supplierId, int? warehouseLocationId)
        {
            if (soLuong <= 0)
            {
                TempData["Error"] = "Số lượng phải lớn hơn 0!";
                return RedirectToAction(nameof(Index));
            }

            if (ngaySanXuat == null || hanSuDung == null)
            {
                TempData["Error"] = "Vui lòng nhập đầy đủ ngày sản xuất và hạn sử dụng!";
                return RedirectToAction(nameof(Index));
            }

            if (hanSuDung <= ngaySanXuat)
            {
                TempData["Error"] = "Hạn sử dụng phải sau ngày sản xuất!";
                return RedirectToAction(nameof(Index));
            }

            string sql = @"
                INSERT INTO Inventory (BanhId, SoLuong, NgaySanXuat, HanSuDung, SupplierId, WarehouseLocationId, LastUpdated)
                VALUES (@BanhId, @SoLuong, @NgaySanXuat, @HanSuDung, @SupplierId, @WarehouseLocationId, GETDATE());
            ";

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@BanhId", banhId);
                    cmd.Parameters.AddWithValue("@SoLuong", soLuong);
                    cmd.Parameters.AddWithValue("@NgaySanXuat", (object)ngaySanXuat ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@HanSuDung", (object)hanSuDung ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SupplierId", (object)supplierId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@WarehouseLocationId", (object)warehouseLocationId ?? DBNull.Value);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }

                TempData["Success"] = "Đã nhập hàng thành công!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi nhập hàng");
                TempData["Error"] = _showDetailedErrors ? ex.Message : "Có lỗi xảy ra khi nhập hàng!";
            }

            return RedirectToAction(nameof(Index));
        }

        // ==========================
        // 🔹 API: Lấy danh sách Nhà cung cấp theo Bánh
        // ==========================
        [HttpGet]
        public JsonResult GetSuppliersByBanh(int banhId)
        {
            var list = new List<SelectListItem>();
            string sql = @"
                SELECT DISTINCT s.Id, s.TenNhaCungCap
                FROM SupplierProduct sp
                JOIN Supplier s ON sp.SupplierId = s.Id
                WHERE sp.BanhId = @BanhId
                ORDER BY s.TenNhaCungCap;
            ";

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@BanhId", banhId);
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new SelectListItem
                            {
                                Value = reader["Id"].ToString(),
                                Text = reader["TenNhaCungCap"].ToString()
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách nhà cung cấp cho bánh Id = {banhId}", banhId);
            }

            return Json(list);
        }

        // ==========================
        // 🔹 API: Lấy danh sách Bánh theo Nhà cung cấp
        // ==========================
        [HttpGet]
        public JsonResult GetBanhBySupplier(int supplierId)
        {
            var list = new List<SelectListItem>();
            string sql = @"
                SELECT DISTINCT b.Id, b.TenBanh
                FROM SupplierProduct sp
                JOIN Banh b ON sp.BanhId = b.Id
                WHERE sp.SupplierId = @SupplierId
                ORDER BY b.TenBanh;
            ";

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@SupplierId", supplierId);
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new SelectListItem
                            {
                                Value = reader["Id"].ToString(),
                                Text = reader["TenBanh"].ToString()
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách bánh cho nhà cung cấp Id = {supplierId}", supplierId);
            }

            return Json(list);
        }

        // ==========================
        // 🔹 API: Lấy danh sách tất cả Bánh
        // ==========================
        [HttpGet]
        public JsonResult GetBanhListAll()
        {
            var list = new List<SelectListItem>();
            string sql = "SELECT Id, TenBanh FROM Banh ORDER BY TenBanh;";

            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new SelectListItem
                        {
                            Value = reader["Id"].ToString(),
                            Text = reader["TenBanh"].ToString()
                        });
                    }
                }
            }
            return Json(list);
        }

        // ==========================
        // 🔹 API: Lấy danh sách tất cả Nhà cung cấp
        // ==========================
        [HttpGet]
        public JsonResult GetSupplierListAll()
        {
            var list = new List<SelectListItem>();
            string sql = "SELECT Id, TenNhaCungCap FROM Supplier;";

            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new SelectListItem
                        {
                            Value = reader["Id"].ToString(),
                            Text = reader["TenNhaCungCap"].ToString()
                        });
                    }
                }
            }
            return Json(list);
        }

        // ==========================
        // 🔹 API: Lấy danh sách tất cả Vị trí kho
        // ==========================
        [HttpGet]
        public JsonResult GetWarehouseLocationListAll()
        {
            var list = new List<SelectListItem>();
            string sql = "SELECT Id, MaViTri FROM WarehouseLocation ORDER BY MaViTri;";

            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new SelectListItem
                        {
                            Value = reader["Id"].ToString(),
                            Text = reader["MaViTri"].ToString()
                        });
                    }
                }
            }
            return Json(list);
        }
    }
}
