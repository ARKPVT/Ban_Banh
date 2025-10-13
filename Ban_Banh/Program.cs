var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Cấu hình session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();

// Middleware session: phải đặt trước UseAuthorization nếu bạn dùng Authorization
app.UseSession();

// Nếu có Authentication/Authorization, giữ thứ tự chuẩn
// app.UseAuthentication();
// app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Banh}/{action=Index}/{id?}");

app.Run();
