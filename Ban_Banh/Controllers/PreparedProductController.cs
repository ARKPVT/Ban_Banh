using Ban_Banh.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Ban_Banh.Controllers
{
    [Authorize(Policy = "KhoOnly")]
    public class PreparedProductController : Controller
    {
        private readonly string _connectionString;

        public PreparedProductController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("BanBanhDB");
        }

        // Hiển thị danh sách OrderDetail của các đơn hàng có StatusId = 2
        public IActionResult Index()
        {
            var list = new List<PreparedProductViewModel>();

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    string sql = @"
                        SELECT 
                            od.Id AS OrderDetailId,
                            o.Id AS OrderId,
                            b.TenBanh AS BanhName,
                            od.Quantity,
                            ISNULL(pp.PreparedCount, 0) AS PreparedCount,
                            s.StatusName AS StatusName
                        FROM OrderDetail od
                        JOIN [Order] o ON o.Id = od.OrderId
                        JOIN Banh b ON b.Id = od.BanhId
                        JOIN OrderDetailStatus s ON s.Id = od.StatusId
                        OUTER APPLY (
                            SELECT COUNT(*) AS PreparedCount 
                            FROM PreparedProduct p 
                            WHERE p.OrderDetailId = od.Id
                        ) pp
                        WHERE o.StatusId = 2
                    ";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new PreparedProductViewModel
                            {
                                OrderDetailId = reader.GetInt32(reader.GetOrdinal("OrderDetailId")),
                                OrderId = reader.GetInt32(reader.GetOrdinal("OrderId")),
                                BanhName = reader["BanhName"].ToString(),
                                Quantity = reader.GetInt32(reader.GetOrdinal("Quantity")),
                                PreparedCount = reader.GetInt32(reader.GetOrdinal("PreparedCount")),
                                StatusName = reader["StatusName"].ToString()
                            });
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                ViewBag.SqlError = BuildSqlError(ex);
            }
            catch (Exception ex)
            {
                ViewBag.SqlError = "[Unhandled Error] " + ex.Message;
            }

            return View(list);
        }

        [HttpPost]
        public IActionResult SavePreparedProduct(int orderDetailId, long productInstanceId)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    // Kiểm tra trùng ProductInstanceId
                    string checkSql = "SELECT COUNT(*) FROM PreparedProduct WHERE ProductInstanceId = @ProductInstanceId";
                    using (SqlCommand checkCmd = new SqlCommand(checkSql, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@ProductInstanceId", productInstanceId);
                        int exists = (int)checkCmd.ExecuteScalar();

                        if (exists > 0)
                        {
                            return Json(new { success = false, message = "Mã sản phẩm này đã được nhập trước đó!" });
                        }
                    }

                    // Thêm bản ghi mới vào PreparedProduct
                    string insertSql = @"
                        INSERT INTO PreparedProduct (OrderDetailId, ProductInstanceId, DatePrepared)
                        VALUES (@OrderDetailId, @ProductInstanceId, GETDATE());
                    ";

                    using (SqlCommand insertCmd = new SqlCommand(insertSql, conn))
                    {
                        insertCmd.Parameters.AddWithValue("@OrderDetailId", orderDetailId);
                        insertCmd.Parameters.AddWithValue("@ProductInstanceId", productInstanceId);
                        insertCmd.ExecuteNonQuery();
                    }
                }

                return Json(new { success = true });
            }
            catch (SqlException ex)
            {
                // Trả JSON lỗi chi tiết để JS hiện alert (không làm sập app)
                return Json(new { success = false, message = BuildSqlError(ex) });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "[Unhandled Error] " + ex.Message });
            }
        }

        // ===== Helper: format lỗi SQL dễ đọc, vẫn không thay đổi logic nghiệp vụ =====
        private string BuildSqlError(SqlException ex)
        {
            var sb = new StringBuilder();
            sb.AppendLine("[SQL Error] " + ex.Message);
            try
            {
                foreach (SqlError e in ex.Errors)
                {
                    sb.AppendLine($" • ({e.Number}) {e.Message} | Server: {e.Server} | Proc: {e.Procedure} | Line: {e.LineNumber}");
                }
            }
            catch { /* ignore */ }
            return sb.ToString();
        }
    }
}
