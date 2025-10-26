namespace Ban_Banh.Models
{
    public class Banh
    {
        public int Id { get; set; }
        public string TenBanh { get; set; }
        public string MoTa { get; set; }
        public decimal Gia { get; set; }
        public string HinhAnh { get; set; }

        // Bổ sung
        public int? CategoryId { get; set; }
        public string CategoryName { get; set; }
        public int SoLuongTon { get; set; }

        // ====== Thông tin chi tiết ======
        public string? MoTaChiTiet { get; set; }
        public string? NguyenLieu { get; set; }
        public string? HuongVi { get; set; }
        public string? KichThuoc { get; set; }

    }
}
