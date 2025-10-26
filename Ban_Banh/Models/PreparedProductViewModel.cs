namespace Ban_Banh.Models
{
    public class PreparedProductViewModel
    {
        public int OrderDetailId { get; set; }
        public int OrderId { get; set; }
        public string BanhName { get; set; }
        public int Quantity { get; set; }
        public string InventoryName { get; set; }
        public string StatusName { get; set; }
        public int PreparedCount { get; set; } // số lượng đã nhập
    }
}
