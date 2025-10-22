using Ban_Banh.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http.Features;
var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddUserSecrets<Program>(optional: true);
// >>> AI Chat wiring (add after builder is created)
builder.Services.AddHttpClient();
builder.Services.AddDbContext<FeedbackDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddSingleton(sp => {
    var cfg = builder.Configuration;
    var opt = new Ban_Banh.Helpers.ChatProviderOptions {
        Provider = cfg["AI:Provider"] ?? Environment.GetEnvironmentVariable("PROVIDER") ?? "OPENAI",
        ApiKey  = cfg["AI:ApiKey"]  ?? Environment.GetEnvironmentVariable("AI_API_KEY") ?? "",
        ApiUrl  = cfg["AI:ApiUrl"]  ?? Environment.GetEnvironmentVariable("AI_API_URL") ?? "",
        Model   = cfg["AI:Model"]   ?? Environment.GetEnvironmentVariable("AI_MODEL") ?? "",
        Temperature = 0.7
    };
    return new Ban_Banh.Helpers.ChatProvider(
        sp.GetRequiredService<IHttpClientFactory>(), // FIX: use IHttpClientFactory
        cfg // FIX: pass IConfiguration
    );
});
builder.Services.Configure<FormOptions>(o =>
{
    // 20MB tổng — khớp với [RequestSizeLimit] ở controller
    o.MultipartBodyLengthLimit = 20 * 1024 * 1024;
});
// <<< AI Chat wiring


builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<Ban_Banh.Helpers.ChatProvider>();

// Cấu hình session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.PropertyNameCaseInsensitive = true);

var app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

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

app.MapControllers();

app.Run();
