using DemoApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DemoApp.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class EmployeeStatsController : Controller
    {
        // Dữ liệu mẫu
        private static List<EmployeeStatisticViewModel> employees = new List<EmployeeStatisticViewModel>
        {
            new EmployeeStatisticViewModel { EmployeeName = "Nguyễn Văn A", OrdersHandled = 120, ProductsSold = 500, Revenue = 150_000_000, Profit = 40_000_000 },
            new EmployeeStatisticViewModel { EmployeeName = "Trần Thị B", OrdersHandled = 95,  ProductsSold = 420, Revenue = 120_000_000, Profit = 35_000_000 },
            new EmployeeStatisticViewModel { EmployeeName = "Phạm Văn C", OrdersHandled = 150, ProductsSold = 700, Revenue = 200_000_000, Profit = 55_000_000 },
            new EmployeeStatisticViewModel { EmployeeName = "Lê Thị D", OrdersHandled = 80,  ProductsSold = 350, Revenue = 90_000_000,  Profit = 25_000_000 },
            new EmployeeStatisticViewModel { EmployeeName = "Đỗ Văn E", OrdersHandled = 130, ProductsSold = 600, Revenue = 170_000_000, Profit = 50_000_000 }
        };

        public IActionResult Index(string period = "month")
        {
            // Dữ liệu mẫu - trong thực tế sẽ lọc theo thời gian
            ViewBag.SelectedPeriod = period;
            ViewBag.Title = "Thống kê theo nhân viên (" + GetPeriodLabel(period) + ")";
            return View(employees);
        }

        private string GetPeriodLabel(string period)
        {
            return period switch
            {
                "day" => "Hôm nay",
                "week" => "Tuần này",
                "month" => "Tháng này",
                "quarter" => "Quý này",
                "year" => "Năm nay",
                _ => "Toàn thời gian"
            };
        }
    }
}
