using DemoApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DemoApp.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class DashboardController : Controller
    {
        public IActionResult Index(string period = "month")
        {
            ViewBag.SelectedPeriod = period;

            // Dữ liệu mẫu
            var model = new DashboardViewModel
            {
                TotalRevenue = 1_250_000_000,
                TotalExpense = 870_000_000,
                TotalOrders = 1285,
                TotalEmployees = 25,
                TotalProductsInStock = 1800,
                MonthlyReports = new List<MonthlyReport>
                {
                    new MonthlyReport { Month = "Tháng 1", Revenue = 150_000_000, Expense = 100_000_000 },
                    new MonthlyReport { Month = "Tháng 2", Revenue = 180_000_000, Expense = 120_000_000 },
                    new MonthlyReport { Month = "Tháng 3", Revenue = 200_000_000, Expense = 130_000_000 },
                    new MonthlyReport { Month = "Tháng 4", Revenue = 220_000_000, Expense = 150_000_000 },
                    new MonthlyReport { Month = "Tháng 5", Revenue = 250_000_000, Expense = 180_000_000 },
                    new MonthlyReport { Month = "Tháng 6", Revenue = 250_000_000, Expense = 190_000_000 },
                },
                ExpenseStructure = new Dictionary<string, decimal>
                {
                    { "Lương nhân viên", 400_000_000 },
                    { "Thuế", 120_000_000 },
                    { "Trang thiết bị", 80_000_000 },
                    { "Chi phí vận hành", 150_000_000 },
                    { "Khác", 120_000_000 }
                },
                Warnings = new List<string>
                {
                    "⚠️ Sản phẩm 'Sữa tươi Vinamilk' chỉ còn 15 hộp trong kho",
                    "⚠️ Nhân viên 'Nguyễn Văn A' nghỉ phép 3 ngày tới",
                    "⚠️ 5 sản phẩm trong danh mục 'Bánh kẹo' sắp hết hạn"
                }
            };

            return View(model);
        }
    }
}
