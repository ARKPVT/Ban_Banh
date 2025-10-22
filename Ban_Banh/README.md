# Feedback Feature (Upload ảnh + lưu DB)

Copy các file vào project ASP.NET Core MVC hiện tại (namespace `Ban_Banh`).

## Cấu trúc
- Models/Feedback.cs
- Data/FeedbackDbContext.cs
- Controllers/FeedbackController.cs
- Views/Shared/_FeedbackNavItem.cshtml
- Views/Shared/_FeedbackModal.cshtml
- wwwroot/js/feedback-modal.js
- wwwroot/css/feedback.css

## Program.cs
```csharp
using Ban_Banh.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http.Features;

// ...
builder.Services.AddDbContext<FeedbackDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.Configure<FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 20 * 1024 * 1024; // 20MB
});

builder.Services.AddControllers();
app.MapControllers(); // để kích hoạt /api/feedback
```

## appsettings.json
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=.;Database=BanBanh;Trusted_Connection=True;TrustServerCertificate=True"
}
```

## DB
- **EF Core**:
```
dotnet tool install --global dotnet-ef
dotnet ef migrations add Feedback_Init
dotnet ef database update
```
- **SQL thuần**:
```sql
CREATE TABLE Feedbacks (
  Id INT IDENTITY(1,1) PRIMARY KEY,
  Name NVARCHAR(150) NULL,
  Email NVARCHAR(150) NULL,
  Rating INT NOT NULL DEFAULT 5,
  Message NVARCHAR(MAX) NOT NULL,
  CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
CREATE TABLE FeedbackImages (
  Id INT IDENTITY(1,1) PRIMARY KEY,
  FeedbackId INT NOT NULL,
  FileName NVARCHAR(255) NOT NULL,
  ContentType NVARCHAR(255) NOT NULL,
  Size BIGINT NOT NULL,
  Url NVARCHAR(512) NOT NULL,
  CONSTRAINT FK_FeedbackImages_Feedbacks
    FOREIGN KEY (FeedbackId) REFERENCES Feedbacks(Id) ON DELETE CASCADE
);
CREATE INDEX IX_Feedbacks_CreatedAt ON Feedbacks(CreatedAt);
CREATE INDEX IX_FeedbackImages_FeedbackId ON FeedbackImages(FeedbackId);
```

## _Layout.cshtml
Trong `<head>`:
```cshtml
<link rel="stylesheet" href="~/css/feedback.css" asp-append-version="true" />
```
Trong navbar (ul.navbar-nav ...):
```cshtml
@await Html.PartialAsync("_FeedbackNavItem")
```
Trước `</body>`:
```cshtml
@await Html.PartialAsync("_FeedbackModal")
<script src="~/js/feedback-modal.js" asp-append-version="true"></script>
```
