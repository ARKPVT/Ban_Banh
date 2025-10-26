using DemoApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DemoApp.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class ScheduleController : Controller
    {
        private static List<WorkScheduleViewModel> schedules = new List<WorkScheduleViewModel>
        {
            new WorkScheduleViewModel { Id = 1, EmployeeName = "Nguyễn Văn A", WorkDate = new DateTime(2025,10,10), Shift="Ca sáng", Status="Đã làm", Notes="Đúng giờ" },
            new WorkScheduleViewModel { Id = 2, EmployeeName = "Trần Thị B", WorkDate = new DateTime(2025,10,10), Shift="Ca chiều", Status="Nghỉ", Notes="Báo nghỉ trước" },
            new WorkScheduleViewModel { Id = 3, EmployeeName = "Phạm Văn C", WorkDate = new DateTime(2025,10,11), Shift="Ca tối", Status="Dự kiến", Notes="Chờ xác nhận" }
        };

        public IActionResult Index()
        {
            return View(schedules.OrderBy(s => s.WorkDate).ToList());
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(WorkScheduleViewModel model)
        {
            model.Id = schedules.Max(s => s.Id) + 1;
            schedules.Add(model);
            return RedirectToAction("Index");
        }

        public IActionResult Edit(int id)
        {
            var schedule = schedules.FirstOrDefault(s => s.Id == id);
            return View(schedule);
        }

        [HttpPost]
        public IActionResult Edit(WorkScheduleViewModel model)
        {
            var existing = schedules.FirstOrDefault(s => s.Id == model.Id);
            if (existing != null)
            {
                existing.EmployeeName = model.EmployeeName;
                existing.WorkDate = model.WorkDate;
                existing.Shift = model.Shift;
                existing.Status = model.Status;
                existing.Notes = model.Notes;
            }
            return RedirectToAction("Index");
        }

        public IActionResult Delete(int id)
        {
            var schedule = schedules.FirstOrDefault(s => s.Id == id);
            if (schedule != null)
                schedules.Remove(schedule);
            return RedirectToAction("Index");
        }
    }
}
