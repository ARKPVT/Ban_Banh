using System;
using System.Collections.Generic;

namespace YourApp.Models
{
    public class SalaryFilterViewModel
    {
        public List<SalaryRecord> Items { get; set; } = new List<SalaryRecord>();

        public int? Year { get; set; }
        public int? Month { get; set; }
        public string Department { get; set; }

        public decimal TotalPaid { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalItems { get; set; }
    }
}
