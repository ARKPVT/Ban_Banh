# Tích hợp AI Chat vào Ban_Banh (ASP.NET Core MVC)

Đã thêm:
- API: `Controllers/ChatApiController.cs` (POST `/api/chat`)
- UI:  `Views/Chat/Index.cshtml` (trang chat)
- Provider: `Helpers/ChatProvider.cs` (hỗ trợ OPENAI & GOOGLE AI Studio)
- Cấu hình: `appsettings*.json` (mục `AI`), có fallback qua biến môi trường `.env` nếu bạn dùng

## Chạy dự án
1) Mở solution `Ban_Banh.sln` và build.
2) Đặt API key:
   - Cập nhật `appsettings.Development.json` phần `AI:ApiKey` **hoặc**
   - đặt biến môi trường `AI_API_KEY` (vẫn hỗ trợ `PROVIDER`, `AI_API_URL`, `AI_MODEL`).
3) Chạy ứng dụng (F5). Mở `/Chat` để test UI, hoặc gọi POST `/api/chat` để lấy JSON.

> Mặc định cấu hình sẵn **GOOGLE/Gemini**. Đổi sang OpenAI bằng:
- `AI:Provider: OPENAI`
- `AI:ApiUrl: https://api.openai.com/v1/chat/completions`
- `AI:Model: gpt-4o-mini` (hoặc model khác)
