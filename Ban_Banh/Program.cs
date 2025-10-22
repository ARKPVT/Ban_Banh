using Ban_Banh.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);
var conn = builder.Configuration.GetConnectionString("DefaultConnection")
         ?? builder.Configuration["DefaultConnection"];
builder.Configuration.AddUserSecrets<Program>(optional: true);
if (string.IsNullOrWhiteSpace(conn))
    throw new Exception("Missing connection string 'DefaultConnection'");

// EF
builder.Services.AddDbContext<FeedbackDbContext>(opt =>
    opt.UseSqlServer(conn));
builder.Services.Configure<FormOptions>(o =>
    o.MultipartBodyLengthLimit = 20 * 1024 * 1024);


// HTTP client cho ChatProvider
builder.Services.AddHttpClient();
// DI cho ChatProvider – chỉ 1 lần là đủ
builder.Services.AddSingleton(sp =>
    new Ban_Banh.Helpers.ChatProvider(
        sp.GetRequiredService<IHttpClientFactory>(),
        builder.Configuration
    )
);
builder.Services.AddControllers().AddJsonOptions(o =>
    o.JsonSerializerOptions.PropertyNameCaseInsensitive = true);

// nhận multipart tới 20MB
builder.Services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = 20 * 1024 * 1024);

builder.Services.AddControllersWithViews();

// Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(o =>
{
    o.IdleTimeout = TimeSpan.FromMinutes(30);
    o.Cookie.HttpOnly = true;
    o.Cookie.IsEssential = true;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();     // chỉ 1 lần
app.UseRouting();
app.UseSession();         // trước UseAuthorization nếu có
// app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Banh}/{action=Index}/{id?}");

app.MapControllers();     // để API chạy

app.Run();
