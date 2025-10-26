using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using YourApp.Models;

namespace YourApp.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class EmployeeController : Controller
    {
        // In-memory demo store
        private static List<Employee> _employees = InitializeSampleData();

        // Index: list with filters & quick date ranges
        public IActionResult Index([FromQuery] EmployeeFilterViewModel filter)
        {
            var q = _employees.AsQueryable();

            // Apply quick range if requested
            ApplyQuickRange(filter);

            if (!string.IsNullOrEmpty(filter.Query))
            {
                var key = filter.Query.Trim().ToLower();
                q = q.Where(e => (e.FullName ?? "").ToLower().Contains(key)
                              || (e.Code ?? "").ToLower().Contains(key)
                              || (e.Email ?? "").ToLower().Contains(key));
            }

            if (!string.IsNullOrEmpty(filter.Department))
                q = q.Where(e => e.Department == filter.Department);

            if (!string.IsNullOrEmpty(filter.Position))
                q = q.Where(e => e.Position == filter.Position);

            if (filter.IsActive.HasValue)
                q = q.Where(e => e.IsActive == filter.IsActive.Value);

            if (filter.From.HasValue)
                q = q.Where(e => e.HireDate.Date >= filter.From.Value.Date);

            if (filter.To.HasValue)
                q = q.Where(e => e.HireDate.Date <= filter.To.Value.Date);

            // Sorting (by HireDate desc then name)
            q = q.OrderByDescending(e => e.HireDate).ThenBy(e => e.FullName);

            // Paging
            filter.TotalItems = q.Count();
            var skip = (Math.Max(filter.Page, 1) - 1) * filter.PageSize;
            filter.Items = q.Skip(skip).Take(filter.PageSize).ToList();

            return View(filter);
        }

        // Details
        public IActionResult Details(int id)
        {
            var emp = _employees.FirstOrDefault(e => e.Id == id);
            if (emp == null) return NotFound();
            return View(emp);
        }

        // Create - GET
        public IActionResult Create()
        {
            return View(new Employee { HireDate = DateTime.Today, IsActive = true });
        }

        // Create - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Employee model)
        {
            if (!ModelState.IsValid) return View(model);

            model.Id = _employees.Any() ? _employees.Max(x => x.Id) + 1 : 1;
            // Ensure unique Code if not provided
            if (string.IsNullOrWhiteSpace(model.Code))
                model.Code = $"NV{model.Id:000}";

            _employees.Add(model);
            TempData["Success"] = "Tạo nhân viên thành công.";
            return RedirectToAction(nameof(Index));
        }

        // Edit - GET
        public IActionResult Edit(int id)
        {
            var emp = _employees.FirstOrDefault(e => e.Id == id);
            if (emp == null) return NotFound();
            return View(emp);
        }

        // Edit - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Employee model)
        {
            if (!ModelState.IsValid) return View(model);

            var emp = _employees.FirstOrDefault(e => e.Id == model.Id);
            if (emp == null) return NotFound();

            // update fields
            emp.FullName = model.FullName;
            emp.Position = model.Position;
            emp.Department = model.Department;
            emp.Email = model.Email;
            emp.Phone = model.Phone;
            emp.HireDate = model.HireDate;
            emp.IsActive = model.IsActive;
            emp.BaseSalary = model.BaseSalary;
            emp.Note = model.Note;
            TempData["Success"] = "Cập nhật nhân viên thành công.";
            return RedirectToAction(nameof(Index));
        }

        // Delete - GET
        public IActionResult Delete(int id)
        {
            var emp = _employees.FirstOrDefault(e => e.Id == id);
            if (emp == null) return NotFound();
            return View(emp);
        }

        // Delete - POST
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var emp = _employees.FirstOrDefault(e => e.Id == id);
            if (emp != null)
            {
                _employees.Remove(emp);
                TempData["Success"] = "Xóa nhân viên thành công.";
            }
            return RedirectToAction(nameof(Index));
        }

        // --- Helpers ---
        private static void ApplyQuickRange(EmployeeFilterViewModel filter)
        {
            if (string.IsNullOrEmpty(filter?.QuickRange)) return;

            DateTime now = DateTime.Today;
            switch (filter.QuickRange.ToLower())
            {
                case "day":
                    filter.From = now;
                    filter.To = now;
                    break;
                case "week":
                    var diff = (int)now.DayOfWeek;
                    var start = now.AddDays(-diff); // start of week (Sunday)
                    filter.From = start;
                    filter.To = start.AddDays(6);
                    break;
                case "month":
                    filter.From = new DateTime(now.Year, now.Month, 1);
                    filter.To = filter.From.Value.AddMonths(1).AddDays(-1);
                    break;
                case "quarter":
                    int q = (now.Month - 1) / 3 + 1;
                    var qStart = new DateTime(now.Year, (q - 1) * 3 + 1, 1);
                    filter.From = qStart;
                    filter.To = qStart.AddMonths(3).AddDays(-1);
                    break;
                case "year":
                    filter.From = new DateTime(now.Year, 1, 1);
                    filter.To = new DateTime(now.Year, 12, 31);
                    break;
            }
        }

        private static List<Employee> InitializeSampleData()
        {
            var list = new List<Employee>();
            var rnd = new Random(42);
            var depts = new[] { "Bán hàng", "Kế toán", "Kho", "Sản xuất", "Hành chính" };
            var positions = new[] { "Nhân viên", "Trưởng phòng", "Giám sát", "Kế toán", "Nhân viên kho" };
            for (int i = 1; i <= 28; i++)
            {
                list.Add(new Employee
                {
                    Id = i,
                    Code = $"NV{i:000}",
                    FullName = $"Nguyễn Văn {i}",
                    Department = depts[rnd.Next(depts.Length)],
                    Position = positions[rnd.Next(positions.Length)],
                    Email = $"nv{i}@demo.local",
                    Phone = $"0900{100000 + i}",
                    HireDate = DateTime.Today.AddDays(-rnd.Next(0, 1200)),
                    IsActive = i % 7 != 0,
                    BaseSalary = 6000000 + rnd.Next(0, 8) * 500000,
                    Note = i % 5 == 0 ? "Cần lưu ý: hợp đồng thử việc" : ""
                });
            }
            return list;
        }
    }
}
