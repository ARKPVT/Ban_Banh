using DemoApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DemoApp.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class ProductStatsController : Controller
    {
        // Dữ liệu mẫu (demo, chưa cần database)
        private static List<ProductStatisticViewModel> allProducts = new List<ProductStatisticViewModel>
        {
            new ProductStatisticViewModel { ProductName = "Sữa tươi Vinamilk 1L", Imported = 200, Sold = 150 },
            new ProductStatisticViewModel { ProductName = "Bánh Oreo 135g", Imported = 300, Sold = 260 },
            new ProductStatisticViewModel { ProductName = "Mì Hảo Hảo", Imported = 1000, Sold = 800 },
            new ProductStatisticViewModel { ProductName = "Coca-Cola lon 330ml", Imported = 500, Sold = 450 },
            new ProductStatisticViewModel { ProductName = "Nước suối Lavie 500ml", Imported = 400, Sold = 370 },
            new ProductStatisticViewModel { ProductName = "Snack Poca vị bò nướng", Imported = 250, Sold = 120 },
            new ProductStatisticViewModel { ProductName = "Trà xanh 0 độ", Imported = 600, Sold = 590 }
        };

        public IActionResult Index(string period = "month")
        {
            // (Dữ liệu demo) Ở thực tế, period sẽ được dùng để lọc dữ liệu theo thời gian
            ViewBag.SelectedPeriod = period;
            ViewBag.Title = "Thống kê sản phẩm (" + GetPeriodLabel(period) + ")";
            return View(allProducts);
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
