using Ban_Banh.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Ban_Banh.Controllers
{
    [Authorize(Policy = "OrderOnly")]
    public class OrdersController : Controller
    {
        private readonly string _connectionString;

        public OrdersController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("BanBanhDB");
        }

        public IActionResult Index()
        {
            var orders = new List<OrderWithProductsViewModel>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                string sql = @"
                SELECT 
                    o.Id AS OrderId,
                    o.AccountId,
                    os.Id AS StatusId,
                    os.StatusName AS Status,
                    o.CreatedAt,
                    o.UpdatedAt,
                    a.FullName,
                    a.Email,
                    a.Phone,
                    a.Address,
                    b.TenBanh,
                    od.Quantity,
                    b.Gia,
                    od.BatchCode,
                    i.WarehouseLocationId,
                    wl.MaViTri AS WarehouseLocation,
                    wl.TenKhu AS WarehouseZone,
                    s.TenNhaCungCap AS SupplierName
                FROM [Order] o
                INNER JOIN Account a ON o.AccountId = a.Id
                INNER JOIN OrderDetail od ON od.OrderId = o.Id
                INNER JOIN Banh b ON od.BanhId = b.Id
                LEFT JOIN Inventory i ON i.BanhId = b.Id AND (od.BatchCode IS NULL OR i.BatchCode = od.BatchCode)
                LEFT JOIN Supplier s ON i.SupplierId = s.Id
                LEFT JOIN WarehouseLocation wl ON i.WarehouseLocationId = wl.Id
                INNER JOIN OrderStatus os ON o.StatusId = os.Id
                WHERE os.StatusName = N'Pending'
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
                                StatusId = (int)reader["StatusId"],
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
                            Gia = (decimal)reader["Gia"],
                            BatchCode = reader["BatchCode"]?.ToString(),
                            SupplierName = reader["SupplierName"]?.ToString(),
                            WarehouseLocation = reader["WarehouseLocation"]?.ToString(),
                            WarehouseZone = reader["WarehouseZone"]?.ToString()
                        });
                    }

                    orders = orderDict.Values.ToList();
                }
            }

            // ✅ Truyền thêm danh sách trạng thái để dùng cho dropdown
            ViewBag.AllStatuses = GetAllStatuses();

            return View(orders);
        }

        private List<OrderStatusViewModel> GetAllStatuses()
        {
            var statuses = new List<OrderStatusViewModel>();
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                string sql = "SELECT Id, StatusName FROM OrderStatus WHERE Id IN (1, 2) ORDER BY Id";
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        statuses.Add(new OrderStatusViewModel
                        {
                            Id = (int)reader["Id"],
                            StatusName = reader["StatusName"].ToString()
                        });
                    }
                }
            }
            return statuses;
        }


        // ✅ Cập nhật trạng thái đơn hàng
        [HttpPost]
        public IActionResult UpdateStatus(int orderId, int statusId)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                string sql = "UPDATE [Order] SET StatusId=@StatusId, UpdatedAt=GETDATE() WHERE Id=@Id";
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@StatusId", statusId);
                    cmd.Parameters.AddWithValue("@Id", orderId);
                    cmd.ExecuteNonQuery();
                }
            }

            return RedirectToAction("Index");
        }
    }
}
