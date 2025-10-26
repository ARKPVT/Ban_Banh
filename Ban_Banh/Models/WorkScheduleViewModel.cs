namespace DemoApp.Models
{
    public class WorkScheduleViewModel
    {
        public int Id { get; set; }
        public string EmployeeName { get; set; }
        public DateTime WorkDate { get; set; }
        public string Shift { get; set; } // Ca sáng, chiều, tối
        public string Status { get; set; } // Đã làm / Nghỉ / Trễ / Dự kiến
        public string Notes { get; set; }
    }
}
