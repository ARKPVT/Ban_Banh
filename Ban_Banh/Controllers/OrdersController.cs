using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Ban_Banh.Models;
using System.Data;
using System.Collections.Generic;
using System.Linq;

namespace Ban_Banh.Controllers
{
    public class OrdersController : Controller
    {
        private readonly string _connectionString;

        public OrdersController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("BanBanhDB");
        }

        // Hiển thị tất cả đơn hàng với sản phẩm
        public IActionResult Index()
        {
            var orders = new List<OrderWithProductsViewModel>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                string sql = @"
                    SELECT 
                        o.Id AS OrderId, o.AccountId, o.Status, o.CreatedAt, o.UpdatedAt,
                        a.FullName, a.Email, a.Phone, a.Address,
                        b.TenBanh, od.Quantity, b.Gia
                    FROM [Order] o
                    INNER JOIN Account a ON o.AccountId = a.Id
                    INNER JOIN OrderDetail od ON od.OrderId = o.Id
                    INNER JOIN Banh b ON od.BanhId = b.Id
                    ORDER BY o.CreatedAt DESC";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
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
                                AccountId = (int)reader["AccountId"],
                                FullName = reader["FullName"].ToString(),
                                Email = reader["Email"].ToString(),
                                Phone = reader["Phone"]?.ToString(),
                                Address = reader["Address"]?.ToString(),
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

            return View(orders);
        }

        // Cập nhật trạng thái đơn hàng
        [HttpPost]
        public IActionResult UpdateStatus(int orderId, string status)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                string sql = "UPDATE [Order] SET Status=@Status, UpdatedAt=GETDATE() WHERE Id=@Id";
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Status", status);
                    cmd.Parameters.AddWithValue("@Id", orderId);
                    cmd.ExecuteNonQuery();
                }
            }

            return RedirectToAction("Index");
        }
    }
}
