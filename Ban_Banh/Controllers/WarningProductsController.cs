using DemoApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DemoApp.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class WarningProductsController : Controller
    {
        // Dữ liệu mẫu
        private static List<WarningProductViewModel> _warnings = new List<WarningProductViewModel>
        {
            new WarningProductViewModel { Id = 1, ProductName = "Sữa tươi Vinamilk 1L", Category = "Đồ uống", Stock = 15, ExpiryDate = DateTime.Today.AddDays(5), WarningType = "NearExpiry", IsHandled = false },
            new WarningProductViewModel { Id = 2, ProductName = "Bánh quy Kinh Đô", Category = "Bánh kẹo", Stock = 0, ExpiryDate = DateTime.Today.AddMonths(6), WarningType = "OutOfStock", IsHandled = false },
            new WarningProductViewModel { Id = 3, ProductName = "Nước ngọt Pepsi lon", Category = "Đồ uống", Stock = 8, ExpiryDate = DateTime.Today.AddMonths(2), WarningType = "LowStock", IsHandled = false },
            new WarningProductViewModel { Id = 4, ProductName = "Sữa chua TH True Yogurt", Category = "Sữa", Stock = 20, ExpiryDate = DateTime.Today.AddDays(3), WarningType = "NearExpiry", IsHandled = false },
            new WarningProductViewModel { Id = 5, ProductName = "Snack Oishi vị tôm", Category = "Bánh kẹo", Stock = 10, ExpiryDate = DateTime.Today.AddMonths(1), WarningType = "Damaged", IsHandled = true }
        };

        public IActionResult Index(string filter = "all")
        {
            ViewBag.SelectedFilter = filter;

            var filtered = filter switch
            {
                "LowStock" => _warnings.Where(x => x.WarningType == "LowStock"),
                "OutOfStock" => _warnings.Where(x => x.WarningType == "OutOfStock"),
                "NearExpiry" => _warnings.Where(x => x.WarningType == "NearExpiry"),
                "Damaged" => _warnings.Where(x => x.WarningType == "Damaged"),
                _ => _warnings
            };

            return View(filtered.ToList());
        }

        [HttpPost]
        public IActionResult MarkHandled(int id)
        {
            var product = _warnings.FirstOrDefault(p => p.Id == id);
            if (product != null)
            {
                product.IsHandled = true;
            }
            return RedirectToAction("Index");
        }
    }
}
