namespace DemoApp.Models
{
    public class ProductStatisticViewModel
    {
        public string ProductName { get; set; }
        public int Imported { get; set; } // Số lượng nhập
        public int Sold { get; set; }     // Số lượng bán

        public int InStock => Imported - Sold; // Còn lại
    }
}
