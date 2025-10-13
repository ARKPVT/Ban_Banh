using System;
using System.Collections.Generic;

namespace YourApp.Models
{
    public class EmployeeFilterViewModel
    {
        public List<Employee> Items { get; set; } = new List<Employee>();

        // Filters
        public string Query { get; set; }           // tìm theo tên / mã
        public string Department { get; set; }      // lọc bộ phận
        public string Position { get; set; }        // lọc chức vụ
        public bool? IsActive { get; set; }

        // Date range filter
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }

        // Quick range selection: "day","week","month","quarter","year"
        public string QuickRange { get; set; }

        // Simple paging (demo)
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 15;
        public int TotalItems { get; set; }
    }
}
