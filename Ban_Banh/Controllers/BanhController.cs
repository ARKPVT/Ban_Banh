using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using Ban_Banh.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using System;

namespace Ban_Banh.Controllers
{
    public class BanhController : Controller
    {
        private readonly string _connectionString;

        public BanhController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("BanBanhDB");
        }

        // ===============================
        // 1️⃣ Hiển thị danh sách bánh
        // ===============================
        public IActionResult Index(int? categoryId, decimal? minPrice, decimal? maxPrice)
        {
            List<Banh> list = new List<Banh>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                string sql = @"
    SELECT 
        b.Id, 
        b.TenBanh, 
        b.MoTa, 
        b.Gia, 
        b.HinhAnh, 
        c.TenDanhMuc, 
        ISNULL(SUM(i.SoLuong), 0) AS TongSoLuong
    FROM Banh b
    LEFT JOIN Category c ON b.CategoryId = c.Id
    LEFT JOIN Inventory i ON b.Id = i.BanhId
    WHERE 1=1";

                if (categoryId != null)
                    sql += " AND b.CategoryId = @CategoryId";
                if (minPrice != null)
                    sql += " AND b.Gia >= @MinPrice";
                if (maxPrice != null)
                    sql += " AND b.Gia <= @MaxPrice";

                // ⚙️ nhóm theo Banh để cộng dồn số lượng các lô khác nhau
                sql += " GROUP BY b.Id, b.TenBanh, b.MoTa, b.Gia, b.HinhAnh, c.TenDanhMuc";

                SqlCommand cmd = new SqlCommand(sql, conn);
                if (categoryId != null)
                    cmd.Parameters.AddWithValue("@CategoryId", categoryId.Value);
                if (minPrice != null)
                    cmd.Parameters.AddWithValue("@MinPrice", minPrice.Value);
                if (maxPrice != null)
                    cmd.Parameters.AddWithValue("@MaxPrice", maxPrice.Value);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Banh
                        {
                            Id = reader.GetInt32(0),
                            TenBanh = reader.GetString(1),
                            MoTa = reader.IsDBNull(2) ? "" : reader.GetString(2),
                            Gia = reader.GetDecimal(3),
                            HinhAnh = reader.IsDBNull(4) ? "noimage.jpg" : reader.GetString(4),
                            CategoryName = reader.IsDBNull(5) ? "" : reader.GetString(5),
                            SoLuongTon = reader.IsDBNull(6) ? 0 : reader.GetInt32(6) // ✅ tổng số lượng tồn kho của tất cả lô
                        });
                    }
                }
            }

            ViewData["CartQuantity"] = GetCartQuantity();
            ViewData["Categories"] = GetCategories();
            ViewData["SelectedCategoryId"] = categoryId;
            ViewData["SelectedMinPrice"] = minPrice;
            ViewData["SelectedMaxPrice"] = maxPrice;

            return View(list);
        }


        // ===============================
        // 2️⃣ Lấy danh mục bánh
        // ===============================
        private List<Category> GetCategories()
        {
            List<Category> categories = new List<Category>();
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                string sql = "SELECT Id, TenDanhMuc FROM Category";
                SqlCommand cmd = new SqlCommand(sql, conn);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        categories.Add(new Category
                        {
                            Id = reader.GetInt32(0),
                            TenDanhMuc = reader.GetString(1)
                        });
                    }
                }
            }
            return categories;
        }

        // ===============================
        // 3️⃣ Thêm sản phẩm vào giỏ
        // ===============================
        public IActionResult AddToCart(int id)
        {
            int? accountId = HttpContext.Session.GetInt32("AccountId");

            if (accountId == null)
            {
                TempData["Message"] = "⚠️ Bạn cần đăng nhập để thêm sản phẩm vào giỏ hàng.";
                return RedirectToAction("Login", "Account");
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    // ✅ Kiểm tra còn hàng không
                    string checkStockSql = "SELECT SUM(SoLuong) FROM Inventory WHERE BanhId=@BanhId";
                    SqlCommand checkStockCmd = new SqlCommand(checkStockSql, conn);
                    checkStockCmd.Parameters.AddWithValue("@BanhId", id);
                    int soLuongTon = Convert.ToInt32(checkStockCmd.ExecuteScalar() ?? 0);

                    if (soLuongTon <= 0)
                    {
                        TempData["Message"] = "❌ Sản phẩm này đã hết hàng!";
                        return RedirectToAction("Index");
                    }

                    string checkSql = "SELECT Quantity FROM Cart WHERE AccountId = @AccountId AND BanhId = @BanhId";
                    SqlCommand checkCmd = new SqlCommand(checkSql, conn);
                    checkCmd.Parameters.AddWithValue("@AccountId", accountId.Value);
                    checkCmd.Parameters.AddWithValue("@BanhId", id);

                    var result = checkCmd.ExecuteScalar();

                    if (result != null)
                    {
                        string updateSql = "UPDATE Cart SET Quantity = Quantity + 1 WHERE AccountId = @AccountId AND BanhId = @BanhId";
                        SqlCommand updateCmd = new SqlCommand(updateSql, conn);
                        updateCmd.Parameters.AddWithValue("@AccountId", accountId.Value);
                        updateCmd.Parameters.AddWithValue("@BanhId", id);
                        updateCmd.ExecuteNonQuery();

                        TempData["Message"] = "✅ Sản phẩm đã được cập nhật số lượng trong giỏ hàng!";
                    }
                    else
                    {
                        string insertSql = "INSERT INTO Cart (AccountId, BanhId, Quantity) VALUES (@AccountId, @BanhId, 1)";
                        SqlCommand insertCmd = new SqlCommand(insertSql, conn);
                        insertCmd.Parameters.AddWithValue("@AccountId", accountId.Value);
                        insertCmd.Parameters.AddWithValue("@BanhId", id);
                        insertCmd.ExecuteNonQuery();

                        TempData["Message"] = "🛒 Sản phẩm đã được thêm vào giỏ hàng!";
                    }
                }
            }
            catch
            {
                TempData["Message"] = "❌ Có lỗi xảy ra, vui lòng thử lại!";
            }

            return RedirectToAction("Index");
        }

        // ===============================
        // 4️⃣ Xem giỏ hàng
        // ===============================
        public IActionResult Cart()
        {
            int? accountId = HttpContext.Session.GetInt32("AccountId");
            if (accountId == null)
            {
                TempData["Message"] = "⚠️ Bạn cần đăng nhập để xem giỏ hàng.";
                return RedirectToAction("Login", "Account");
            }

            List<Cart> cartItems = new List<Cart>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                string sql = @"
                    SELECT c.Id, c.AccountId, c.BanhId, c.Quantity, c.CreatedAt,
                           b.TenBanh, b.MoTa, b.Gia, b.HinhAnh
                    FROM Cart c
                    INNER JOIN Banh b ON c.BanhId = b.Id
                    WHERE c.AccountId = @AccountId";
                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@AccountId", accountId.Value);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        cartItems.Add(new Cart
                        {
                            Id = reader.GetInt32(0),
                            AccountId = reader.GetInt32(1),
                            BanhId = reader.GetInt32(2),
                            Quantity = reader.GetInt32(3),
                            CreatedAt = reader.GetDateTime(4),
                            Banh = new Banh
                            {
                                Id = reader.GetInt32(2),
                                TenBanh = reader.GetString(5),
                                MoTa = reader.IsDBNull(6) ? "" : reader.GetString(6),
                                Gia = reader.GetDecimal(7),
                                HinhAnh = reader.IsDBNull(8) ? "noimage.jpg" : reader.GetString(8)
                            }
                        });
                    }
                }
            }

            ViewData["CartQuantity"] = GetCartQuantity();
            return View(cartItems);
        }

        // ===============================
        // 5️⃣ Cập nhật giỏ hàng
        // ===============================

        [HttpPost]
        public IActionResult UpdateCartAjax(int cartId, int quantity)
        {
            if (quantity <= 0)
                return BadRequest("⚠️ Số lượng phải là số nguyên dương!");

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                string getBanhSql = "SELECT BanhId FROM Cart WHERE Id=@CartId";
                SqlCommand getBanhCmd = new SqlCommand(getBanhSql, conn);
                getBanhCmd.Parameters.AddWithValue("@CartId", cartId);
                object banhObj = getBanhCmd.ExecuteScalar();

                if (banhObj == null)
                    return NotFound("❌ Không tìm thấy sản phẩm trong giỏ hàng.");

                int banhId = Convert.ToInt32(banhObj);

                string checkStockSql = "SELECT SUM(SoLuong) FROM Inventory WHERE BanhId=@BanhId";
                SqlCommand checkStockCmd = new SqlCommand(checkStockSql, conn);
                checkStockCmd.Parameters.AddWithValue("@BanhId", banhId);
                int soLuongTon = Convert.ToInt32(checkStockCmd.ExecuteScalar() ?? 0);

                if (soLuongTon <= 0)
                    return BadRequest("❌ Sản phẩm đã hết hàng!");

                if (quantity > soLuongTon)
                {
                    string fixSql = "UPDATE Cart SET Quantity=@Quantity WHERE Id=@CartId";
                    SqlCommand fixCmd = new SqlCommand(fixSql, conn);
                    fixCmd.Parameters.AddWithValue("@Quantity", soLuongTon);
                    fixCmd.Parameters.AddWithValue("@CartId", cartId);
                    fixCmd.ExecuteNonQuery();

                    TempData["Message"] = $"⚠️ Chỉ còn {soLuongTon} sản phẩm trong kho!";
                    return BadRequest(TempData.Peek("Message"));
                }


                string sql = "UPDATE Cart SET Quantity=@Quantity WHERE Id=@CartId";
                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Quantity", quantity);
                cmd.Parameters.AddWithValue("@CartId", cartId);
                cmd.ExecuteNonQuery();
            }

            return Ok();
        }




        public IActionResult RemoveFromCart(int cartId)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                string sql = "DELETE FROM Cart WHERE Id = @CartId";
                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@CartId", cartId);
                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("Cart");
        }

        public IActionResult IncreaseQuantity(int cartId)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                // 🔹 Lấy BanhId và Quantity hiện tại trong giỏ
                string getCartSql = "SELECT BanhId, Quantity FROM Cart WHERE Id=@CartId";
                SqlCommand getCartCmd = new SqlCommand(getCartSql, conn);
                getCartCmd.Parameters.AddWithValue("@CartId", cartId);

                int banhId = 0;
                int quantity = 0;

                using (var reader = getCartCmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        banhId = reader.GetInt32(0);
                        quantity = reader.GetInt32(1);
                    }
                }

                // 🔹 Lấy số lượng tồn kho
                string checkStockSql = "SELECT SUM(SoLuong) FROM Inventory WHERE BanhId=@BanhId";
                SqlCommand checkStockCmd = new SqlCommand(checkStockSql, conn);
                checkStockCmd.Parameters.AddWithValue("@BanhId", banhId);
                int soLuongTon = Convert.ToInt32(checkStockCmd.ExecuteScalar() ?? 0);


                if (quantity >= soLuongTon)
                {
                    TempData["Message"] = "⚠️ Số lượng bạn chọn đã đạt giới hạn tồn kho!";
                }
                else
                {
                    string updateSql = "UPDATE Cart SET Quantity = Quantity + 1 WHERE Id=@CartId";
                    SqlCommand updateCmd = new SqlCommand(updateSql, conn);
                    updateCmd.Parameters.AddWithValue("@CartId", cartId);
                    updateCmd.ExecuteNonQuery();
                }
            }

            return RedirectToAction("Cart");
        }


        public IActionResult DecreaseQuantity(int cartId)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                string getSql = "SELECT Quantity FROM Cart WHERE Id=@CartId";
                SqlCommand getCmd = new SqlCommand(getSql, conn);
                getCmd.Parameters.AddWithValue("@CartId", cartId);
                int quantity = Convert.ToInt32(getCmd.ExecuteScalar());

                if (quantity > 1)
                {
                    string sql = "UPDATE Cart SET Quantity = Quantity - 1 WHERE Id = @CartId";
                    SqlCommand cmd = new SqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@CartId", cartId);
                    cmd.ExecuteNonQuery();
                }
                else
                {
                    string sql = "DELETE FROM Cart WHERE Id=@CartId";
                    SqlCommand cmd = new SqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@CartId", cartId);
                    cmd.ExecuteNonQuery();
                }
            }
            return RedirectToAction("Cart");
        }

        private int GetCartQuantity()
        {
            int? accountId = HttpContext.Session.GetInt32("AccountId");
            if (accountId == null) return 0;

            int quantity = 0;

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                string sql = "SELECT SUM(Quantity) FROM Cart WHERE AccountId=@AccountId";
                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@AccountId", accountId.Value);
                var result = cmd.ExecuteScalar();
                if (result != DBNull.Value && result != null)
                    quantity = Convert.ToInt32(result);
            }

            return quantity;
        }

        // ===============================
        // 6️⃣ Checkout
        // ===============================
        public IActionResult Checkout()
        {
            int? accountId = HttpContext.Session.GetInt32("AccountId");
            if (accountId == null)
            {
                TempData["Message"] = "⚠️ Bạn cần đăng nhập để thanh toán.";
                return RedirectToAction("Login", "Account");
            }

            var accountInfo = new
            {
                FullName = HttpContext.Session.GetString("FullName"),
                Email = HttpContext.Session.GetString("UserEmail"),
                AvatarUrl = HttpContext.Session.GetString("AvatarUrl"),
            };

            List<Cart> cartItems = new List<Cart>();
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                string sql = @"
                    SELECT c.Id, c.AccountId, c.BanhId, c.Quantity, b.TenBanh, b.Gia
                    FROM Cart c
                    INNER JOIN Banh b ON c.BanhId = b.Id
                    WHERE c.AccountId=@AccountId";
                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@AccountId", accountId.Value);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        cartItems.Add(new Cart
                        {
                            Id = reader.GetInt32(0),
                            AccountId = reader.GetInt32(1),
                            BanhId = reader.GetInt32(2),
                            Quantity = reader.GetInt32(3),
                            Banh = new Banh
                            {
                                Id = reader.GetInt32(2),
                                TenBanh = reader.GetString(4),
                                Gia = reader.GetDecimal(5)
                            }
                        });
                    }
                }
            }

            ViewData["AccountInfo"] = accountInfo;
            return View(cartItems);
        }

        [HttpPost]
        public IActionResult ConfirmCheckout(string FullName, string Email, string Phone, string Address)
        {
            int? accountId = HttpContext.Session.GetInt32("AccountId");
            if (accountId == null)
                return RedirectToAction("Login", "Account");

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                using (SqlTransaction tran = conn.BeginTransaction())
                {
                    try
                    {
                        // ✅ 1. Cập nhật thông tin tài khoản
                        string updateAccountSql = @"
                    UPDATE Account
                    SET 
                        FullName = @FullName,
                        Email = @Email,
                        Phone = @Phone,
                        Address = @Address
                    WHERE Id = @AccountId";
                        using (SqlCommand cmd = new SqlCommand(updateAccountSql, conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@FullName", (object?)FullName ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Email", (object?)Email ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Phone", (object?)Phone ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Address", (object?)Address ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@AccountId", accountId.Value);
                            cmd.ExecuteNonQuery();
                        }

                        // ✅ 2. Cập nhật lại session
                        HttpContext.Session.SetString("FullName", FullName ?? "");
                        HttpContext.Session.SetString("UserEmail", Email ?? "");
                        HttpContext.Session.SetString("Phone", Phone ?? "");
                        HttpContext.Session.SetString("Address", Address ?? "");

                        // ✅ 3. Lấy sản phẩm trong giỏ hàng
                        var cartItems = new List<(int BanhId, int Quantity)>();
                        string selectCart = "SELECT BanhId, Quantity FROM Cart WHERE AccountId=@AccountId";
                        using (SqlCommand cmd = new SqlCommand(selectCart, conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@AccountId", accountId.Value);
                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    cartItems.Add((reader.GetInt32(0), reader.GetInt32(1)));
                                }
                            }
                        }

                        if (cartItems.Count == 0)
                        {
                            TempData["Message"] = "🛒 Giỏ hàng trống, không thể thanh toán!";
                            tran.Rollback();
                            return RedirectToAction("Cart", "Banh");
                        }

                        // ✅ 4. Tạo đơn hàng
                        int orderId;
                        string insertOrder = @"
                    INSERT INTO [Order] (AccountId, CreatedAt)
                    VALUES (@AccountId, @CreatedAt);
                    SELECT SCOPE_IDENTITY();";
                        using (SqlCommand cmd = new SqlCommand(insertOrder, conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@AccountId", accountId.Value);
                            cmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
                            orderId = Convert.ToInt32(cmd.ExecuteScalar());
                        }

                        // ✅ 5. Lưu chi tiết và trừ kho theo FIFO (hạn sử dụng sớm nhất)
                        foreach (var item in cartItems)
                        {
                            int remaining = item.Quantity;

                            // --- Lấy danh sách lô hàng theo hạn sử dụng ---
                            var lots = new List<(int LotId, int SoLuong, string BatchCode)>();
                            string selectLots = @"
                        SELECT Id, SoLuong, BatchCode
                        FROM Inventory
                        WHERE BanhId=@BanhId
                        ORDER BY HanSuDung ASC, Id ASC";
                            using (SqlCommand cmdLots = new SqlCommand(selectLots, conn, tran))
                            {
                                cmdLots.Parameters.AddWithValue("@BanhId", item.BanhId);
                                using (var readerLots = cmdLots.ExecuteReader())
                                {
                                    while (readerLots.Read())
                                        lots.Add((readerLots.GetInt32(0), readerLots.GetInt32(1), readerLots.GetString(2)));
                                }
                            }

                            // --- Trừ kho và ghi OrderDetail ---
                            // --- Trừ kho và ghi OrderDetail ---
                            foreach (var lot in lots)
                            {
                                if (remaining <= 0) break;

                                int toDeduct = Math.Min(remaining, lot.SoLuong);

                                // ❗ Bỏ qua lô có SoLuong = 0 hoặc không cần trừ
                                if (toDeduct <= 0) continue;

                                // Trừ kho
                                string updateLot = "UPDATE Inventory SET SoLuong = SoLuong - @Qty WHERE Id=@LotId";
                                using (SqlCommand cmdUpdateLot = new SqlCommand(updateLot, conn, tran))
                                {
                                    cmdUpdateLot.Parameters.AddWithValue("@Qty", toDeduct);
                                    cmdUpdateLot.Parameters.AddWithValue("@LotId", lot.LotId);
                                    cmdUpdateLot.ExecuteNonQuery();
                                }

                                // Lưu chi tiết đơn hàng theo lô hàng
                                string insertDetail = @"
        INSERT INTO OrderDetail (OrderId, BanhId, Quantity, BatchCode)
        VALUES (@OrderId, @BanhId, @Quantity, @BatchCode)";
                                using (SqlCommand cmdDetail = new SqlCommand(insertDetail, conn, tran))
                                {
                                    cmdDetail.Parameters.AddWithValue("@OrderId", orderId);
                                    cmdDetail.Parameters.AddWithValue("@BanhId", item.BanhId);
                                    cmdDetail.Parameters.AddWithValue("@Quantity", toDeduct);
                                    cmdDetail.Parameters.AddWithValue("@BatchCode", lot.BatchCode);
                                    cmdDetail.ExecuteNonQuery();
                                }

                                remaining -= toDeduct;
                            }

                            // --- Kiểm tra thiếu hàng ---
                            if (remaining > 0)
                            {
                                TempData["Message"] = $"⚠️ Sản phẩm (ID: {item.BanhId}) không đủ hàng trong kho!";
                            }

                            
                        }

                        // ✅ 6. Xóa giỏ hàng sau khi thanh toán
                        string deleteCart = "DELETE FROM Cart WHERE AccountId=@AccountId";
                        using (SqlCommand cmd = new SqlCommand(deleteCart, conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@AccountId", accountId.Value);
                            cmd.ExecuteNonQuery();
                        }

                        tran.Commit();
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        TempData["Message"] = "❌ Lỗi khi xác nhận đơn hàng: " + ex.Message;
                        return RedirectToAction("Cart", "Banh");
                    }
                }
            }

            TempData["Message"] = "✅ Đơn hàng đã được xác nhận và tồn kho đã được cập nhật theo FIFO!";
            return RedirectToAction("Index", "Banh");
        }

        
        // Chi tiết sản phẩm
        public IActionResult Details(int id)
        {
            Banh banh = null;

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                string query = @"
                    SELECT b.Id, b.TenBanh, b.MoTa, b.Gia, b.HinhAnh, b.CategoryId,
                           c.TenDanhMuc,
                           ct.MoTaChiTiet, ct.NguyenLieu, ct.HuongVi, ct.KichThuoc
                    FROM Banh b
                    LEFT JOIN Category c ON b.CategoryId = c.Id
                    LEFT JOIN BanhChiTiet ct ON b.Id = ct.BanhId
                    WHERE b.Id = @Id";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", id);

                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    banh = new Banh
                    {
                        Id = (int)reader["Id"],
                        TenBanh = reader["TenBanh"].ToString(),
                        MoTa = reader["MoTa"].ToString(),
                        Gia = (decimal)reader["Gia"],
                        HinhAnh = reader["HinhAnh"].ToString(),
                        CategoryId = reader["CategoryId"] as int?,
                        CategoryName = reader["TenDanhMuc"].ToString(),
                        MoTaChiTiet = reader["MoTaChiTiet"]?.ToString(),
                        NguyenLieu = reader["NguyenLieu"]?.ToString(),
                        HuongVi = reader["HuongVi"]?.ToString(),
                        KichThuoc = reader["KichThuoc"]?.ToString()
                    };
                }
            }

            if (banh == null)
            {
                return NotFound();
            }

            return View(banh);
        }

    }
}
