------------------------------------------------------------
-- 1️⃣ TẠO CƠ SỞ DỮ LIỆU
------------------------------------------------------------
CREATE DATABASE BanBanhDB;
GO

USE BanBanhDB;
GO


------------------------------------------------------------
-- 1️⃣ BẢNG ACCOUNT
------------------------------------------------------------
CREATE TABLE Account (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    FullName NVARCHAR(255) NOT NULL,
    Email NVARCHAR(255) NOT NULL UNIQUE,
    Password NVARCHAR(255) NOT NULL,
    AvatarUrl NVARCHAR(255) NULL,
    Phone NVARCHAR(20) NULL,
    Address NVARCHAR(255) NULL,
    IsLocked BIT NOT NULL DEFAULT 0,        -- 0: hoạt động, 1: bị khóa
    RoleId INT NULL                         -- liên kết đến bảng Role
);
GO

------------------------------------------------------------
-- 2️⃣ BẢNG ROLE (quản lý các chức vụ: Admin, Order, Kho, Bếp, Ship, ...)
------------------------------------------------------------
CREATE TABLE Role (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    RoleName NVARCHAR(50) NOT NULL UNIQUE,   -- ví dụ: 'Admin', 'Order', 'Kho', 'Bếp', 'Ship'
    Description NVARCHAR(255) NULL
);
GO

------------------------------------------------------------
-- 3️⃣ KHÓA NGOẠI NỐI LẠI ACCOUNT ↔ ROLE
------------------------------------------------------------
ALTER TABLE Account
ADD CONSTRAINT FK_Account_Role
FOREIGN KEY (RoleId) REFERENCES Role(Id);
GO

GO

------------------------------------------------------------
-- 4️⃣ BẢNG CATEGORY
------------------------------------------------------------
CREATE TABLE Category (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    TenDanhMuc NVARCHAR(100) NOT NULL
);
GO

-- Dữ liệu mẫu cho Category
INSERT INTO Category (TenDanhMuc)
VALUES 
(N'Bánh Mì'),
(N'Bánh Kem'),
(N'Bánh Bao');
GO

------------------------------------------------------------
-- 5️⃣ BẢNG BÁNH
------------------------------------------------------------
CREATE TABLE Banh (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    TenBanh NVARCHAR(100) NOT NULL,
    MoTa NVARCHAR(255),
    Gia DECIMAL(18,2) NOT NULL,
    HinhAnh NVARCHAR(255),
    CategoryId INT NULL,
    FOREIGN KEY (CategoryId) REFERENCES Category(Id)
);
GO

-- Dữ liệu mẫu cho Bánh
INSERT INTO Banh (TenBanh, MoTa, Gia, HinhAnh, CategoryId)
VALUES 
(N'Bánh Mì', N'Bánh mì truyền thống Việt Nam', 15000, 'https://gamek.mediacdn.vn/133514250583805952/2020/1/30/wallpaper-1073266-1580374178391532575408.jpg', 1),
(N'Bánh Kem', N'Bánh kem socola thơm ngon', 120000, 'https://th.bing.com/th/id/OIP.w3O2dsmn0el9QxsX3wuRUQHaEK?o=7&cb=12rm=3&rs=1&pid=ImgDetMain&o=7&rm=3', 2),
(N'Bánh Bao', N'Bánh bao nhân thịt nóng hổi', 20000, 'https://th.bing.com/th/id/R.c0ceb77bd3fee3e527d26b8ce5d2db3a?rik=xrAmeMjfoWV84w&pid=ImgRaw&r=0', 3);
GO

------------------------------------------------------------
-- 6️⃣ BẢNG GIỎ HÀNG (CART)
------------------------------------------------------------
CREATE TABLE Cart (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    AccountId INT NOT NULL,
    BanhId INT NOT NULL,
    Quantity INT NOT NULL DEFAULT 1,
    CreatedAt DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (AccountId) REFERENCES Account(Id),
    FOREIGN KEY (BanhId) REFERENCES Banh(Id)
);
GO


------------------------------------------------------------
-- BẢNG VỊ TRÍ KHO (WAREHOUSE LOCATION)
------------------------------------------------------------
CREATE TABLE WarehouseLocation (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    MaViTri NVARCHAR(50) NOT NULL UNIQUE,
    TenKhu NVARCHAR(100) NULL,
    GhiChu NVARCHAR(255) NULL
);
GO

-- Dữ liệu mẫu
INSERT INTO WarehouseLocation (MaViTri, TenKhu, GhiChu)
VALUES
(N'A1', N'Khu A - Tầng 1', N'Khu chứa bánh mì'),
(N'B1', N'Khu B - Tầng 1', N'Khu chứa bánh kem'),
(N'C1', N'Khu C - Tầng 1', N'Khu chứa bánh bao');
GO
------------------------------------------------------------
-- 1️⃣4️⃣ BẢNG NHÀ CUNG CẤP (SUPPLIER)
------------------------------------------------------------
CREATE TABLE Supplier (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    TenNhaCungCap NVARCHAR(255) NOT NULL,
    DiaChi NVARCHAR(255),
    SoDienThoai NVARCHAR(20),
    Email NVARCHAR(255),
    Website NVARCHAR(255),
    GhiChu NVARCHAR(500)
);
GO
-- Dữ liệu mẫu
INSERT INTO Supplier (TenNhaCungCap, DiaChi, SoDienThoai, Email, Website)
VALUES
(N'Công ty Bánh Việt', N'Hà Nội', N'0909123456', N'supplier1@banhviet.vn', N'https://banhviet.vn'),
(N'Bánh Ngon 24h', N'Hồ Chí Minh', N'0909789123', N'supplier2@banhngon.vn', N'https://banhngon.vn');
go
------------------------------------------------------------
-- 9️⃣ BẢNG TỒN KHO (INVENTORY)
------------------------------------------------------------
CREATE TABLE Inventory (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    BanhId INT NOT NULL,
    SoLuong INT NOT NULL DEFAULT 0,
    NgaySanXuat DATE NULL,
    HanSuDung DATE NULL,
    LastUpdated DATETIME DEFAULT GETDATE(),
    BatchCode AS CONCAT('LO', RIGHT('0000' + CAST(Id AS VARCHAR(4)), 4)) PERSISTED,
    SupplierId INT NULL,
    WarehouseLocationId INT NULL,
    FOREIGN KEY (SupplierId) REFERENCES Supplier(Id),
    FOREIGN KEY (WarehouseLocationId) REFERENCES WarehouseLocation(Id),
    FOREIGN KEY (BanhId) REFERENCES Banh(Id)
);
GO

CREATE TABLE OrderStatus (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    StatusName NVARCHAR(50) NOT NULL UNIQUE
);
GO

-- Dữ liệu mặc định
INSERT INTO OrderStatus (StatusName)
VALUES 
    (N'Pending'),      -- Chờ xử lý
    (N'Processing'),   -- Đang chuẩn bị
    (N'ReadyToShip'),
    (N'Shipped'),      -- Đã gửi hàng
    (N'Completed'),    -- Hoàn thành
    (N'Cancelled');    -- Đã hủy
GO

------------------------------------------------------------
-- 7️⃣ BẢNG ĐƠN HÀNG (ORDER)
------------------------------------------------------------
CREATE TABLE [Order] (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    AccountId INT NOT NULL,
    CreatedAt DATETIME NOT NULL,
    UpdatedAt DATETIME NULL,
    PreparedQuantity INT NOT NULL DEFAULT 0,
    StatusId INT NOT NULL DEFAULT 1,
    FOREIGN KEY (AccountId) REFERENCES Account(Id),
    FOREIGN KEY (StatusId) REFERENCES OrderStatus(Id)
);
GO

CREATE TABLE OrderDetailStatus (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    StatusName NVARCHAR(50) NOT NULL UNIQUE
);
GO

INSERT INTO OrderDetailStatus (StatusName)
VALUES (N'Chưa đủ'), (N'Đã đủ');
GO

------------------------------------------------------------
-- 8️⃣ BẢNG CHI TIẾT ĐƠN HÀNG (ORDER DETAIL)
------------------------------------------------------------
CREATE TABLE OrderDetail (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    OrderId INT NOT NULL,
    BanhId INT NOT NULL,
    Quantity INT NOT NULL,
    BatchCode NVARCHAR(20) NULL,
    InventoryId INT NULL,
    StatusId INT NOT NULL DEFAULT 1,
    FOREIGN KEY (StatusId) REFERENCES OrderDetailStatus(Id),
    FOREIGN KEY (InventoryId) REFERENCES Inventory(Id),
    FOREIGN KEY (OrderId) REFERENCES [Order](Id),
    FOREIGN KEY (BanhId) REFERENCES Banh(Id)
);
GO


------------------------------------------------------------
-- 🔟 BẢNG HÀNG HẾT HẠN (EXPIRED INVENTORY)
------------------------------------------------------------
IF OBJECT_ID('ExpiredInventory', 'U') IS NOT NULL
    DROP TABLE ExpiredInventory;
GO

CREATE TABLE ExpiredInventory (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    BanhId INT NOT NULL,
    SoLuong INT NOT NULL,
    NgaySanXuat DATE NULL,
    HanSuDung DATE NULL,
    NguyenNhan NVARCHAR(255) NULL,         -- 🆕 Cột mới: nguyên nhân
    MovedAt DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (BanhId) REFERENCES Banh(Id)
);
GO

------------------------------------------------------------
-- 1️⃣1️⃣ THỦ TỤC CHUYỂN HÀNG HẾT HẠN
------------------------------------------------------------
IF OBJECT_ID('sp_MoveExpiredInventory', 'P') IS NOT NULL
    DROP PROCEDURE sp_MoveExpiredInventory;
GO

CREATE PROCEDURE sp_MoveExpiredInventory
AS
BEGIN
    SET NOCOUNT ON;

    -- Chèn sản phẩm hết hạn sang bảng ExpiredInventory
    INSERT INTO ExpiredInventory (BanhId, SoLuong, NgaySanXuat, HanSuDung, NguyenNhan)
    SELECT 
        BanhId, 
        SoLuong, 
        NgaySanXuat, 
        HanSuDung, 
        N'Hết hạn sử dụng' AS NguyenNhan  -- 🆕 Thêm nguyên nhân mặc định
    FROM Inventory
    WHERE HanSuDung < CAST(GETDATE() AS DATE);

    -- Xóa sản phẩm hết hạn khỏi Inventory
    DELETE FROM Inventory
    WHERE HanSuDung < CAST(GETDATE() AS DATE);
END;


GO


-- Thực thi thủ tục ngay để đảm bảo hoạt động
EXEC sp_MoveExpiredInventory;
GO

-- Thêm các vai trò
INSERT INTO Role (RoleName, Description) VALUES
('Admin', N'Quản trị hệ thống'),
('Order', N'Nhân viên nhận đơn'),
('Kho', N'Nhân viên kho'),
('Bep', N'Nhân viên bếp'),
('Ship', N'Nhân viên giao hàng'),
('User', N'Khách hàng');


------------------------------------------------------------
-- 1️⃣3️⃣ DỮ LIỆU ADMIN MẪU
------------------------------------------------------------
INSERT INTO Account (FullName, Email, [Password], AvatarUrl, Phone, [Address], RoleId)
VALUES 
(N'Nguyễn Văn A', N'a@example.com', N'8D969EEF6ECAD3C29A3A629280E686CF0C3F5D5A86AFF3CA12020C923ADC6C92', N'avatar1.jpg', N'0909000001', N'Hà Nội',1),
(N'Trần Thị B', N'b@example.com', N'8D969EEF6ECAD3C29A3A629280E686CF0C3F5D5A86AFF3CA12020C923ADC6C92', N'avatar2.jpg', N'0909000002', N'Hồ Chí Minh',2),
(N'Lê Văn C', N'c@example.com', N'8D969EEF6ECAD3C29A3A629280E686CF0C3F5D5A86AFF3CA12020C923ADC6C92', N'avatar3.jpg', N'0909000003', N'Đà Nẵng',3),
(N'Lê Văn D', N'd@example.com', N'8D969EEF6ECAD3C29A3A629280E686CF0C3F5D5A86AFF3CA12020C923ADC6C92', N'avatar3.jpg', N'0909000003', N'Đà Nẵng',4),
(N'Lê Văn E', N'E@example.com', N'8D969EEF6ECAD3C29A3A629280E686CF0C3F5D5A86AFF3CA12020C923ADC6C92', N'avatar3.jpg', N'0909000003', N'Đà Nẵng',5);
GO






------------------------------------------------------------
-- 1️⃣5️⃣ BẢNG LIÊN KẾT NHÀ CUNG CẤP - BÁNH (SUPPLIER_PRODUCT)
------------------------------------------------------------
CREATE TABLE SupplierProduct (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    SupplierId INT NOT NULL,
    BanhId INT NOT NULL,
    GiaNhap DECIMAL(18,2) NOT NULL,
    NgayNhap DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (SupplierId) REFERENCES Supplier(Id),
    FOREIGN KEY (BanhId) REFERENCES Banh(Id)
);
GO



INSERT INTO SupplierProduct (SupplierId, BanhId, GiaNhap)
VALUES
(1, 1, 10000),
(1, 2, 95000),
(2, 3, 15000);
GO





------------------------------------------
CREATE TABLE BanhChiTiet (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    BanhId INT NOT NULL,
    MoTaChiTiet NVARCHAR(MAX),
    NguyenLieu NVARCHAR(MAX),
    HuongVi NVARCHAR(255),
    KichThuoc NVARCHAR(100),
    FOREIGN KEY (BanhId) REFERENCES Banh(Id) ON DELETE CASCADE
);
GO

CREATE TABLE InventoryHistory (
    Id INT,
    BanhId INT NOT NULL,
    SoLuong INT NOT NULL DEFAULT 0,
    NgaySanXuat DATE NULL,
    HanSuDung DATE NULL,
    LastUpdated DATETIME DEFAULT GETDATE(),
    BatchCode AS CONCAT('LO', RIGHT('0000' + CAST(Id AS VARCHAR(4)), 4)) PERSISTED,
    SupplierId INT NULL,
    WarehouseLocationId INT NULL,
    FOREIGN KEY (SupplierId) REFERENCES Supplier(Id),
    FOREIGN KEY (WarehouseLocationId) REFERENCES WarehouseLocation(Id),
    FOREIGN KEY (BanhId) REFERENCES Banh(Id)
);
go
------------------------------------------------
-- 1️⃣ Bảng ProductStatus (bảng danh mục trạng thái)
------------------------------------------------
IF OBJECT_ID('ProductStatus', 'U') IS NOT NULL
    DROP TABLE ProductStatus;
GO

CREATE TABLE ProductStatus (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    StatusName NVARCHAR(50) NOT NULL UNIQUE
);
GO

-- Thêm 3 trạng thái mặc định
INSERT INTO ProductStatus (StatusName)
VALUES (N'Trong kho'), (N'Xuất kho'), (N'Đã bán'), (N'Lỗi');
GO


------------------------------------------------
-- 2️⃣ Bảng ProductInstance (sửa lại để dùng khóa ngoại StatusId)
------------------------------------------------
IF OBJECT_ID('ProductInstance', 'U') IS NOT NULL
    DROP TABLE ProductInstance;
GO

CREATE TABLE ProductInstance (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    InventoryId INT NOT NULL,
    BanhId INT NOT NULL,
    SerialNumber AS CONCAT('SP', RIGHT('000000' + CAST(Id AS VARCHAR(6)), 6)) PERSISTED,
    StatusId INT NOT NULL DEFAULT 1, -- Mặc định là "Trong kho"
    DateAdded DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (InventoryId) REFERENCES Inventory(Id),
    FOREIGN KEY (BanhId) REFERENCES Banh(Id),
    FOREIGN KEY (StatusId) REFERENCES ProductStatus(Id)
);
GO


------------------------------------------------
-- 3️⃣ Trigger trg_AfterInsert_Inventory (giữ nguyên logic cũ)
------------------------------------------------
IF OBJECT_ID('trg_AfterInsert_Inventory', 'TR') IS NOT NULL
    DROP TRIGGER trg_AfterInsert_Inventory;
GO

CREATE TRIGGER trg_AfterInsert_Inventory
ON Inventory
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    -- 1️⃣ Ghi lịch sử nhập kho
    INSERT INTO InventoryHistory (
        Id,
        BanhId,
        SoLuong,
        NgaySanXuat,
        HanSuDung,
        LastUpdated,
        SupplierId,
        WarehouseLocationId
    )
    SELECT 
        Id,
        BanhId,
        SoLuong,
        NgaySanXuat,
        HanSuDung,
        LastUpdated,
        SupplierId,
        WarehouseLocationId
    FROM inserted;

    -- 2️⃣ Tạo bản ghi sản phẩm chi tiết (mỗi chiếc bánh riêng lẻ)
    ;WITH Numbers AS (
        SELECT i.Id AS InventoryId, i.BanhId, n.Number
        FROM inserted i
        CROSS APPLY (
            SELECT TOP (i.SoLuong) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS Number
            FROM master.dbo.spt_values
        ) n
    )
    INSERT INTO ProductInstance (InventoryId, BanhId)
    SELECT InventoryId, BanhId
    FROM Numbers;
END;
GO

---------------
CREATE TABLE PreparedProduct (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    OrderDetailId INT NOT NULL,
    ProductInstanceId BIGINT NOT NULL,
    DatePrepared DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (OrderDetailId) REFERENCES OrderDetail(Id),
    FOREIGN KEY (ProductInstanceId) REFERENCES ProductInstance(Id)
);
GO
CREATE TRIGGER trg_PreparedProduct_ValidateBatch
ON PreparedProduct
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1
        FROM INSERTED i
        JOIN OrderDetail od ON i.OrderDetailId = od.Id
        JOIN ProductInstance pi ON i.ProductInstanceId = pi.Id
        JOIN Inventory inv ON pi.InventoryId = inv.Id
        WHERE od.BatchCode IS NOT NULL
          AND inv.BatchCode IS NOT NULL
          AND od.BatchCode <> inv.BatchCode
    )
    BEGIN
        RAISERROR(N'Lỗi: Mã lô của sản phẩm không trùng với mã lô của chi tiết đơn hàng.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;
GO

CREATE TRIGGER trg_PreparedProduct_UpdateOrderDetailStatus
ON PreparedProduct
AFTER INSERT, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE od
    SET od.StatusId =
        CASE
            WHEN (
                SELECT COUNT(*) 
                FROM PreparedProduct pp 
                WHERE pp.OrderDetailId = od.Id
            ) >= od.Quantity THEN 2  -- Đã đủ
            ELSE 1                   -- Chưa đủ
        END
    FROM OrderDetail od
    WHERE od.Id IN (
        SELECT DISTINCT OrderDetailId FROM INSERTED
        UNION
        SELECT DISTINCT OrderDetailId FROM DELETED
    );
END;
GO

CREATE TRIGGER trg_PreparedProduct_UpdateOrderPreparedQuantity
ON PreparedProduct
AFTER INSERT, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE o
    SET o.PreparedQuantity = ISNULL((
        SELECT COUNT(pp.Id)
        FROM OrderDetail od
        LEFT JOIN PreparedProduct pp ON od.Id = pp.OrderDetailId
        WHERE od.OrderId = o.Id
    ), 0)
    FROM [Order] o
    WHERE o.Id IN (
        SELECT DISTINCT od.OrderId
        FROM OrderDetail od
        INNER JOIN (
            SELECT DISTINCT OrderDetailId FROM INSERTED
            UNION
            SELECT DISTINCT OrderDetailId FROM DELETED
        ) x ON od.Id = x.OrderDetailId
    );
END;
GO

CREATE OR ALTER TRIGGER trg_OrderDetail_UpdateOrderStatus
ON OrderDetail
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE o
    SET o.StatusId =
        CASE
            -- Nếu tất cả OrderDetail của đơn đều "Đã đủ" (StatusId = 2) ⇒ chuyển trạng thái "Shipped" (Id = 3)
            WHEN NOT EXISTS (
                SELECT 1
                FROM OrderDetail od
                WHERE od.OrderId = o.Id AND od.StatusId <> 2
            ) THEN 3
            -- Ngược lại, nếu còn món chưa đủ ⇒ để nguyên trạng thái cũ (Pending hoặc Processing)
            ELSE o.StatusId
        END
    FROM [Order] o
    WHERE o.Id IN (SELECT DISTINCT OrderId FROM INSERTED);
END;
GO
--------------------------------------
----------------------------------------
-- 1.1. Thêm cột DriverId vào bảng [Order]
IF COL_LENGTH('Order', 'DriverId') IS NULL
BEGIN
    ALTER TABLE [Order]
    ADD DriverId INT NULL;

    ALTER TABLE [Order]
    ADD CONSTRAINT FK_Order_Driver FOREIGN KEY (DriverId) REFERENCES Account(Id);
END
GO

-- 1.2. View tổng hợp cho tài xế (gom thông tin khách + sản phẩm)
IF OBJECT_ID('vw_DriverOrders', 'V') IS NOT NULL
    DROP VIEW vw_DriverOrders;
GO

CREATE VIEW vw_DriverOrders
AS
SELECT
    o.Id AS OrderId,
    o.AccountId AS CustomerAccountId,
    cust.FullName AS CustomerName,
    cust.Email AS CustomerEmail,
    cust.Phone AS CustomerPhone,
    cust.Address AS CustomerAddress,
    o.DriverId,
    drv.FullName AS DriverName,
    s.StatusName,
    o.CreatedAt,
    o.UpdatedAt,
    od.Id AS OrderDetailId,
    od.BanhId,
    b.TenBanh,
    od.Quantity,
    b.Gia,

    -- Thêm các cột cần thiết
    od.BatchCode AS BatchCode,
    inv.Id AS InventoryId,
    inv.BatchCode AS InventoryBatchCode,   -- (tính sẵn trong Inventory)
    sup.TenNhaCungCap AS SupplierName,
    wl.MaViTri AS WarehouseLocation,
    wl.TenKhu AS WarehouseZone
FROM [Order] o
INNER JOIN Account cust ON o.AccountId = cust.Id
LEFT JOIN Account drv ON o.DriverId = drv.Id
INNER JOIN OrderStatus s ON o.StatusId = s.Id
INNER JOIN OrderDetail od ON od.OrderId = o.Id
INNER JOIN Banh b ON od.BanhId = b.Id
LEFT JOIN Inventory inv ON od.InventoryId = inv.Id
LEFT JOIN Supplier sup ON inv.SupplierId = sup.Id
LEFT JOIN WarehouseLocation wl ON inv.WarehouseLocationId = wl.Id;


GO

-- 1.3. Proc: Nhận đơn (tài xế nhận, đổi trạng thái sang Shipped)
IF OBJECT_ID('sp_AssignOrderToDriver', 'P') IS NOT NULL
    DROP PROCEDURE sp_AssignOrderToDriver;
GO
CREATE PROCEDURE sp_AssignOrderToDriver
    @OrderId INT,
    @DriverId INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @readyId INT = (SELECT Id FROM OrderStatus WHERE StatusName = N'ReadyToShip');
    DECLARE @shippedId INT = (SELECT Id FROM OrderStatus WHERE StatusName = N'Shipped');

    IF NOT EXISTS (SELECT 1 FROM [Order] WHERE Id = @OrderId AND StatusId = @readyId)
    BEGIN
        RAISERROR(N'Đơn không tồn tại hoặc chưa sẵn sàng để giao.', 16, 1);
        RETURN;
    END

    UPDATE [Order]
    SET DriverId = @DriverId,
        StatusId = @shippedId,
        UpdatedAt = GETDATE()
    WHERE Id = @OrderId;
END;
GO

-- 1.4. Proc: Đánh dấu đã giao (tài xế chuyển sang Completed)
IF OBJECT_ID('sp_MarkOrderCompleted', 'P') IS NOT NULL
    DROP PROCEDURE sp_MarkOrderCompleted;
GO
CREATE PROCEDURE sp_MarkOrderCompleted
    @OrderId INT,
    @DriverId INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @shippedId INT = (SELECT Id FROM OrderStatus WHERE StatusName = N'Shipped');
    DECLARE @completedId INT = (SELECT Id FROM OrderStatus WHERE StatusName = N'Completed');

    IF NOT EXISTS (SELECT 1 FROM [Order] WHERE Id = @OrderId AND StatusId = @shippedId AND DriverId = @DriverId)
    BEGIN
        RAISERROR(N'Đơn không hợp lệ hoặc không thuộc tài xế này.', 16, 1);
        RETURN;
    END

    UPDATE [Order]
    SET StatusId = @completedId,
        UpdatedAt = GETDATE()
    WHERE Id = @OrderId;
END;
GO
