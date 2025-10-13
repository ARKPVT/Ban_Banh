namespace DemoApp.Models
{
    public class DashboardViewModel
    {
        public decimal TotalRevenue { get; set; }
        public decimal TotalExpense { get; set; }
        public decimal Profit => TotalRevenue - TotalExpense;

        public int TotalOrders { get; set; }
        public int TotalEmployees { get; set; }
        public int TotalProductsInStock { get; set; }

        public List<MonthlyReport> MonthlyReports { get; set; } = new();
        public Dictionary<string, decimal> ExpenseStructure { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }

    public class MonthlyReport
    {
        public string Month { get; set; }
        public decimal Revenue { get; set; }
        public decimal Expense { get; set; }
    }
}
