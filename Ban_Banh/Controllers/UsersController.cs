using Ban_Banh.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;

namespace Ban_Banh.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class UsersController : Controller
    {
        private readonly string _connectionString;

        public UsersController(IConfiguration configuration)
        {
            // Lấy chuỗi kết nối từ appsettings.json
            _connectionString = configuration.GetConnectionString("BanBanhDB");
        }

        // Hiển thị danh sách người dùng
        public IActionResult Index()
        {
            var users = new List<UserViewModel>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                string sql = @"
                    SELECT Id, FullName, Email, Phone, Address, IsLocked
                    FROM Account
                    ORDER BY Id DESC";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        users.Add(new UserViewModel
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            FullName = reader.GetString(reader.GetOrdinal("FullName")),
                            Email = reader.GetString(reader.GetOrdinal("Email")),
                            Phone = reader.IsDBNull(reader.GetOrdinal("Phone")) ? null : reader.GetString(reader.GetOrdinal("Phone")),
                            Address = reader.IsDBNull(reader.GetOrdinal("Address")) ? null : reader.GetString(reader.GetOrdinal("Address")),
                            IsLocked = reader.GetBoolean(reader.GetOrdinal("IsLocked"))
                        });
                    }
                }
            }

            return View(users);
        }

        // POST: /Users/ToggleLock/5
        [HttpPost]
        public IActionResult ToggleLock(int id)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                string sql = @"
                    UPDATE Account
                    SET IsLocked = CASE WHEN IsLocked = 1 THEN 0 ELSE 1 END
                    WHERE Id = @Id";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.ExecuteNonQuery();
                }
            }

            return RedirectToAction("Index");
        }
    }
}
