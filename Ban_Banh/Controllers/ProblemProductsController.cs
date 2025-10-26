using DemoApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DemoApp.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class ProblemProductsController : Controller
    {
        private static List<ProblemProductViewModel> _problemProducts = new List<ProblemProductViewModel>
        {
            new ProblemProductViewModel { Id = 1, ProductName = "Sữa chua TH True", Category = "Sữa", ProblemType = "Damaged", Description = "Bị rách bao bì", Status = "Pending", ReportDate = DateTime.Today.AddDays(-2) },
            new ProblemProductViewModel { Id = 2, ProductName = "Snack Oishi tôm cay", Category = "Bánh kẹo", ProblemType = "Returned", Description = "Khách trả do lỗi gói", Status = "Processing", ReportDate = DateTime.Today.AddDays(-5) },
            new ProblemProductViewModel { Id = 3, ProductName = "Bánh mì tươi Kinh Đô", Category = "Bánh", ProblemType = "Recalled", Description = "Thu hồi theo thông báo nhà sản xuất", Status = "Resolved", ReportDate = DateTime.Today.AddDays(-10) }
        };

        public IActionResult Index(string filterType = "all", string filterStatus = "all")
        {
            ViewBag.FilterType = filterType;
            ViewBag.FilterStatus = filterStatus;

            var filtered = _problemProducts.AsEnumerable();

            if (filterType != "all")
                filtered = filtered.Where(p => p.ProblemType == filterType);

            if (filterStatus != "all")
                filtered = filtered.Where(p => p.Status == filterStatus);

            return View(filtered.ToList());
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(ProblemProductViewModel model)
        {
            if (ModelState.IsValid)
            {
                model.Id = _problemProducts.Max(p => p.Id) + 1;
                model.ReportDate = DateTime.Now;
                model.Status = "Pending";
                _problemProducts.Add(model);
                return RedirectToAction("Index");
            }
            return View(model);
        }

        [HttpPost]
        public IActionResult UpdateStatus(int id, string newStatus)
        {
            var product = _problemProducts.FirstOrDefault(p => p.Id == id);
            if (product != null)
            {
                product.Status = newStatus;
            }
            return RedirectToAction("Index");
        }
    }
}
