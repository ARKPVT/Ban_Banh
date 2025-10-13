namespace DemoApp.Models
{
    public class EmployeeStatisticViewModel
    {
        public string EmployeeName { get; set; }
        public int OrdersHandled { get; set; }    // Số đơn xử lý
        public int ProductsSold { get; set; }     // Sản phẩm bán được
        public decimal Revenue { get; set; }      // Doanh thu
        public decimal Profit { get; set; }       // Lợi nhuận
    }
}
