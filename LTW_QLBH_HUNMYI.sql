CREATE DATABASE QLBH1
GO
USE QLBH1
GO

-- ==========================================================================================
--                                         TẠO BẢNG
-- ==========================================================================================

----------1. BẢNG KHÁCH HÀNG----------
CREATE TABLE KHACHHANG
(
	MAKH VARCHAR(10) NOT NULL,
	HOTENKH NVARCHAR(50) NOT NULL,
	GIOITINH NVARCHAR(5) CHECK(GIOITINH IN (N'Nam', N'Nữ')),
	SDT CHAR(10),
	EMAIL VARCHAR(50),
	DIACHI NVARCHAR(100),
	NGAYDANGKY DATE DEFAULT GETDATE(),
	CONSTRAINT PK_KH PRIMARY KEY (MAKH),
	CONSTRAINT UQ_KHACHHANG_SDT UNIQUE (SDT),
	CONSTRAINT UQ_KHACHHANG_EMAIL UNIQUE (EMAIL),
	CONSTRAINT CK_KH_EMAIL_FORMAT CHECK (EMAIL LIKE '_%@_%._%')
)
GO

----------2. BẢNG NHÂN VIÊN----------
CREATE TABLE NHANVIEN
(
	MANV VARCHAR(10) NOT NULL PRIMARY KEY,
	HOTENNV NVARCHAR(50) NOT NULL,
	GIOITINH NVARCHAR(5) CHECK(GIOITINH IN (N'Nam', N'Nữ')),
	NGAYSINH DATE,
	NGAYVAOLAM DATE,
	SDT CHAR(10) UNIQUE,
	EMAIL VARCHAR(50) UNIQUE CHECK (EMAIL LIKE '_%@_%._%'),
	DIACHI NVARCHAR(100),
	CHUCVU NVARCHAR(30) CHECK (CHUCVU IN (N'Chủ shop', N'Nhân viên')),
	LUONGCOBAN MONEY CHECK (LUONGCOBAN >= 0),
	TRANGTHAI NVARCHAR(20) DEFAULT N'Đang làm' CHECK (TRANGTHAI IN (N'Đang làm', N'Nghỉ việc')),
	CONSTRAINT CK_NV_NGAY CHECK (NGAYVAOLAM >= NGAYSINH)
)
GO

----------3. BẢNG TÀI KHOẢN----------
CREATE TABLE ACCOUNT
(
	USERID VARCHAR(10) PRIMARY KEY,  
    USERNAME VARCHAR(30) NOT NULL UNIQUE,
    PASSWORDHASH NVARCHAR(256) NOT NULL, -- Lưu mật khẩu đã hash
    VAITRO NVARCHAR(30) NOT NULL CHECK (VAITRO IN (N'Chủ shop', N'Nhân viên', N'Khách')),
    MAKH VARCHAR(10) NULL,
    MANV VARCHAR(10) NULL,
    EMAIL VARCHAR(100) UNIQUE NOT NULL,
    SDT VARCHAR(15) NULL,
    TRANGTHAI NVARCHAR(20) DEFAULT N'Hoạt động' CHECK (TRANGTHAI IN (N'Hoạt động', N'Khóa')),
    FOREIGN KEY (MAKH) REFERENCES KHACHHANG(MAKH),
    FOREIGN KEY (MANV) REFERENCES NHANVIEN(MANV),
    CONSTRAINT CK_ACCOUNT_ROLE CHECK 
	(
        (VAITRO = N'Khách' AND MAKH IS NOT NULL AND MANV IS NULL) OR
        (VAITRO IN (N'Nhân viên', N'Chủ shop') AND MANV IS NOT NULL AND MAKH IS NULL)
    )
)
GO

----------4. BẢNG DANH MỤC----------
CREATE TABLE DANHMUC 
(
    MADM VARCHAR(10) NOT NULL PRIMARY KEY,
    TENDM NVARCHAR(50) UNIQUE NOT NULL,
	MOTA_DM NVARCHAR(255),
    TRANGTHAI NVARCHAR(20) DEFAULT N'Hiển thị' CHECK (TRANGTHAI IN (N'Hiển thị', N'Ẩn'))
)
GO

----------5. BẢNG SẢN PHẨM----------
CREATE TABLE SANPHAM 
(
    MASP VARCHAR(10) PRIMARY KEY NOT NULL,
    TENSP NVARCHAR(100) NOT NULL,
	HINHANH NVARCHAR(200),
	MOTA NVARCHAR(500),
    SOLUONGTON INT DEFAULT 0 CHECK (SOLUONGTON >= 0),
    GIA MONEY CHECK (GIA > 0),
    TRANGTHAI NVARCHAR(20) DEFAULT N'Đang bán' CHECK (TRANGTHAI IN (N'Đang bán', N'Ngừng bán', N'Hết hàng')),
    MADM VARCHAR(10) NOT NULL,
    NGAYTAO DATE DEFAULT GETDATE(),
    CONSTRAINT FK_SP_DM FOREIGN KEY (MADM) REFERENCES DANHMUC(MADM)
)
GO

----------6. BẢNG GIỎ HÀNG----------
CREATE TABLE GIOHANG
(
    MAGH VARCHAR(10) PRIMARY KEY,
    MAKH VARCHAR(10) NOT NULL,
    NGAYTAO DATE DEFAULT GETDATE(),
    TONGSL INT NULL DEFAULT 0,
    FOREIGN KEY (MAKH) REFERENCES KHACHHANG(MAKH)
)
GO

----------7. BẢNG CHI TIẾT GIỎ HÀNG----------
CREATE TABLE CHITIET_GIOHANG
(

    MAGH VARCHAR(10),
    MASP VARCHAR(10),
    SOLUONG INT CHECK (SOLUONG > 0),
    NGAYTHEM DATETIME DEFAULT GETDATE(),
    PRIMARY KEY (MAGH, MASP),
    FOREIGN KEY (MAGH) REFERENCES GIOHANG(MAGH),
    FOREIGN KEY (MASP) REFERENCES SANPHAM(MASP)
)
GO

----------8. BẢNG HÓA ĐƠN BÁN----------
CREATE TABLE HOADON_BAN 
(
    MAHD_BAN VARCHAR(10) NOT NULL PRIMARY KEY,
	NGAYLAP DATETIME DEFAULT GETDATE(),
    MANV VARCHAR(10) NULL, -- Có thể null nếu khách tự đặt online
    MAKH VARCHAR(10) NOT NULL,
    TONGTIEN MONEY DEFAULT 0 CHECK (TONGTIEN >= 0),
    TRANGTHAI NVARCHAR(30) DEFAULT N'Chờ xử lý' 
        CHECK (TRANGTHAI IN (N'Chờ xử lý', N'Đang xử lý', N'Hoàn thành', N'Đã hủy')),
    GHICHU NVARCHAR(500),
    CONSTRAINT FK_HOADON_NHANVIEN FOREIGN KEY (MANV) REFERENCES NHANVIEN(MANV),
    FOREIGN KEY (MAKH) REFERENCES KHACHHANG(MAKH)
)
GO

----------9. BẢNG CHI TIẾT HÓA ĐƠN BÁN----------
CREATE TABLE CTHD_BAN 
(
    MAHD_BAN VARCHAR(10) NOT NULL,
    MASP VARCHAR(10),
    SOLUONG INT CHECK (SOLUONG > 0),
    DONGIA MONEY CHECK (DONGIA >= 0),
    THANHTIEN AS (SOLUONG * DONGIA) PERSISTED,
    CONSTRAINT PK_CTHD_BAN PRIMARY KEY (MAHD_BAN, MASP),
    CONSTRAINT FK_CTHD_HOADON FOREIGN KEY (MAHD_BAN) REFERENCES HOADON_BAN(MAHD_BAN),
    CONSTRAINT FK_CTHD_SANPHAM FOREIGN KEY (MASP) REFERENCES SANPHAM(MASP)
)
GO

----------10. BẢNG XƯỞNG IN----------
CREATE TABLE XUONGIN 
(
    MAXI VARCHAR(10) NOT NULL PRIMARY KEY,
    TENXI NVARCHAR(50) NOT NULL,
    DIACHI NVARCHAR(100),
    SDT VARCHAR(15) UNIQUE,
    EMAIL NVARCHAR(50) UNIQUE CHECK (EMAIL LIKE '_%@_%._%'),
	NGUOILIENHE NVARCHAR(50),
    GHICHU NVARCHAR(MAX) NULL,
    TRANGTHAI NVARCHAR(20) DEFAULT N'Hoạt động' CHECK (TRANGTHAI IN (N'Hoạt động', N'Ngừng hợp tác'))
)
GO

----------11. BẢNG PHIẾU NHẬP----------
CREATE TABLE PHIEUNHAP 
(
    MAPN VARCHAR(10) NOT NULL PRIMARY KEY,
    NGAYNHAP DATETIME DEFAULT GETDATE(),
    MAXI VARCHAR(10),
    MANV VARCHAR(10),
    TONGTIEN MONEY DEFAULT 0,
    TRANGTHAI NVARCHAR(20) DEFAULT N'Đã nhập' CHECK (TRANGTHAI IN (N'Đã nhập', N'Đã hủy')),
    CONSTRAINT FK_PHIEUNHAP_NHACUNGCAP FOREIGN KEY (MAXI) REFERENCES XUONGIN(MAXI),
    FOREIGN KEY (MANV) REFERENCES NHANVIEN(MANV)
)
GO

----------12. BẢNG CHI TIẾT PHIẾU NHẬP----------
CREATE TABLE CHITIETPHIEUNHAP 
(
    MAPN VARCHAR(10) NOT NULL,
    MASP VARCHAR(10),
    SOLUONG INT CHECK (SOLUONG > 0),
    DONGIA MONEY CHECK (DONGIA >= 0),
    THANHTIEN AS (SOLUONG * DONGIA) PERSISTED,
    CONSTRAINT PK_CTPN PRIMARY KEY (MAPN, MASP),
    CONSTRAINT FK_CTPN_PHIEUNHAP FOREIGN KEY (MAPN) REFERENCES PHIEUNHAP(MAPN),
    FOREIGN KEY (MASP) REFERENCES SANPHAM(MASP)
)
GO

-----TRIGGER CẬP NHẬT TỒN KHO MỖI KIA LẬP HÓA ĐƠN BÁN-----
-- Giảm tồn kho khi bán hàng
CREATE TRIGGER TRG_UPDATE_TONKHO_BAN
ON CTHD_BAN
AFTER INSERT
AS
BEGIN
    UPDATE SP
    SET SP.SOLUONGTON = SP.SOLUONGTON - I.SOLUONG
    FROM SANPHAM SP
    JOIN INSERTED I ON SP.MASP = I.MASP
    IF EXISTS (SELECT 1 FROM SANPHAM WHERE SOLUONGTON < 0)
    BEGIN
        RAISERROR(N'Sản phẩm không đủ tồn kho!',16,1)
        ROLLBACK TRANSACTION
    END
END
GO

----- TRIGGER CẬP NHẬT TỒN KHO KHI NHẬP HÀNG -----
-- Tăng tồn kho khi nhập hàng
CREATE TRIGGER TRG_UPDATE_TONKHO_NHAP
ON CHITIETPHIEUNHAP       -- bảng chi tiết phiếu nhập (ví dụ)
AFTER INSERT
AS
BEGIN
    -- Tăng số lượng tồn kho
    UPDATE SP
    SET SP.SOLUONGTON = SP.SOLUONGTON + I.SOLUONG
    FROM SANPHAM SP
    JOIN INSERTED I ON SP.MASP = I.MASP

    -- Kiểm tra nếu số lượng tồn kho bị âm (trường hợp hiếm)
    IF EXISTS (SELECT 1 FROM SANPHAM WHERE SOLUONGTON < 0)
    BEGIN
        RAISERROR(N'Số lượng tồn kho không hợp lệ!', 16, 1)
        ROLLBACK TRANSACTION
    END
END
GO

-----Cập nhật tổng tiền hóa đơn khi thêm mới chi tiết-----
CREATE TRIGGER TRG_UpdateTotalInvoice
ON CTHD_BAN
AFTER INSERT, DELETE, UPDATE
AS
BEGIN
    UPDATE HOADON_BAN
    SET TONGTIEN =
        (SELECT SUM(THANHTIEN) FROM CTHD_BAN WHERE MAHD_BAN = HOADON_BAN.MAHD_BAN)
    WHERE MAHD_BAN IN (
        SELECT MAHD_BAN FROM inserted
        UNION
        SELECT MAHD_BAN FROM deleted
    );
END

-- TRIGGER TỰ ĐỘNG CẬP NHẬT TRẠNG THÁI KHI SỐ LƯỢNG THAY ĐỔI
CREATE OR ALTER TRIGGER TRG_AUTO_UPDATE_TRANGTHAI_SANPHAM
ON SANPHAM
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Cập nhật sang "Hết hàng" nếu số lượng <= 0
    UPDATE SANPHAM
    SET TRANGTHAI = N'Hết hàng'
    WHERE MASP IN (
        SELECT i.MASP 
        FROM inserted i
        WHERE i.SOLUONGTON <= 0 
          AND i.TRANGTHAI != N'Hết hàng'
          AND i.TRANGTHAI != N'Ngừng bán'  -- Không đổi nếu đã ngừng bán
    );
    
    -- Cập nhật lại "Đang bán" nếu có hàng trở lại
    UPDATE SANPHAM
    SET TRANGTHAI = N'Đang bán'
    WHERE MASP IN (
        SELECT i.MASP 
        FROM inserted i
        WHERE i.SOLUONGTON > 0 
          AND i.TRANGTHAI = N'Hết hàng'
    );
END
GO

-- CẬP NHẬT TẤT CẢ SẢN PHẨM HẾT HÀNG NGAY BÂY GIỜ
UPDATE SANPHAM 
SET TRANGTHAI = N'Hết hàng' 
WHERE SOLUONGTON <= 0 
  AND TRANGTHAI NOT IN (N'Ngừng bán', N'Hết hàng');

-- KIỂM TRA KẾT QUẢ
SELECT MASP, TENSP, SOLUONGTON, TRANGTHAI 
FROM SANPHAM 
WHERE SOLUONGTON <= 0;

CREATE TRIGGER TRG_AUTO_SYNC_ACCOUNT_STATUS 
ON NHANVIEN 
AFTER UPDATE 
AS 
BEGIN
    SET NOCOUNT ON;
    
    UPDATE acc
    SET acc.TRANGTHAI = 
        CASE 
            WHEN i.TRANGTHAI = N'Nghỉ việc' THEN N'Khóa'
            WHEN i.TRANGTHAI = N'Đang làm' THEN N'Hoạt động'
            ELSE acc.TRANGTHAI
        END,
        acc.VAITRO = 
        CASE 
            WHEN i.CHUCVU = N'Chủ shop' THEN N'Chủ shop'
            ELSE N'Nhân viên'
        END
    FROM ACCOUNT acc
    INNER JOIN inserted i ON acc.MANV = i.MANV
    WHERE acc.MANV IS NOT NULL;
END
-- ==========================================================================================
--                                         THÊM DỮ LIỆU MẪU
-- ==========================================================================================

-- 1. Thêm khách hàng mẫu
INSERT INTO KHACHHANG (MAKH,HOTENKH, GIOITINH, SDT, EMAIL, DIACHI, NGAYDANGKY) VALUES
('KH1',N'Trần Tuấn Huy', N'Nam', '0987654321', 'customer1@gmail.com', N'Quận 1, TP.HCM', '2024-01-01'),
('KH2',N'Nguyễn Thị D', N'Nữ', '0123456789', 'customer2@gmail.com', N'Quận 12, TP.HCM', '2025-08-01')
GO

-- 2. Thêm nhân viên
INSERT INTO NHANVIEN (MANV, HOTENNV, GIOITINH, NGAYSINH, NGAYVAOLAM, SDT, EMAIL, DIACHI, CHUCVU, LUONGCOBAN, TRANGTHAI) VALUES
('NV01', N'Nguyễn Văn A', N'Nam', '1990-01-01', '2020-01-01', '0901234567', 'owner@hunmyi.com', N'TP.HCM', N'Chủ Shop', 20000000, N'Đang làm'),
('NV02', N'Trần Thị B', N'Nữ', '1995-05-05', '2021-06-01', '0912345678', 'staff1@hunmyi.com', N'TP.HCM', N'Nhân Viên', 10000000, N'Đang làm'),
('NV03', N'Lê Văn C', N'Nam', '1998-03-10', '2022-03-01', '0923456789', 'staff2@hunmyi.com', N'TP.HCM', N'Nhân Viên', 10000000, N'Đang làm')
GO

-- 3. Thêm tài khoản (mật khẩu: 123456)
INSERT INTO ACCOUNT (USERID ,USERNAME,PASSWORDHASH,VAITRO,MAKH,MANV,EMAIL,SDT,TRANGTHAI) VALUES
('ID1', 'owner', N'e10adc3949ba59abbe56e057f20f883e', N'Chủ shop', NULL, 'NV1', 'owner@hunmyi.com', '0901234567', N'Hoạt động'),
('ID2', 'staff1', N'e10adc3949ba59abbe56e057f20f883e', N'Nhân viên', NULL, 'NV2', 'staff1@hunmyi.com', '0912345678', N'Hoạt động'),
('ID3', 'staff2', N'e10adc3949ba59abbe56e057f20f883e', N'Nhân viên', NULL, 'NV3', 'staff2@hunmyi.com', '0923456789', N'Hoạt động'),
('ID4', 'customer1', N'e10adc3949ba59abbe56e057f20f883e', N'Khách', 'KH1', NULL, 'customer1@gmail.com', '0987654321', N'Hoạt động'),
('ID5', 'customer2', N'e10adc3949ba59abbe56e057f20f883e', N'Khách', 'KH2', NULL, 'customer2@gmail.com', '0987654321', N'Hoạt động')
GO


-- 4. Thêm danh mục
INSERT INTO DANHMUC (MADM ,TENDM, MOTA_DM, TRANGTHAI) VALUES
('DM1',N'Pin Gỗ ', N'Pin cài áo, cặp ', N'Hiển thị'),
('DM2',N'Standee Gacha', N'Standee 3cm từ nhiều fandom ', N'Hiển thị'),
('DM3',N'Badge', N'Các loại pin cài áo và cặp ', N'Hiển thị'),
('DM4',N'Sticker', N'Sticker đa dạng về mẫu mã ', N'Hiển thị'),
('DM5',N'Phonecharm', N'Móc treo điện thoại và chìa khóa ', N'Hiển thị'),
('DM6',N'Card Gacha', N'Hình các nhân vật ', N'Hiển thị')


-- 5. Thêm sản phẩm
INSERT INTO SANPHAM (MASP, TENSP, HINHANH, MOTA, SOLUONGTON, GIA, TRANGTHAI, MADM, NGAYTAO) VALUES
('SP1', N'Pin Gỗ HSR', 'pingo1.jpg', N'Pin gỗ Honkai: Star Rail, nhiều mẫu', 49, 40000, N'Đang bán', 'DM1', '2024-01-01'),
('SP2', N'Pin Gỗ GI', 'pingo2.jpg', N'Pin gỗ Gensin Impact, nhiều mẫu', 59, 40000, N'Đang bán', 'DM1', '2024-01-01'),
('SP3', N'Pin Gỗ ZZZ', 'pingo3.jpg', N'Pin gỗ Zenless Zone Zero, nhiều mẫu', 68, 40000, N'Đang bán', 'DM1', '2024-01-01'),

('SP4', N'Honkai: Star Rail', 'standee1.jpg', N'Mô hình dựng đứng các nhân vật Honkai: Star Rail', 44, 30000, N'Đang bán', 'DM2', '2024-01-01'),
('SP5', N'Gensin Impact ', 'standee2.jpg', N'Mô hình dựng đứng các nhân vật Gensin Impact', 38, 30000, N'Đang bán', 'DM2', '2024-01-01'),
('SP6', N'Zenless Zone Zero', 'standee3.jpg', N'Mô hình dựng đứng các nhân vật Zenless Zone Zero', 78, 30000, N'Đang bán', 'DM2', '2024-01-01'),

('SP7', N'Gensin Impact', 'badge1.jpg', N'Huy hiệu cài áo hình các nhân vật trong Gensin Impact', 46, 20000, N'Đang bán', 'DM3', '2024-01-01'),
('SP8', N'Honkai: Star Rail', 'badge2.jpg', N'Huy hiệu cài áo hình các nhân vật trong Honkai: Star Rail', 54, 20000, N'Đang bán', 'DM3', '2024-01-01'),

('SP9', N'Gensin Impact', 'sticker1.jpg', N'Sticker hình các nhân vật trong Gensin Impact', 76, 15000, N'Đang bán', 'DM4', '2024-01-01'),
('SP10', N'Mochi', 'sticker2.jpg', N'Sticker hình các nhân vật trong Mochi', 42, 15000, N'Đang bán', 'DM4', '2024-01-01'),

('SP11', N'Kimetsu no Yaiba', 'phonecharm1.jpg', N'Móc điện thoại hình các nhân vật trong Kimetsu no Yaiba', 45, 30000, N'Đang bán', 'DM5', '2024-01-01'),
('SP12', N'Singger Board', 'phonecharm2.jpg', N'Móc điện thoại hình các nhân vật trong Singger Board', 37, 30000, N'Đang bán', 'DM5', '2024-01-01'),

('SP13', N'Bling', 'card1.jpg', N'Card bo góc hình các nhân vật trong Bling', 57, 30000, N'Đang bán', 'DM6', '2024-01-01'),
('SP14', N'Spot Light', 'card2.jpg', N'Card bo góc hình các nhân vật trong Spot Light', 59, 30000, N'Đang bán', 'DM6', '2024-01-01');
GO


-- 6. Tạo giỏ hàng cho khách
INSERT INTO GIOHANG VALUES 
('GH1', 'KH1', '2024-01-01', 0),
('GH2', 'KH2', '2025-08-11', 0)
GO



SELECT * FROM KHACHHANG
SELECT * FROM NHANVIEN
SELECT * FROM ACCOUNT
SELECT * FROM DANHMUC
SELECT * FROM SANPHAM
SELECT * FROM GIOHANG
SELECT * FROM CHITIET_GIOHANG
SELECT * FROM HOADON_BAN
SELECT * FROM CTHD_BAN
SELECT * FROM XUONGIN
SELECT * FROM PHIEUNHAP
SELECT * FROM CHITIETPHIEUNHAP



