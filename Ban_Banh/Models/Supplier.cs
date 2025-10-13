using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace Ban_Banh.Models
{
    public class Supplier
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên nhà cung cấp là bắt buộc")]
        public string TenNhaCungCap { get; set; }

        public string? DiaChi { get; set; }
        public string? SoDienThoai { get; set; }
        public string? Email { get; set; }
        public string? Website { get; set; }
        public string? GhiChu { get; set; }

        [ValidateNever]
        public string? TenBanh { get; set; }

        [ValidateNever]
        public decimal? GiaNhap { get; set; }

        [ValidateNever]
        public DateTime? NgayNhap { get; set; }

        // ✅ Danh sách bánh mà nhà cung cấp này cung cấp
        [ValidateNever]
        public List<SupplierProduct>? SupplierProducts { get; set; }
    }

    // ✅ Thêm lớp con SupplierProduct
    public class SupplierProduct
    {
        public int Id { get; set; }
        public int SupplierId { get; set; }
        public int BanhId { get; set; }
        public string? TenBanh { get; set; }
        public decimal GiaNhap { get; set; }
        public DateTime NgayNhap { get; set; }
    }
}
