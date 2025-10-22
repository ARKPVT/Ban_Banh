using System;
using System.Collections.Generic;

namespace Ban_Banh.Models
{
    public class DriverOrderItemVM
    {
        public int OrderDetailId { get; set; }
        public int BanhId { get; set; }
        public string TenBanh { get; set; } = "";
        public int Quantity { get; set; }
        public decimal Gia { get; set; }

        public string? BatchCode { get; set; }
        public int? InventoryId { get; set; }
        public string? InventoryBatchCode { get; set; }
        public string? SupplierName { get; set; }
        public string? WarehouseLocation { get; set; }
        public string? WarehouseZone { get; set; }
    }

    public class DriverOrderVM
    {
        public int OrderId { get; set; }
        public string StatusName { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Driver
        public int? DriverId { get; set; }
        public string? DriverName { get; set; }

        // Customer
        public int CustomerAccountId { get; set; }
        public string CustomerName { get; set; } = "";
        public string? CustomerEmail { get; set; }
        public string? CustomerPhone { get; set; }
        public string? CustomerAddress { get; set; }

        public List<DriverOrderItemVM> Items { get; set; } = new();
    }
}
