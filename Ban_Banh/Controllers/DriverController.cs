using Ban_Banh.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Ban_Banh.Controllers
{
    [Authorize(Policy = "ShipOnly")]
    public class DriverController : Controller
    {
        private readonly string _connectionString;
        private const string SessionUserIdKey = "AccountId"; // đổi nếu app bạn dùng key khác

        public DriverController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("BanBanhDB");
        }

        private int? GetCurrentDriverId()
        {
            // bạn có cơ chế đăng nhập riêng -> set SessionUserIdKey khi login
            return HttpContext.Session.GetInt32(SessionUserIdKey);
        }

        // GET: /Driver
        // Hiển thị ReadyToShip (tất cả) + Shipped (của tôi)
        public IActionResult Index()
        {
            var driverId = GetCurrentDriverId();
            if (driverId == null)
            {
                TempData["Error"] = "Bạn cần đăng nhập tài khoản tài xế trước.";
                return RedirectToAction("Login", "Account");
            }

            var rows = new List<( // row phẳng đọc từ view vw_DriverOrders
                int OrderId, int CustomerAccountId, string CustomerName, string CustomerEmail, string CustomerPhone, string CustomerAddress,
                int? DriverId, string? DriverName, string StatusName, DateTime CreatedAt, DateTime? UpdatedAt,
                int OrderDetailId, int BanhId, string TenBanh, int Quantity, decimal Gia,
                string? BatchCode, int? InventoryId, string? InventoryBatchCode, string? SupplierName, string? WarehouseLocation, string? WarehouseZone
            )>();

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                // Lấy Id của 2 trạng thái để chắc chắn theo dữ liệu hiện có
                int readyId, shippedId;
                using (var cmd = new SqlCommand("SELECT Id, StatusName FROM OrderStatus WHERE StatusName IN (N'ReadyToShip', N'Shipped')", conn))
                using (var rd = cmd.ExecuteReader())
                {
                    int tmpReady = 0, tmpShipped = 0;
                    while (rd.Read())
                    {
                        var id = rd.GetInt32(0);
                        var name = rd.GetString(1);
                        if (name == "ReadyToShip") tmpReady = id;
                        if (name == "Shipped") tmpShipped = id;
                    }
                    readyId = tmpReady; shippedId = tmpShipped;
                }

                // Lấy dữ liệu từ view
                var sql = @"
SELECT
    OrderId, CustomerAccountId, CustomerName, CustomerEmail, CustomerPhone, CustomerAddress,
    DriverId, DriverName, StatusName, CreatedAt, UpdatedAt,
    OrderDetailId, BanhId, TenBanh, Quantity, Gia,
    BatchCode, InventoryId, InventoryBatchCode, SupplierName, WarehouseLocation, WarehouseZone
FROM vw_DriverOrders
WHERE
    StatusName = N'ReadyToShip'
    OR (StatusName = N'Shipped' AND DriverId = @DriverId)
ORDER BY CreatedAt DESC, OrderId DESC, OrderDetailId ASC";

                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@DriverId", driverId.Value);
                    using (var rd = cmd.ExecuteReader())
                    {
                        while (rd.Read())
                        {
                            rows.Add((
                                rd.GetInt32(0),                              // OrderId
                                rd.GetInt32(1),                              // CustomerAccountId
                                rd.GetString(2),                             // CustomerName
                                rd.IsDBNull(3) ? null : rd.GetString(3),     // CustomerEmail
                                rd.IsDBNull(4) ? null : rd.GetString(4),     // CustomerPhone
                                rd.IsDBNull(5) ? null : rd.GetString(5),     // CustomerAddress
                                rd.IsDBNull(6) ? (int?)null : rd.GetInt32(6),// DriverId
                                rd.IsDBNull(7) ? null : rd.GetString(7),     // DriverName
                                rd.GetString(8),                             // StatusName
                                rd.GetDateTime(9),                           // CreatedAt
                                rd.IsDBNull(10) ? (DateTime?)null : rd.GetDateTime(10), // UpdatedAt
                                rd.GetInt32(11),                             // OrderDetailId
                                rd.GetInt32(12),                             // BanhId
                                rd.GetString(13),                            // TenBanh
                                rd.GetInt32(14),                             // Quantity
                                rd.GetDecimal(15),                           // Gia
                                rd.IsDBNull(16) ? null : rd.GetString(16),   // BatchCode
                                rd.IsDBNull(17) ? (int?)null : rd.GetInt32(17), // InventoryId
                                rd.IsDBNull(18) ? null : rd.GetString(18),   // InventoryBatchCode
                                rd.IsDBNull(19) ? null : rd.GetString(19),   // SupplierName
                                rd.IsDBNull(20) ? null : rd.GetString(20),   // WarehouseLocation
                                rd.IsDBNull(21) ? null : rd.GetString(21)    // WarehouseZone  <-- THÊM DÒNG NÀY
                            ));
                        }

                    }
                }
            }

            // Gom thành ViewModel theo Order
            var grouped = rows
                .GroupBy(r => r.OrderId)
                .Select(g =>
                {
                    var first = g.First();
                    var vm = new DriverOrderVM
                    {
                        OrderId = first.OrderId,
                        StatusName = first.StatusName,
                        CreatedAt = first.CreatedAt,
                        UpdatedAt = first.UpdatedAt,
                        DriverId = first.DriverId,
                        DriverName = first.DriverName,
                        CustomerAccountId = first.CustomerAccountId,
                        CustomerName = first.CustomerName,
                        CustomerEmail = first.CustomerEmail,
                        CustomerPhone = first.CustomerPhone,
                        CustomerAddress = first.CustomerAddress
                    };
                    foreach (var r in g)
                    {
                        vm.Items.Add(new DriverOrderItemVM
                        {
                            OrderDetailId = r.OrderDetailId,
                            BanhId = r.BanhId,
                            TenBanh = r.TenBanh,
                            Quantity = r.Quantity,
                            Gia = r.Gia,
                            BatchCode = r.BatchCode,
                            InventoryId = r.InventoryId,
                            InventoryBatchCode = r.InventoryBatchCode,
                            SupplierName = r.SupplierName,
                            WarehouseLocation = r.WarehouseLocation,
                            WarehouseZone = r.WarehouseZone
                        });
                    }
                    return vm;
                })
                .OrderByDescending(x => x.CreatedAt)
                .ToList();

            return View(grouped);
        }

        // POST: /Driver/Assign/5  -> Nhận đơn (ReadyToShip -> Shipped)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Assign(int orderId)
        {
            var driverId = GetCurrentDriverId();
            if (driverId == null) return Unauthorized();

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand("sp_AssignOrderToDriver", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@OrderId", orderId);
                    cmd.Parameters.AddWithValue("@DriverId", driverId.Value);
                    try
                    {
                        cmd.ExecuteNonQuery();
                        TempData["Success"] = $"Đã nhận đơn #{orderId}.";
                    }
                    catch (SqlException ex)
                    {
                        TempData["Error"] = ex.Message;
                    }
                }
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: /Driver/Cancel/5  -> Báo lỗi (hủy đơn) (Shipped -> Cancelled), chỉ tài xế của đơn
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Cancel(int orderId)
        {
            var driverId = GetCurrentDriverId();
            if (driverId == null) return Unauthorized();

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand("sp_MarkOrderCancelled", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@OrderId", orderId);
                    cmd.Parameters.AddWithValue("@DriverId", driverId.Value);
                    try
                    {
                        cmd.ExecuteNonQuery();
                        TempData["Success"] = $"Đã chuyển đơn #{orderId} sang Cancelled.";
                    }
                    catch (SqlException ex)
                    {
                        TempData["Error"] = ex.Message;
                    }
                }
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
