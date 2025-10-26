namespace Ban_Banh.Models
{
    public class Inventory
    {
        public int Id { get; set; }
        public int BanhId { get; set; }
        public string TenBanh { get; set; }
        public int SoLuong { get; set; }
        public DateTime LastUpdated { get; set; }

        public DateTime? NgaySanXuat { get; set; }
        public DateTime? HanSuDung { get; set; }

        // 🔹 Mới thêm:
        public int? SupplierId { get; set; }
        public string TenNhaCungCap { get; set; }

        public int? WarehouseLocationId { get; set; }
        public string MaViTri { get; set; }
        public string TenKhu { get; set; }

    }
}
