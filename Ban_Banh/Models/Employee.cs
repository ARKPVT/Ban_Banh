using System;

namespace YourApp.Models
{
    public class Employee
    {
        public int Id { get; set; }                // primary key (in-memory)
        public string Code { get; set; }          // Mã nhân viên (NV001)
        public string FullName { get; set; }
        public string Position { get; set; }      // Chức vụ
        public string Department { get; set; }    // Bộ phận
        public string Email { get; set; }
        public string Phone { get; set; }
        public DateTime HireDate { get; set; }
        public bool IsActive { get; set; } = true;
        public decimal BaseSalary { get; set; }   // lương cơ bản (demo)
        public string Note { get; set; }
    }
}
