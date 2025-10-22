using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using Ban_Banh.Models;

namespace Ban_Banh.Controllers
{
    public class BanhController : Controller
    {
        private readonly string _connectionString;

        public BanhController(IConfiguration configuration)
        {
            _connectionString =
                configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Missing ConnectionStrings:DefaultConnection");
        }

        // ===============================
        // 1️⃣ Hiển thị danh sách bánh
        // ===============================
        public IActionResult Index(int? categoryId, decimal? minPrice, decimal? maxPrice)
        {
            var list = new List<Banh>();

            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            var sql = @"
SELECT 
    b.Id, b.TenBanh, b.MoTa, b.Gia, b.HinhAnh, 
    c.TenDanhMuc, 
    ISNULL(SUM(i.SoLuong), 0) AS TongSoLuong
FROM Banh b
LEFT JOIN Category  c ON b.CategoryId = c.Id
LEFT JOIN Inventory i ON b.Id       = i.BanhId
WHERE 1 = 1";

            if (categoryId != null) sql += " AND b.CategoryId = @CategoryId";
            if (minPrice != null) sql += " AND b.Gia >= @MinPrice";
            if (maxPrice != null) sql += " AND b.Gia <= @MaxPrice";

            sql += " GROUP BY b.Id, b.TenBanh, b.MoTa, b.Gia, b.HinhAnh, c.TenDanhMuc";

            using (var cmd = new SqlCommand(sql, conn))
            {
                if (categoryId != null) cmd.Parameters.AddWithValue("@CategoryId", categoryId.Value);
                if (minPrice != null) cmd.Parameters.AddWithValue("@MinPrice", minPrice.Value);
                if (maxPrice != null) cmd.Parameters.AddWithValue("@MaxPrice", maxPrice.Value);

                using var reader = cmd.ExecuteReader();
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
                        SoLuongTon = reader.IsDBNull(6) ? 0 : reader.GetInt32(6)
                    });
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
            var categories = new List<Category>();

            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            const string sql = "SELECT Id, TenDanhMuc FROM Category";
            using var cmd = new SqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                categories.Add(new Category
                {
                    Id = reader.GetInt32(0),
                    TenDanhMuc = reader.GetString(1)
                });
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
                using var conn = new SqlConnection(_connectionString);
                conn.Open();

                // Kiểm tra tồn kho
                const string checkStockSql = "SELECT ISNULL(SUM(SoLuong),0) FROM Inventory WHERE BanhId=@BanhId";
                using (var checkStockCmd = new SqlCommand(checkStockSql, conn))
                {
                    checkStockCmd.Parameters.AddWithValue("@BanhId", id);
                    int soLuongTon = Convert.ToInt32(checkStockCmd.ExecuteScalar() ?? 0);
                    if (soLuongTon <= 0)
                    {
                        TempData["Message"] = "❌ Sản phẩm này đã hết hàng!";
                        return RedirectToAction("Index");
                    }
                }

                // Đã có trong giỏ?
                const string checkSql = "SELECT Quantity FROM Cart WHERE AccountId=@AccountId AND BanhId=@BanhId";
                using (var checkCmd = new SqlCommand(checkSql, conn))
                {
                    checkCmd.Parameters.AddWithValue("@AccountId", accountId.Value);
                    checkCmd.Parameters.AddWithValue("@BanhId", id);
                    var exist = checkCmd.ExecuteScalar();

                    if (exist != null)
                    {
                        const string updateSql =
                            "UPDATE Cart SET Quantity = Quantity + 1 WHERE AccountId=@AccountId AND BanhId=@BanhId";
                        using var updateCmd = new SqlCommand(updateSql, conn);
                        updateCmd.Parameters.AddWithValue("@AccountId", accountId.Value);
                        updateCmd.Parameters.AddWithValue("@BanhId", id);
                        updateCmd.ExecuteNonQuery();
                        TempData["Message"] = "✅ Sản phẩm đã được cập nhật số lượng trong giỏ hàng!";
                    }
                    else
                    {
                        const string insertSql =
                            "INSERT INTO Cart (AccountId, BanhId, Quantity) VALUES (@AccountId, @BanhId, 1)";
                        using var insertCmd = new SqlCommand(insertSql, conn);
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

            var cartItems = new List<Cart>();
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            const string sql = @"
SELECT c.Id, c.AccountId, c.BanhId, c.Quantity, c.CreatedAt,
       b.TenBanh, b.MoTa, b.Gia, b.HinhAnh
FROM Cart c
INNER JOIN Banh b ON c.BanhId = b.Id
WHERE c.AccountId = @AccountId";

            using var cmd = new SqlCommand(sql, conn);
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

            ViewData["CartQuantity"] = GetCartQuantity();
            return View(cartItems);
        }

        // ===============================
        // 5️⃣ Cập nhật giỏ hàng (AJAX)
        // ===============================
        [HttpPost]
        public IActionResult UpdateCartAjax(int cartId, int quantity)
        {
            if (quantity <= 0) return BadRequest("⚠️ Số lượng phải là số nguyên dương!");

            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            // lấy BanhId
            const string getBanhSql = "SELECT BanhId FROM Cart WHERE Id=@CartId";
            using var getBanhCmd = new SqlCommand(getBanhSql, conn);
            getBanhCmd.Parameters.AddWithValue("@CartId", cartId);
            var banhObj = getBanhCmd.ExecuteScalar();
            if (banhObj == null) return NotFound("❌ Không tìm thấy sản phẩm trong giỏ hàng.");
            int banhId = Convert.ToInt32(banhObj);

            // check tồn
            const string checkStockSql = "SELECT ISNULL(SUM(SoLuong),0) FROM Inventory WHERE BanhId=@BanhId";
            using var checkStockCmd = new SqlCommand(checkStockSql, conn);
            checkStockCmd.Parameters.AddWithValue("@BanhId", banhId);
            int soLuongTon = Convert.ToInt32(checkStockCmd.ExecuteScalar() ?? 0);

            if (soLuongTon <= 0) return BadRequest("❌ Sản phẩm đã hết hàng!");

            if (quantity > soLuongTon)
            {
                const string fixSql = "UPDATE Cart SET Quantity=@Quantity WHERE Id=@CartId";
                using var fixCmd = new SqlCommand(fixSql, conn);
                fixCmd.Parameters.AddWithValue("@Quantity", soLuongTon);
                fixCmd.Parameters.AddWithValue("@CartId", cartId);
                fixCmd.ExecuteNonQuery();
                TempData["Message"] = $"⚠️ Chỉ còn {soLuongTon} sản phẩm trong kho!";
                return BadRequest(TempData.Peek("Message"));
            }

            const string upd = "UPDATE Cart SET Quantity=@Quantity WHERE Id=@CartId";
            using var cmd = new SqlCommand(upd, conn);
            cmd.Parameters.AddWithValue("@Quantity", quantity);
            cmd.Parameters.AddWithValue("@CartId", cartId);
            cmd.ExecuteNonQuery();

            return Ok();
        }

        public IActionResult RemoveFromCart(int cartId)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            const string sql = "DELETE FROM Cart WHERE Id = @CartId";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@CartId", cartId);
            cmd.ExecuteNonQuery();
            return RedirectToAction("Cart");
        }

        public IActionResult IncreaseQuantity(int cartId)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            // lấy BanhId + Quantity hiện tại
            const string getCartSql = "SELECT BanhId, Quantity FROM Cart WHERE Id=@CartId";
            using var getCartCmd = new SqlCommand(getCartSql, conn);
            getCartCmd.Parameters.AddWithValue("@CartId", cartId);

            int banhId = 0, quantity = 0;
            using (var reader = getCartCmd.ExecuteReader())
            {
                if (reader.Read()) { banhId = reader.GetInt32(0); quantity = reader.GetInt32(1); }
            }

            // tồn kho
            const string stockSql = "SELECT ISNULL(SUM(SoLuong),0) FROM Inventory WHERE BanhId=@BanhId";
            using var stockCmd = new SqlCommand(stockSql, conn);
            stockCmd.Parameters.AddWithValue("@BanhId", banhId);
            int soLuongTon = Convert.ToInt32(stockCmd.ExecuteScalar() ?? 0);

            if (quantity >= soLuongTon)
                TempData["Message"] = "⚠️ Số lượng bạn chọn đã đạt giới hạn tồn kho!";
            else
            {
                const string upd = "UPDATE Cart SET Quantity = Quantity + 1 WHERE Id=@CartId";
                using var cmd = new SqlCommand(upd, conn);
                cmd.Parameters.AddWithValue("@CartId", cartId);
                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("Cart");
        }

        public IActionResult DecreaseQuantity(int cartId)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            const string getSql = "SELECT Quantity FROM Cart WHERE Id=@CartId";
            using var getCmd = new SqlCommand(getSql, conn);
            getCmd.Parameters.AddWithValue("@CartId", cartId);
            int quantity = Convert.ToInt32(getCmd.ExecuteScalar());

            if (quantity > 1)
            {
                const string upd = "UPDATE Cart SET Quantity = Quantity - 1 WHERE Id=@CartId";
                using var cmd = new SqlCommand(upd, conn);
                cmd.Parameters.AddWithValue("@CartId", cartId);
                cmd.ExecuteNonQuery();
            }
            else
            {
                const string del = "DELETE FROM Cart WHERE Id=@CartId";
                using var cmd = new SqlCommand(del, conn);
                cmd.Parameters.AddWithValue("@CartId", cartId);
                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("Cart");
        }

        private int GetCartQuantity()
        {
            int? accountId = HttpContext.Session.GetInt32("AccountId");
            if (accountId == null) return 0;

            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            const string sql = "SELECT SUM(Quantity) FROM Cart WHERE AccountId=@AccountId";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@AccountId", accountId.Value);
            var result = cmd.ExecuteScalar();
            return (result == null || result == DBNull.Value) ? 0 : Convert.ToInt32(result);
        }

        // ===============================
        // 6️⃣ Checkout (giữ logic của bạn, chỉ gọn using/cmd)
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

            var cartItems = new List<Cart>();
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            const string sql = @"
SELECT c.Id, c.AccountId, c.BanhId, c.Quantity, b.TenBanh, b.Gia
FROM Cart c INNER JOIN Banh b ON c.BanhId = b.Id
WHERE c.AccountId = @AccountId";

            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@AccountId", accountId.Value);
                using var reader = cmd.ExecuteReader();
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

            ViewData["AccountInfo"] = accountInfo;
            return View(cartItems);
        }

        [HttpPost]
        public IActionResult ConfirmCheckout(string FullName, string Email, string Phone, string Address)
        {
            int? accountId = HttpContext.Session.GetInt32("AccountId");
            if (accountId == null) return RedirectToAction("Login", "Account");

            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var tran = conn.BeginTransaction();
            try
            {
                // 1. Cập nhật Account
                const string updAcc = @"
UPDATE Account SET FullName=@FullName, Email=@Email, Phone=@Phone, Address=@Address
WHERE Id=@AccountId";
                using (var cmd = new SqlCommand(updAcc, conn, tran))
                {
                    cmd.Parameters.AddWithValue("@FullName", (object?)FullName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Email", (object?)Email ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Phone", (object?)Phone ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Address", (object?)Address ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@AccountId", accountId.Value);
                    cmd.ExecuteNonQuery();
                }

                HttpContext.Session.SetString("FullName", FullName ?? "");
                HttpContext.Session.SetString("UserEmail", Email ?? "");
                HttpContext.Session.SetString("Phone", Phone ?? "");
                HttpContext.Session.SetString("Address", Address ?? "");

                // 2. Lấy giỏ hàng
                var cartItems = new List<(int BanhId, int Quantity)>();
                const string selCart = "SELECT BanhId, Quantity FROM Cart WHERE AccountId=@AccountId";
                using (var cmd = new SqlCommand(selCart, conn, tran))
                {
                    cmd.Parameters.AddWithValue("@AccountId", accountId.Value);
                    using var r = cmd.ExecuteReader();
                    while (r.Read()) cartItems.Add((r.GetInt32(0), r.GetInt32(1)));
                }
                if (cartItems.Count == 0)
                {
                    TempData["Message"] = "🛒 Giỏ hàng trống, không thể thanh toán!";
                    tran.Rollback();
                    return RedirectToAction("Cart", "Banh");
                }

                // 3. Tạo Order
                int orderId;
                const string insOrder = @"
INSERT INTO [Order] (AccountId, CreatedAt) VALUES (@AccountId, @CreatedAt);
SELECT SCOPE_IDENTITY();";
                using (var cmd = new SqlCommand(insOrder, conn, tran))
                {
                    cmd.Parameters.AddWithValue("@AccountId", accountId.Value);
                    cmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
                    orderId = Convert.ToInt32(cmd.ExecuteScalar());
                }

                // 4. FIFO trừ kho + OrderDetail
                foreach (var item in cartItems)
                {
                    int remaining = item.Quantity;

                    var lots = new List<(int LotId, int SoLuong, string BatchCode)>();
                    const string selLots = @"
SELECT Id, SoLuong, BatchCode
FROM Inventory
WHERE BanhId=@BanhId
ORDER BY HanSuDung ASC, Id ASC";
                    using (var cmdLots = new SqlCommand(selLots, conn, tran))
                    {
                        cmdLots.Parameters.AddWithValue("@BanhId", item.BanhId);
                        using var rLots = cmdLots.ExecuteReader();
                        while (rLots.Read())
                            lots.Add((rLots.GetInt32(0), rLots.GetInt32(1), rLots.GetString(2)));
                    }

                    foreach (var lot in lots)
                    {
                        if (remaining <= 0) break;
                        var toDeduct = Math.Min(remaining, lot.SoLuong);
                        if (toDeduct <= 0) continue;

                        const string updLot = "UPDATE Inventory SET SoLuong = SoLuong - @Qty WHERE Id=@LotId";
                        using (var cmd = new SqlCommand(updLot, conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@Qty", toDeduct);
                            cmd.Parameters.AddWithValue("@LotId", lot.LotId);
                            cmd.ExecuteNonQuery();
                        }

                        const string insDetail = @"
INSERT INTO OrderDetail (OrderId, BanhId, Quantity, BatchCode)
VALUES (@OrderId, @BanhId, @Quantity, @BatchCode)";
                        using (var cmd = new SqlCommand(insDetail, conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@OrderId", orderId);
                            cmd.Parameters.AddWithValue("@BanhId", item.BanhId);
                            cmd.Parameters.AddWithValue("@Quantity", toDeduct);
                            cmd.Parameters.AddWithValue("@BatchCode", lot.BatchCode);
                            cmd.ExecuteNonQuery();
                        }

                        remaining -= toDeduct;
                    }

                    if (remaining > 0)
                        TempData["Message"] = $"⚠️ Sản phẩm (ID: {item.BanhId}) không đủ hàng trong kho!";
                }

                // 5. Xóa giỏ hàng
                const string delCart = "DELETE FROM Cart WHERE AccountId=@AccountId";
                using (var cmd = new SqlCommand(delCart, conn, tran))
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

            TempData["Message"] = "✅ Đơn hàng đã được xác nhận và tồn kho đã được cập nhật theo FIFO!";
            return RedirectToAction("Index", "Banh");
        }

        // ===============================
        // Chi tiết sản phẩm
        // ===============================
        public IActionResult Details(int id)
        {
            Banh banh = null;

            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            const string query = @"
SELECT b.Id, b.TenBanh, b.MoTa, b.Gia, b.HinhAnh, b.CategoryId,
       c.TenDanhMuc,
       ct.MoTaChiTiet, ct.NguyenLieu, ct.HuongVi, ct.KichThuoc
FROM Banh b
LEFT JOIN Category    c  ON b.CategoryId = c.Id
LEFT JOIN BanhChiTiet ct ON b.Id        = ct.BanhId
WHERE b.Id = @Id";

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Id", id);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                banh = new Banh
                {
                    Id = reader.GetInt32(0),
                    TenBanh = reader.GetString(1),
                    MoTa = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    Gia = reader.GetDecimal(3),
                    HinhAnh = reader.IsDBNull(4) ? "noimage.jpg" : reader.GetString(4),
                    CategoryId = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                    CategoryName = reader.IsDBNull(6) ? "" : reader.GetString(6),
                    MoTaChiTiet = reader.IsDBNull(7) ? null : reader.GetString(7),
                    NguyenLieu = reader.IsDBNull(8) ? null : reader.GetString(8),
                    HuongVi = reader.IsDBNull(9) ? null : reader.GetString(9),
                    KichThuoc = reader.IsDBNull(10) ? null : reader.GetString(10)
                };
            }

            if (banh == null) return NotFound();
            return View(banh);
        }
    }
}
