using Ban_Banh.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Ban_Banh.Controllers
{
    [Authorize(Policy = "UserOnly")]
    public class CustomerOrdersController : Controller
    {
        private readonly string _connectionString;

        public CustomerOrdersController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("BanBanhDB");
        }

        // Trang lịch sử đơn hàng của khách hàng
        public IActionResult Index()
        {
            var email = HttpContext.Session.GetString("UserEmail");
            if (email == null)
                return RedirectToAction("Login", "Account");

            var orders = new List<OrderWithProductsViewModel>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                string sql = @"
                    SELECT o.Id AS OrderId, o.AccountId, s.StatusName AS Status, o.CreatedAt, o.UpdatedAt,
       b.Id AS BanhId, b.TenBanh, od.Quantity, b.Gia
FROM [Order] o
INNER JOIN Account a ON o.AccountId = a.Id
INNER JOIN OrderDetail od ON od.OrderId = o.Id
INNER JOIN Banh b ON od.BanhId = b.Id
INNER JOIN OrderStatus s ON o.StatusId = s.Id
WHERE a.Email = @Email
ORDER BY o.CreatedAt DESC
";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        var orderDict = new Dictionary<int, OrderWithProductsViewModel>();

                        while (reader.Read())
                        {
                            int orderId = (int)reader["OrderId"];
                            int banhId = (int)reader["BanhId"];

                            if (!orderDict.ContainsKey(orderId))
                            {
                                orderDict[orderId] = new OrderWithProductsViewModel
                                {
                                    OrderId = orderId,
                                    Status = reader["Status"].ToString(),
                                    CreatedAt = (DateTime)reader["CreatedAt"],
                                    UpdatedAt = reader["UpdatedAt"] as DateTime?,
                                    Products = new List<OrderProductViewModel>()
                                };
                            }

                            var order = orderDict[orderId];

                            // Kiểm tra xem bánh này đã có trong danh sách chưa
                            var existingProduct = order.Products.FirstOrDefault(p => p.BanhId == banhId);

                            if (existingProduct != null)
                            {
                                existingProduct.Quantity += (int)reader["Quantity"];
                            }
                            else
                            {
                                order.Products.Add(new OrderProductViewModel
                                {
                                    BanhId = banhId,
                                    TenBanh = reader["TenBanh"].ToString(),
                                    Quantity = (int)reader["Quantity"],
                                    Gia = (decimal)reader["Gia"]
                                });
                            }
                        }

                        orders = orderDict.Values.ToList();
                    }
                }
            }

            return View(orders);
        }

        // Chi tiết đơn hàng
        public IActionResult Details(int id)
        {
            var email = HttpContext.Session.GetString("UserEmail");
            if (email == null)
                return RedirectToAction("Login", "Account");

            OrderWithProductsViewModel? order = null;

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                string sql = @"
                    SELECT o.Id AS OrderId, o.AccountId, s.StatusName AS Status, o.CreatedAt, o.UpdatedAt,
       b.Id AS BanhId, b.TenBanh, od.Quantity, b.Gia
FROM [Order] o
INNER JOIN Account a ON o.AccountId = a.Id
INNER JOIN OrderDetail od ON od.OrderId = o.Id
INNER JOIN Banh b ON od.BanhId = b.Id
INNER JOIN OrderStatus s ON o.StatusId = s.Id
WHERE a.Email = @Email AND o.Id = @OrderId
";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    cmd.Parameters.AddWithValue("@OrderId", id);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int banhId = (int)reader["BanhId"];

                            if (order == null)
                            {
                                order = new OrderWithProductsViewModel
                                {
                                    OrderId = (int)reader["OrderId"],
                                    Status = reader["Status"].ToString(),
                                    CreatedAt = (DateTime)reader["CreatedAt"],
                                    UpdatedAt = reader["UpdatedAt"] as DateTime?,
                                    Products = new List<OrderProductViewModel>()
                                };
                            }

                            var existingProduct = order.Products.FirstOrDefault(p => p.BanhId == banhId);
                            if (existingProduct != null)
                            {
                                existingProduct.Quantity += (int)reader["Quantity"];
                            }
                            else
                            {
                                order.Products.Add(new OrderProductViewModel
                                {
                                    BanhId = banhId,
                                    TenBanh = reader["TenBanh"].ToString(),
                                    Quantity = (int)reader["Quantity"],
                                    Gia = (decimal)reader["Gia"]
                                });
                            }
                        }
                    }
                }
            }

            if (order == null)
                return NotFound();

            return View(order);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Complete(int id)
        {
            var email = HttpContext.Session.GetString("UserEmail");
            if (email == null)
                return RedirectToAction("Login", "Account");

            int rows;
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                // Chỉ cập nhật đơn của chính user này và đang Shipped
                var sql = @"
UPDATE o
SET o.StatusId = (SELECT Id FROM OrderStatus WHERE StatusName = N'Completed'),
    o.UpdatedAt = GETDATE()
FROM [Order] o
INNER JOIN Account a ON a.Id = o.AccountId
WHERE o.Id = @OrderId
  AND a.Email = @Email
  AND o.StatusId = (SELECT Id FROM OrderStatus WHERE StatusName = N'Shipped');";

                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@OrderId", id);
                    cmd.Parameters.AddWithValue("@Email", email);
                    rows = cmd.ExecuteNonQuery();
                }
            }

            if (rows > 0)
                TempData["Success"] = $"Đơn #{id} đã được chuyển sang 'Completed'. Cảm ơn bạn!";
            else
                TempData["Error"] = "Không thể cập nhật. Có thể đơn không thuộc bạn hoặc không ở trạng thái 'Shipped'.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Cancel(int id)
        {
            var email = HttpContext.Session.GetString("UserEmail");
            if (email == null)
                return RedirectToAction("Login", "Account");

            int rows;
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                // Chỉ cập nhật đơn của chính user này và đang Shipped
                var sql = @"
UPDATE o
SET o.StatusId = (SELECT Id FROM OrderStatus WHERE StatusName = N'Cancelled'),
    o.UpdatedAt = GETDATE()
FROM [Order] o
INNER JOIN Account a ON a.Id = o.AccountId
WHERE o.Id = @OrderId
  AND a.Email = @Email
  AND o.StatusId = (SELECT Id FROM OrderStatus WHERE StatusName = N'Shipped');";

                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@OrderId", id);
                    cmd.Parameters.AddWithValue("@Email", email);
                    rows = cmd.ExecuteNonQuery();
                }
            }

            if (rows > 0)
                TempData["Success"] = $"Đơn #{id} đã được chuyển sang 'Cancelled'.";
            else
                TempData["Error"] = "Không thể hủy. Có thể đơn không thuộc bạn hoặc không ở trạng thái 'Shipped'.";

            return RedirectToAction(nameof(Index));
        }
    }
}
