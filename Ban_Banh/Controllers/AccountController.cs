using Ban_Banh.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;


namespace Ban_Banh.Controllers
{
    public class AccountController : Controller
    {
        private readonly IConfiguration _configuration;

        public AccountController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // GET: /Account/Register/
        public IActionResult Register()
        {
            return View();
        }
        public IActionResult AccessDenied()
        {
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(Account model)
        {
            if (ModelState.IsValid)
            {
                string? connectionString = _configuration.GetConnectionString("BanBanhDB");

                try
                {
                    using (SqlConnection conn = new SqlConnection(connectionString))
                    {
                        conn.Open();

                        // 1. Kiểm tra email có tồn tại chưa
                        string checkQuery = "SELECT COUNT(*) FROM Account WHERE Email = @Email";
                        using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn))
                        {
                            checkCmd.Parameters.AddWithValue("@Email", model.Email);
                            int count = (int)checkCmd.ExecuteScalar();
                            if (count > 0)
                            {
                                ModelState.AddModelError("Email", "Email này đã được sử dụng.");
                                return View(model);
                            }
                        }

                        // 2. Hash mật khẩu
                        string hashedPassword = HashPassword(model.Password);

                        // 3. Lưu thông tin
                        // ✅ Lấy RoleId mặc định cho User
                        string roleQuery = "SELECT TOP 1 Id FROM Role WHERE RoleName = 'User'";
                        int roleId = 0;
                        using (SqlCommand roleCmd = new SqlCommand(roleQuery, conn))
                        {
                            object result = roleCmd.ExecuteScalar();
                            roleId = result != null ? Convert.ToInt32(result) : 0;
                        }

                        // ✅ Thêm tài khoản với RoleId
                        string query = @"INSERT INTO Account 
                 (FullName, Email, Password, AvatarUrl, Phone, Address, RoleId) 
                 VALUES (@FullName, @Email, @Password, @AvatarUrl, @Phone, @Address, @RoleId)";
                        using (SqlCommand cmd = new SqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@FullName", model.FullName);
                            cmd.Parameters.AddWithValue("@Email", model.Email);
                            cmd.Parameters.AddWithValue("@Password", hashedPassword);
                            cmd.Parameters.AddWithValue("@AvatarUrl", string.IsNullOrEmpty(model.AvatarUrl) ?
                                "https://tse1.mm.bing.net/th/id/OIP.5yvzs8ftQYN8o7dnmNb-EAHaEK?cb=12&rs=1&pid=ImgDetMain&o=7&rm=3" : model.AvatarUrl);
                            cmd.Parameters.AddWithValue("@Phone", string.IsNullOrEmpty(model.Phone) ? (object)DBNull.Value : model.Phone);
                            cmd.Parameters.AddWithValue("@Address", string.IsNullOrEmpty(model.Address) ? (object)DBNull.Value : model.Address);
                            cmd.Parameters.AddWithValue("@RoleId", roleId);

                            int rows = cmd.ExecuteNonQuery();
                            if (rows <= 0)
                            {
                                ModelState.AddModelError("", "Không thể lưu tài khoản. Vui lòng thử lại.");
                                return View(model);
                            }
                        }


                        // ✅ Sau khi đăng ký xong -> lấy Id vừa tạo
                        string getIdQuery = "SELECT Id FROM Account WHERE Email = @Email";
                        using (SqlCommand getIdCmd = new SqlCommand(getIdQuery, conn))
                        {
                            getIdCmd.Parameters.AddWithValue("@Email", model.Email);
                            int accountId = (int)getIdCmd.ExecuteScalar();

                            // ✅ Lưu session
                            HttpContext.Session.SetInt32("AccountId", accountId);
                            HttpContext.Session.SetString("UserEmail", model.Email);
                            HttpContext.Session.SetString("FullName", model.FullName);
                            HttpContext.Session.SetString("AvatarUrl", string.IsNullOrEmpty(model.AvatarUrl) ?
                                "https://tse1.mm.bing.net/th/id/OIP.5yvzs8ftQYN8o7dnmNb-EAHaEK?cb=12&rs=1&pid=ImgDetMain&o=7&rm=3" : model.AvatarUrl);
                        }

                        TempData["Message"] = "Đăng ký thành công! Bạn đã được đăng nhập.";
                        return RedirectToAction("Index", "Banh");
                    }
                }
                catch (SqlException sqlEx)
                {
                    ModelState.AddModelError("", "Lỗi cơ sở dữ liệu: " + sqlEx.Message);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Có lỗi xảy ra: " + ex.Message);
                }
            }

            return View(model);
        }

        // GET: /Account/Login
        public IActionResult Login()
        {
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password)

        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError("", "Vui lòng nhập email và mật khẩu.");
                return View();
            }

            string? connectionString = _configuration.GetConnectionString("BanBanhDB");

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    string hashedPassword = HashPassword(password);

                    // ✅ Bổ sung cột IsLocked để kiểm tra tài khoản bị khóa
                    string query = @"SELECT Id, FullName, AvatarUrl, Phone, Address, Email, IsLocked 
                             FROM Account 
                             WHERE Email = @Email AND Password = @Password";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Email", email);
                        cmd.Parameters.AddWithValue("@Password", hashedPassword);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                bool isLocked = Convert.ToBoolean(reader["IsLocked"]);
                                if (isLocked)
                                {
                                    // ❌ Nếu tài khoản bị khóa → không cho đăng nhập
                                    ModelState.AddModelError("", "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên.");
                                    return View();
                                }

                                // Nếu tài khoản hoạt động bình thường → cho đăng nhập
                                int accountId = Convert.ToInt32(reader["Id"]);
                                string fullName = reader["FullName"]?.ToString() ?? string.Empty;
                                string avatarUrl = reader["AvatarUrl"] is DBNull or null ? string.Empty : reader["AvatarUrl"]!.ToString()!;
                                string? phone = reader["Phone"] == DBNull.Value ? null : reader["Phone"]?.ToString();
                                string? address = reader["Address"] == DBNull.Value ? null : reader["Address"]?.ToString();
                                string userEmail = reader["Email"] == DBNull.Value ? string.Empty : reader["Email"]!.ToString()!;

                                // ✅ Lưu session
                                HttpContext.Session.SetInt32("AccountId", accountId);
                                HttpContext.Session.SetString("UserEmail", userEmail);
                                HttpContext.Session.SetString("FullName", fullName);
                                HttpContext.Session.SetString("AvatarUrl", string.IsNullOrEmpty(avatarUrl)
                                    ? "https://tse1.mm.bing.net/th/id/OIP.5yvzs8ftQYN8o7dnmNb-EAHaEK?cb=12&rs=1&pid=ImgDetMain&o=7&rm=3"
                                    : avatarUrl);
                                HttpContext.Session.SetString("Phone", string.IsNullOrEmpty(phone) ? "" : phone);
                                HttpContext.Session.SetString("Address", string.IsNullOrEmpty(address) ? "" : address);

                                // ✅ Lấy quyền (Role) của tài khoản
                                reader.Close(); // đóng reader trước khi query khác

                                string roleQuery = @"SELECT R.RoleName 
                     FROM Account A 
                     LEFT JOIN Role R ON A.RoleId = R.Id
                     WHERE A.Id = @AccountId";
                                using (SqlCommand roleCmd = new SqlCommand(roleQuery, conn))
                                {
                                    roleCmd.Parameters.AddWithValue("@AccountId", accountId);
                                    string roleName = roleCmd.ExecuteScalar()?.ToString() ?? "User";

                                    // Lưu RoleName vào Session để tiện dùng sau này
                                    HttpContext.Session.SetString("RoleName", roleName);
                                    // ✅ Đăng nhập vào hệ thống Authentication (để dùng [Authorize] / User.IsInRole)
                                    var claims = new List<Claim>
{
    new Claim(ClaimTypes.Name, userEmail),
    new Claim(ClaimTypes.NameIdentifier, accountId.ToString()),
    new Claim(ClaimTypes.Role, roleName) // gắn role từ DB
};

                                    var claimsIdentity = new ClaimsIdentity(
                                        claims,
                                        CookieAuthenticationDefaults.AuthenticationScheme
                                    );

                                    var authProperties = new AuthenticationProperties
                                    {
                                        IsPersistent = true, // Lưu cookie
                                        ExpiresUtc = DateTimeOffset.UtcNow.AddHours(3)
                                    };

                                    await HttpContext.SignInAsync(
                                        CookieAuthenticationDefaults.AuthenticationScheme,
                                        new ClaimsPrincipal(claimsIdentity),
                                        authProperties
                                    );


                                    // Điều hướng theo quyền
                                    if (roleName.Equals("Admin", StringComparison.OrdinalIgnoreCase))
                                    {
                                        TempData["Message"] = "Đăng nhập thành công! (Admin)";
                                        return RedirectToAction("Index", "Dashboard");
                                    }
                                    else if (roleName.Equals("Kho", StringComparison.OrdinalIgnoreCase))
                                    {
                                        TempData["Message"] = "Đăng nhập thành công! (Kho)";
                                        return RedirectToAction("Index", "Inventory");
                                    }
                                    else if (roleName.Equals("Bep", StringComparison.OrdinalIgnoreCase))
                                    {
                                        TempData["Message"] = "Đăng nhập thành công! (Bếp)";
                                        return RedirectToAction("Index", "Bep");
                                    }
                                    else if (roleName.Equals("Ship", StringComparison.OrdinalIgnoreCase))
                                    {
                                        TempData["Message"] = "Đăng nhập thành công! (Ship)";
                                        return RedirectToAction("Index", "Driver");
                                    }
                                    else if (roleName.Equals("Order", StringComparison.OrdinalIgnoreCase))
                                    {
                                        TempData["Message"] = "Đăng nhập thành công! (Order)";
                                        return RedirectToAction("Index", "Orders");
                                    }
                                    else
                                    {
                                        TempData["Message"] = "Đăng nhập thành công!";
                                        return RedirectToAction("Index", "Banh");
                                    }
                                }

                            }
                            else
                            {
                                ModelState.AddModelError("", "Email hoặc mật khẩu không đúng.");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Có lỗi xảy ra: " + ex.Message);
            }

            return View();
        }




        // GET: /Account/Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData["Message"] = "Bạn đã đăng xuất.";
            return RedirectToAction("Login");
        }

        // GET: /Account/Profile
        public IActionResult Profile()
        {
            string? email = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Login");
            }

            string? connectionString = _configuration.GetConnectionString("BanBanhDB");
            ProfileViewModel? account = null;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT Id, FullName, Email, AvatarUrl, Phone, Address FROM Account WHERE Email = @Email";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            account = new ProfileViewModel
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                FullName = reader["FullName"] == DBNull.Value ? string.Empty : reader["FullName"]!.ToString()!,
                                Email = reader["Email"] == DBNull.Value ? string.Empty : reader["Email"]!.ToString()!,
                                AvatarUrl = reader["AvatarUrl"].ToString(),
                                Phone = reader["Phone"]?.ToString(),
                                Address = reader["Address"]?.ToString()
                            };
                        }
                    }
                }
            }

            return View(account);
        }

        // POST: /Account/Profile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Profile(ProfileViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            string? connectionString = _configuration.GetConnectionString("BanBanhDB");

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"UPDATE Account 
                         SET FullName=@FullName, AvatarUrl=@AvatarUrl, Phone=@Phone, Address=@Address 
                         WHERE Id=@Id";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", model.Id);
                    cmd.Parameters.AddWithValue("@FullName", model.FullName);
                    cmd.Parameters.AddWithValue("@AvatarUrl", string.IsNullOrEmpty(model.AvatarUrl) ?
                        "https://tse1.mm.bing.net/th/id/OIP.5yvzs8ftQYN8o7dnmNb-EAHaEK?cb=12&rs=1&pid=ImgDetMain&o=7&rm=3" : model.AvatarUrl);
                    cmd.Parameters.AddWithValue("@Phone", string.IsNullOrEmpty(model.Phone) ? (object)DBNull.Value : model.Phone);
                    cmd.Parameters.AddWithValue("@Address", string.IsNullOrEmpty(model.Address) ? (object)DBNull.Value : model.Address);

                    int rows = cmd.ExecuteNonQuery();
                    if (rows == 0)
                    {
                        TempData["Message"] = "⚠️ Không cập nhật được hồ sơ.";
                        return View(model);
                    }
                }
            }

            // cập nhật session đầy đủ sau khi update hồ sơ
            HttpContext.Session.SetString("FullName", model.FullName);
            HttpContext.Session.SetString("AvatarUrl", string.IsNullOrEmpty(model.AvatarUrl) ?
                "https://tse1.mm.bing.net/th/id/OIP.5yvzs8ftQYN8o7dnmNb-EAHaEK?cb=12&rs=1&pid=ImgDetMain&o=7&rm=3" : model.AvatarUrl);
            HttpContext.Session.SetString("Phone", string.IsNullOrEmpty(model.Phone) ? "" : model.Phone);
            HttpContext.Session.SetString("Address", string.IsNullOrEmpty(model.Address) ? "" : model.Address);


            TempData["Message"] = "Cập nhật hồ sơ thành công!";
            return RedirectToAction("Profile");
        }

        /// <summary>
        /// Hàm hash mật khẩu SHA256
        /// </summary>
        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                foreach (var b in bytes)
                    builder.Append(b.ToString("x2"));
                return builder.ToString();
            }
        }
        /////////////////////////////////////////////////////////////////////////////////////////////////////////
        // GET: /Account/Profile
        public IActionResult Profile_ad()
        {
            string? email = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Login");
            }

            string? connectionString = _configuration.GetConnectionString("BanBanhDB");
            ProfileViewModel? account = null;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT Id, FullName, Email, AvatarUrl, Phone, Address FROM Account WHERE Email = @Email";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            account = new ProfileViewModel
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                FullName = reader["FullName"] == DBNull.Value ? string.Empty : reader["FullName"]!.ToString()!,
                                Email = reader["Email"] == DBNull.Value ? string.Empty : reader["Email"]!.ToString()!,
                                AvatarUrl = reader["AvatarUrl"].ToString(),
                                Phone = reader["Phone"]?.ToString(),
                                Address = reader["Address"]?.ToString()
                            };
                        }
                    }
                }
            }

            return View(account);
        }

        // POST: /Account/Profile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Profile_ad(ProfileViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            string? connectionString =  _configuration.GetConnectionString("BanBanhDB");

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"UPDATE Account 
                         SET FullName=@FullName, AvatarUrl=@AvatarUrl, Phone=@Phone, Address=@Address 
                         WHERE Id=@Id";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", model.Id);
                    cmd.Parameters.AddWithValue("@FullName", model.FullName);
                    cmd.Parameters.AddWithValue("@AvatarUrl", string.IsNullOrEmpty(model.AvatarUrl) ?
                        "https://tse1.mm.bing.net/th/id/OIP.5yvzs8ftQYN8o7dnmNb-EAHaEK?cb=12&rs=1&pid=ImgDetMain&o=7&rm=3" : model.AvatarUrl);
                    cmd.Parameters.AddWithValue("@Phone", string.IsNullOrEmpty(model.Phone) ? (object)DBNull.Value : model.Phone);
                    cmd.Parameters.AddWithValue("@Address", string.IsNullOrEmpty(model.Address) ? (object)DBNull.Value : model.Address);

                    int rows = cmd.ExecuteNonQuery();
                    if (rows == 0)
                    {
                        TempData["Message"] = "⚠️ Không cập nhật được hồ sơ.";
                        return View(model);
                    }
                }
            }

            // cập nhật session đầy đủ sau khi update hồ sơ
            HttpContext.Session.SetString("FullName", model.FullName);
            HttpContext.Session.SetString("AvatarUrl", string.IsNullOrEmpty(model.AvatarUrl) ?
                "https://tse1.mm.bing.net/th/id/OIP.5yvzs8ftQYN8o7dnmNb-EAHaEK?cb=12&rs=1&pid=ImgDetMain&o=7&rm=3" : model.AvatarUrl);
            HttpContext.Session.SetString("Phone", string.IsNullOrEmpty(model.Phone) ? "" : model.Phone);
            HttpContext.Session.SetString("Address", string.IsNullOrEmpty(model.Address) ? "" : model.Address);


            TempData["Message"] = "Cập nhật hồ sơ thành công!";
            return RedirectToAction("Profile_ad");
        }

    }
}
