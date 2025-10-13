using System;

namespace YourApp.Models
{
    public class SalaryRecord
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; }   // cache cho demo (thực tế join bảng nhân viên)
        public string Department { get; set; }

        public DateTime Period { get; set; }       // kỳ lương (thường lấy ngày đầu tháng)
        public decimal BaseSalary { get; set; }
        public decimal Bonus { get; set; }
        public decimal Deduction { get; set; }
        public decimal Total => BaseSalary + Bonus - Deduction;
        public string Note { get; set; }
    }
}
