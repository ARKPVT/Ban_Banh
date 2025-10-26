namespace DemoApp.Models
{
    public class WarningProductViewModel
    {
        public int Id { get; set; }
        public string ProductName { get; set; }
        public string Category { get; set; }
        public int Stock { get; set; }
        public DateTime ExpiryDate { get; set; }
        public string WarningType { get; set; } // "OutOfStock", "LowStock", "NearExpiry", "Damaged"
        public bool IsHandled { get; set; } // Đã xử lý hay chưa
    }
}
