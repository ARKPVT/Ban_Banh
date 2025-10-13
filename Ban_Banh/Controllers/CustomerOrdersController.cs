using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using Ban_Banh.Models;

namespace Ban_Banh.Controllers
{
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
                    SELECT 
                        o.Id AS OrderId, o.AccountId, o.Status, o.CreatedAt, o.UpdatedAt,
                        b.TenBanh, od.Quantity, b.Gia
                    FROM [Order] o
                    INNER JOIN Account a ON o.AccountId = a.Id
                    INNER JOIN OrderDetail od ON od.OrderId = o.Id
                    INNER JOIN Banh b ON od.BanhId = b.Id
                    WHERE a.Email = @Email
                    ORDER BY o.CreatedAt DESC";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        var orderDict = new Dictionary<int, OrderWithProductsViewModel>();
                        while (reader.Read())
                        {
                            int orderId = (int)reader["OrderId"];
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

                            orderDict[orderId].Products.Add(new OrderProductViewModel
                            {
                                TenBanh = reader["TenBanh"].ToString(),
                                Quantity = (int)reader["Quantity"],
                                Gia = (decimal)reader["Gia"]
                            });
                        }
                        orders = orderDict.Values.ToList();
                    }
                }
            }

            return View(orders);
        }

        // (Tuỳ chọn) hiển thị chi tiết đơn hàng - dùng chung modal cũng được
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
                    SELECT 
                        o.Id AS OrderId, o.AccountId, o.Status, o.CreatedAt, o.UpdatedAt,
                        b.TenBanh, od.Quantity, b.Gia
                    FROM [Order] o
                    INNER JOIN Account a ON o.AccountId = a.Id
                    INNER JOIN OrderDetail od ON od.OrderId = o.Id
                    INNER JOIN Banh b ON od.BanhId = b.Id
                    WHERE a.Email = @Email AND o.Id = @OrderId";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    cmd.Parameters.AddWithValue("@OrderId", id);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
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
                            order.Products.Add(new OrderProductViewModel
                            {
                                TenBanh = reader["TenBanh"].ToString(),
                                Quantity = (int)reader["Quantity"],
                                Gia = (decimal)reader["Gia"]
                            });
                        }
                    }
                }
            }

            if (order == null) return NotFound();
            return View(order);
        }
    }
}
