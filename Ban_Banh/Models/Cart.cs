namespace Ban_Banh.Models
{
    public class Cart
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public int BanhId { get; set; }
        public int Quantity { get; set; }
        public DateTime CreatedAt { get; set; }

        // Liên kết với bảng Banh (hiển thị thông tin bánh)
        public Banh Banh { get; set; }
    }
}
