using System;
using System.Collections.Generic;

namespace Ban_Banh.Models
{
    public class OrderWithProductsViewModel
    {
        public int OrderId { get; set; }
        public int AccountId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int StatusId { get; set; }
        

        public List<OrderProductViewModel> Products { get; set; }
    }

    public class OrderProductViewModel
    {
        public string TenBanh { get; set; }
        public int BanhId { get; set; }
        public int Quantity { get; set; }
        public decimal Gia { get; set; }
        // Bổ sung thêm các thông tin cần thiết để chuẩn bị hàng
        public string BatchCode { get; set; }           // Mã lô hàng
        public string SupplierName { get; set; }        // Tên nhà cung cấp
        public string WarehouseLocation { get; set; }   // Mã vị trí kho
        public string WarehouseZone { get; set; }       // Tên khu kho
    }

}
