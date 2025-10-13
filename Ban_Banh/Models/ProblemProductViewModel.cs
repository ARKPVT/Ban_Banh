namespace DemoApp.Models
{
    public class ProblemProductViewModel
    {
        public int Id { get; set; }
        public string ProductName { get; set; }
        public string Category { get; set; }
        public string ProblemType { get; set; } // "Damaged", "Returned", "Recalled"
        public string Description { get; set; }
        public string Status { get; set; } // "Pending", "Processing", "Resolved"
        public DateTime ReportDate { get; set; }
    }
}
