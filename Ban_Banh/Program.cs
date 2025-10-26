using Ban_Banh.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// =============================
// 🧩 CẤU HÌNH DỊCH VỤ
// =============================
builder.Configuration.AddUserSecrets<Program>(optional: true);

// 🧠 Cấu hình dịch vụ AI Chat
builder.Services.AddHttpClient();
builder.Services.AddDbContext<FeedbackDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton(sp =>
{
    var cfg = builder.Configuration;
    var opt = new Ban_Banh.Helpers.ChatProviderOptions
    {
        Provider = cfg["AI:Provider"] ?? Environment.GetEnvironmentVariable("PROVIDER") ?? "OPENAI",
        ApiKey = cfg["AI:ApiKey"] ?? Environment.GetEnvironmentVariable("AI_API_KEY") ?? "",
        ApiUrl = cfg["AI:ApiUrl"] ?? Environment.GetEnvironmentVariable("AI_API_URL") ?? "",
        Model = cfg["AI:Model"] ?? Environment.GetEnvironmentVariable("AI_MODEL") ?? "",
        Temperature = 0.7
    };
    return new Ban_Banh.Helpers.ChatProvider(
        sp.GetRequiredService<IHttpClientFactory>(),
        cfg
    );
});

builder.Services.Configure<FormOptions>(o =>
{
    // 20MB tổng — khớp với [RequestSizeLimit] ở controller
    o.MultipartBodyLengthLimit = 20 * 1024 * 1024;
});

builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<Ban_Banh.Helpers.ChatProvider>();

// =============================
// ⚙️ CẤU HÌNH SESSION + COOKIE
// =============================
builder.Services.AddDistributedMemoryCache(); // Dùng bộ nhớ để lưu session

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // thời gian hết hạn
    options.Cookie.HttpOnly = true;                 // bảo mật cookie
    options.Cookie.IsEssential = true;              // cookie cần thiết
    options.Cookie.Name = ".BanBanh.Session";       // tên cookie tùy chọn
});

// ✅ Thêm Authentication bằng Cookie
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";            // chuyển về trang Login nếu chưa đăng nhập
        options.AccessDeniedPath = "/Account/AccessDenied"; // trang lỗi khi bị chặn quyền
        options.ExpireTimeSpan = TimeSpan.FromHours(3);  // cookie hết hạn sau 3 tiếng
        options.SlidingExpiration = true;                // tự động gia hạn nếu người dùng hoạt động
    });

// ✅ Cấu hình Authorization (phù hợp với DB)
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("OrderOnly", policy => policy.RequireRole("Order"));
    options.AddPolicy("KhoOnly", policy => policy.RequireRole("Kho"));
    options.AddPolicy("BepOnly", policy => policy.RequireRole("Bep"));
    options.AddPolicy("ShipOnly", policy => policy.RequireRole("Ship"));
    options.AddPolicy("UserOnly", policy => policy.RequireRole("User"));

    // (Tuỳ chọn) Gom nhóm tất cả nhân viên (trừ khách hàng)
    options.AddPolicy("StaffOnly", policy => policy.RequireRole("Order", "Kho", "Bep", "Ship", "Admin"));
});

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.PropertyNameCaseInsensitive = true);

// =============================
// 🚀 BUILD APP
// =============================
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// 🧠 Dùng Session phải đặt TRƯỚC Authorization
app.UseSession();

// ✅ Bắt buộc có Authentication để dùng [Authorize] / SignInAsync()
app.UseAuthentication();

app.UseAuthorization();

// =============================
// 📍 ĐỊNH TUYẾN
// =============================
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Banh}/{action=Index}/{id?}");

app.MapControllers();

app.Run();
