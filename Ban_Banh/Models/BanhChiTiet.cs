// Models/BanhChiTiet.cs
namespace Ban_Banh.Models
{
    public class BanhChiTiet
    {
        public int Id { get; set; }
        public int BanhId { get; set; }
        public string MoTaChiTiet { get; set; }
        public string NguyenLieu { get; set; }
        public string HuongVi { get; set; }
        public string KichThuoc { get; set; }

        public virtual Banh Banh { get; set; }
    }
}
