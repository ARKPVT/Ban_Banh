using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using YourApp.Models;

namespace YourApp.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class SalaryController : Controller
    {
        private static List<SalaryRecord> _salaries = InitializeSampleData();

        public IActionResult Index([FromQuery] SalaryFilterViewModel filter)
        {
            var q = _salaries.AsQueryable();

            if (filter.Year.HasValue)
                q = q.Where(x => x.Period.Year == filter.Year);

            if (filter.Month.HasValue)
                q = q.Where(x => x.Period.Month == filter.Month);

            if (!string.IsNullOrEmpty(filter.Department))
                q = q.Where(x => x.Department == filter.Department);

            q = q.OrderByDescending(x => x.Period).ThenBy(x => x.EmployeeName);

            filter.TotalItems = q.Count();
            var skip = (filter.Page - 1) * filter.PageSize;
            filter.Items = q.Skip(skip).Take(filter.PageSize).ToList();

            filter.TotalPaid = filter.Items.Sum(x => x.Total);

            return View(filter);
        }

        public IActionResult Details(int id)
        {
            var s = _salaries.FirstOrDefault(x => x.Id == id);
            if (s == null) return NotFound();
            return View(s);
        }

        public IActionResult Create()
        {
            return View(new SalaryRecord
            {
                Period = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(SalaryRecord model)
        {
            if (!ModelState.IsValid) return View(model);

            model.Id = _salaries.Any() ? _salaries.Max(x => x.Id) + 1 : 1;
            _salaries.Add(model);
            TempData["Success"] = "Đã thêm bảng lương.";
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Edit(int id)
        {
            var s = _salaries.FirstOrDefault(x => x.Id == id);
            if (s == null) return NotFound();
            return View(s);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(SalaryRecord model)
        {
            if (!ModelState.IsValid) return View(model);
            var s = _salaries.FirstOrDefault(x => x.Id == model.Id);
            if (s == null) return NotFound();

            s.EmployeeName = model.EmployeeName;
            s.Department = model.Department;
            s.Period = model.Period;
            s.BaseSalary = model.BaseSalary;
            s.Bonus = model.Bonus;
            s.Deduction = model.Deduction;
            s.Note = model.Note;

            TempData["Success"] = "Cập nhật bảng lương thành công.";
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Delete(int id)
        {
            var s = _salaries.FirstOrDefault(x => x.Id == id);
            if (s == null) return NotFound();
            return View(s);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var s = _salaries.FirstOrDefault(x => x.Id == id);
            if (s != null)
            {
                _salaries.Remove(s);
                TempData["Success"] = "Đã xóa bản ghi lương.";
            }
            return RedirectToAction(nameof(Index));
        }

        private static List<SalaryRecord> InitializeSampleData()
        {
            var list = new List<SalaryRecord>();
            var rnd = new Random(1);
            var names = new[] { "Nguyễn Văn A", "Trần Thị B", "Lê Văn C", "Phạm Thị D", "Hoàng Văn E" };
            var depts = new[] { "Kế toán", "Kho", "Bán hàng", "Sản xuất" };

            for (int i = 0; i < 30; i++)
            {
                var monthOffset = rnd.Next(0, 12);
                list.Add(new SalaryRecord
                {
                    Id = i + 1,
                    EmployeeId = i + 1,
                    EmployeeName = names[rnd.Next(names.Length)],
                    Department = depts[rnd.Next(depts.Length)],
                    Period = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-monthOffset),
                    BaseSalary = 7000000 + rnd.Next(0, 5) * 500000,
                    Bonus = rnd.Next(0, 3) * 1000000,
                    Deduction = rnd.Next(0, 2) * 500000,
                    Note = (i % 6 == 0) ? "Tăng ca nhiều" : ""
                });
            }
            return list;
        }
    }
}
