-- =============================================
-- HỆ THỐNG QUẢN LÝ VÀ PHÊ DUYỆT THU CHI NỘI BỘ
-- BẢN PRODUCTION - TÍCH HỢP XÁC THỰC BCRYPT (CÓ MODULE THU NỘI BỘ)
-- =============================================

USE master;
GO

-- Xóa database cũ nếu tồn tại để làm sạch
IF EXISTS (SELECT name FROM sys.databases WHERE name = 'QL_ThuChiNoiBo')
BEGIN
    ALTER DATABASE QL_ThuChiNoiBo SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE QL_ThuChiNoiBo;
END
GO

CREATE DATABASE QL_ThuChiNoiBo;
GO
USE QL_ThuChiNoiBo;
GO

-- =============================================
-- 1. CỤM DANH MỤC (MASTER DATA)
-- =============================================

-- Chức vụ
CREATE TABLE ChucVu (
    MaChucVu INT IDENTITY(1,1) PRIMARY KEY,
    TenChucVu NVARCHAR(100) NOT NULL,
    TrangThaiHoatDong BIT DEFAULT 1
);

-- Phòng ban
CREATE TABLE PhongBan (
    MaPhongBan INT IDENTITY(1,1) PRIMARY KEY,
    TenPhongBan NVARCHAR(150) NOT NULL,
    MaTruongPhong INT NULL, 
    TrangThaiHoatDong BIT DEFAULT 1
);

-- Nhân viên
CREATE TABLE NhanVien (
    MaNhanVien INT IDENTITY(1,1) PRIMARY KEY,
    HoTen NVARCHAR(100) NOT NULL,
    MaPhongBan INT NOT NULL,
    MaChucVu INT NOT NULL,
    TenDangNhap VARCHAR(50) UNIQUE NOT NULL, 
    MatKhauHash VARCHAR(255) NOT NULL,
    Email VARCHAR(100) NULL,
    SoDienThoai VARCHAR(20) NULL,
    TrangThaiHoatDong BIT DEFAULT 1,
    FOREIGN KEY (MaPhongBan) REFERENCES PhongBan(MaPhongBan),
    FOREIGN KEY (MaChucVu) REFERENCES ChucVu(MaChucVu)
);

ALTER TABLE PhongBan
ADD CONSTRAINT FK_PhongBan_TruongPhong 
FOREIGN KEY (MaTruongPhong) REFERENCES NhanVien(MaNhanVien);

-- Ngân sách phòng ban
CREATE TABLE NganSach (
    MaNganSach INT IDENTITY(1,1) PRIMARY KEY,
    MaPhongBan INT NOT NULL,
    NamTaiChinh INT NOT NULL, 
    Thang INT NULL,       -- [UPDATE] Hỗ trợ Chốt Sổ đệm cuốn chiếu tháng
    TongNganSach DECIMAL(18,2) NOT NULL DEFAULT 0,
    DaChi DECIMAL(18,2) NOT NULL DEFAULT 0,
    DaThu DECIMAL(18,2) NOT NULL DEFAULT 0,     -- [UPDATE] Thêm túi đựng tiền THU NỘI BỘ
    TienDangTreo DECIMAL(18,2) NOT NULL DEFAULT 0, 
    FOREIGN KEY (MaPhongBan) REFERENCES PhongBan(MaPhongBan)
);

-- =============================================
-- 2. CỤM NGHIỆP VỤ THU CHI (TRANSACTIONS)
-- =============================================

-- Phiếu đề xuất
CREATE TABLE PhieuDeXuat (
    MaPhieu INT IDENTITY(1,1) PRIMARY KEY,
    NguoiTao INT NOT NULL,
    MaPhongBan INT NOT NULL,
    LoaiPhieu NVARCHAR(50) NOT NULL, -- 'TamUng', 'HoanUng', 'ThanhToan', 'ThuNoiBo'
    TongTien DECIMAL(18,2) NOT NULL DEFAULT 0,
    LyDo NVARCHAR(MAX),
    TrangThai NVARCHAR(50) NOT NULL DEFAULT 'Nhap', 
    NgayTao DATETIME DEFAULT GETDATE(),
    MaPhieuGoc INT NULL, 
    BuocDuyetHienTai INT DEFAULT 1,
    FOREIGN KEY (NguoiTao) REFERENCES NhanVien(MaNhanVien),
    FOREIGN KEY (MaPhongBan) REFERENCES PhongBan(MaPhongBan),
    FOREIGN KEY (MaPhieuGoc) REFERENCES PhieuDeXuat(MaPhieu)
);

-- Chi tiết phiếu
CREATE TABLE ChiTietPhieu (
    MaChiTiet INT IDENTITY(1,1) PRIMARY KEY,
    MaPhieu INT NOT NULL,
    HangMuc NVARCHAR(255) NOT NULL,
    SoTien DECIMAL(18,2) NOT NULL,
    GhiChu NVARCHAR(500),
    FOREIGN KEY (MaPhieu) REFERENCES PhieuDeXuat(MaPhieu) ON DELETE CASCADE
);

-- Chứng từ đính kèm
CREATE TABLE ChungTuDinhKem (
    MaChungTu INT IDENTITY(1,1) PRIMARY KEY,
    MaPhieu INT NOT NULL,
    TenFile NVARCHAR(255) NOT NULL,
    DuongDan NVARCHAR(MAX) NOT NULL, 
    LoaiFile NVARCHAR(50), 
    NgayTaiLen DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (MaPhieu) REFERENCES PhieuDeXuat(MaPhieu) ON DELETE CASCADE
);

-- =============================================
-- 3. CỤM CẤU HÌNH LUỒNG DUYỆT (WORKFLOW ENGINE)
-- =============================================

-- Cấu hình luồng duyệt
CREATE TABLE CauHinhLuongDuyet (
    MaCauHinh INT IDENTITY(1,1) PRIMARY KEY,
    LoaiPhieu NVARCHAR(50) NOT NULL,
    TienToiThieu DECIMAL(18,2) NOT NULL DEFAULT 0,
    TienToiDa DECIMAL(18,2) NOT NULL,
    TongSoBuoc INT NOT NULL
);

-- Chi tiết luồng duyệt
CREATE TABLE ChiTietLuongDuyet (
    MaChiTietLuong INT IDENTITY(1,1) PRIMARY KEY,
    MaCauHinh INT NOT NULL,
    ThuTuBuoc INT NOT NULL,
    MaChucVuDuyet INT NOT NULL,
    MaPhongBanDuyet INT NULL,
    FOREIGN KEY (MaCauHinh) REFERENCES CauHinhLuongDuyet(MaCauHinh) ON DELETE CASCADE,
    FOREIGN KEY (MaChucVuDuyet) REFERENCES ChucVu(MaChucVu),
    FOREIGN KEY (MaPhongBanDuyet) REFERENCES PhongBan(MaPhongBan)
);

-- =============================================
-- 4. CỤM DẤU VẾT (AUDIT LOG)
-- =============================================

-- Nhật ký phê duyệt
CREATE TABLE NhatKyDuyet (
    MaNhatKy INT IDENTITY(1,1) PRIMARY KEY,
    MaPhieu INT NOT NULL,
    NguoiXuLy INT NOT NULL,
    HanhDong NVARCHAR(50) NOT NULL, 
    GhiChu NVARCHAR(MAX), 
    ThoiGian DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (MaPhieu) REFERENCES PhieuDeXuat(MaPhieu),
    FOREIGN KEY (NguoiXuLy) REFERENCES NhanVien(MaNhanVien)
);
GO


CREATE TABLE ThongBao (
    MaThongBao INT IDENTITY(1,1) PRIMARY KEY,
    NguoiNhan INT NOT NULL,
    NoiDung NVARCHAR(500) NOT NULL,
    Url NVARCHAR(255),
    ThoiGian DATETIME DEFAULT GETDATE(),
    DaDoc BIT NOT NULL DEFAULT 0,
    FOREIGN KEY (NguoiNhan) REFERENCES NhanVien(MaNhanVien)
);
GO

-- =============================================
-- 5. BƠM DỮ LIỆU MẪU (SEED DATA)
-- =============================================

-- 1. Giám đốc
-- 2. Kế toán trưởng
-- 3. Trưởng phòng Kinh doanh
-- 4. Nhân viên KD (Nhóm 7)
-- 5. Nhân viên KD 2
-- 6. Trưởng phòng IT
-- 7. Nhân viên IT
-- 8. Trưởng phòng HCNS
-- 9. Nhân viên HCNS
-- 10. Nhân viên Kế toán

-- 5.1. Chức vụ & Phòng ban
INSERT INTO ChucVu (TenChucVu) VALUES 
(N'Giám đốc'), (N'Kế toán trưởng'), (N'Trưởng phòng'), (N'Nhân viên');

INSERT INTO PhongBan (TenPhongBan) VALUES 
(N'Ban Giám đốc'), (N'Phòng Kế toán'), (N'Phòng Kinh doanh'), (N'Phòng IT'), (N'Phòng Hành chính - NS');

-- 5.2. Nhân viên
DECLARE @DefaultPassHash VARCHAR(255) = '$2a$10$EixZaYVK1fsbw1ZfbX3OXePaWxn96p36WQoeG6Lruj3vjGoRX.vO2';

INSERT INTO NhanVien (HoTen, MaPhongBan, MaChucVu, TenDangNhap, MatKhauHash, Email) VALUES 
(N'Lê Duy An', 1, 1, 'anld', @DefaultPassHash, 'anld@dainam.edu.vn'), 
(N'Nguyễn Thế Phương Nam', 2, 2, 'namntp', @DefaultPassHash, 'namntp@dainam.edu.vn'), 
(N'Đỗ Duy Văn', 3, 3, 'vandd', @DefaultPassHash, 'vandd@dainam.edu.vn'),  
(N'Lê Văn Sếp', 3, 4, 'seplv', @DefaultPassHash, 'seplv@congty.com'),  
(N'Trần Thị Sale', 3, 4, 'salett', @DefaultPassHash, 'salett@congty.com'),  
(N'Ông Kẹ IT', 4, 3, 'keit', @DefaultPassHash, 'keit@congty.com'),  
(N'Phạm Coder', 4, 4, 'coderp', @DefaultPassHash, 'coderp@congty.com'),  
(N'Nguyễn Mẫu Mực', 5, 3, 'mucnm', @DefaultPassHash, 'mucnm@congty.com'), 
(N'Trần Tuyển Dụng', 5, 4, 'dungtt', @DefaultPassHash, 'dungtt@congty.com'), 
(N'Lê Kế Toán', 2, 4, 'toanlk', @DefaultPassHash, 'toanlk@congty.com');

UPDATE PhongBan SET MaTruongPhong = 1 WHERE MaPhongBan = 1;
UPDATE PhongBan SET MaTruongPhong = 2 WHERE MaPhongBan = 2;
UPDATE PhongBan SET MaTruongPhong = 3 WHERE MaPhongBan = 3;
UPDATE PhongBan SET MaTruongPhong = 6 WHERE MaPhongBan = 4;
UPDATE PhongBan SET MaTruongPhong = 8 WHERE MaPhongBan = 5;

-- 5.3. Cấp ngân sách
INSERT INTO NganSach (MaPhongBan, NamTaiChinh, TongNganSach, DaChi, DaThu, TienDangTreo) VALUES 
(2, 2026, 20000000, 0, 0, 0),        
(3, 2026, 100000000, 5000000, 0, 0),
(4, 2026, 200000000, 0, 0, 45000000),
(5, 2026, 50000000, 0, 0, 0);        

-- 5.4. Setup Luồng duyệt (Workflow Engine)
-- ID 1, 2: <= 10tr (Chỉ Kế toán trưởng)
INSERT INTO CauHinhLuongDuyet (LoaiPhieu, TienToiThieu, TienToiDa, TongSoBuoc) 
VALUES (N'TamUng', 0, 10000000, 1), (N'ThanhToan', 0, 10000000, 1);
INSERT INTO ChiTietLuongDuyet (MaCauHinh, ThuTuBuoc, MaChucVuDuyet) VALUES (1, 1, 2), (2, 1, 2);

-- ID 3, 4: > 10tr (Kế toán trưởng -> Giám đốc)
INSERT INTO CauHinhLuongDuyet (LoaiPhieu, TienToiThieu, TienToiDa, TongSoBuoc) 
VALUES (N'TamUng', 10000000.01, 999999999, 2), (N'ThanhToan', 10000000.01, 999999999, 2);
INSERT INTO ChiTietLuongDuyet (MaCauHinh, ThuTuBuoc, MaChucVuDuyet) VALUES (3, 1, 2), (3, 2, 1), (4, 1, 2), (4, 2, 1);

-- [UPDATE] ID 5: Luồng duyệt PHIẾU THU NỘI BỘ (Chỉ 1 bước cho Kế toán trưởng xác nhận tiền vào)
INSERT INTO CauHinhLuongDuyet (LoaiPhieu, TienToiThieu, TienToiDa, TongSoBuoc) 
VALUES (N'ThuNoiBo', 0, 9999999999, 1);
INSERT INTO ChiTietLuongDuyet (MaCauHinh, ThuTuBuoc, MaChucVuDuyet) 
VALUES (5, 1, 2);

-- 5.5. Đổ dữ liệu Test Phiếu Đề Xuất
-- Phiếu 1, 2, 3, 4 giữ nguyên
INSERT INTO PhieuDeXuat (NguoiTao, MaPhongBan, LoaiPhieu, TongTien, LyDo, TrangThai, BuocDuyetHienTai)
VALUES (4, 3, N'TamUng', 5000000, N'Tạm ứng đi công tác Đà Nẵng', N'DaChiTien', 1);
INSERT INTO ChiTietPhieu (MaPhieu, HangMuc, SoTien, GhiChu) VALUES 
(1, N'Vé máy bay', 3000000, N'Khứ hồi'), (1, N'Khách sạn', 2000000, N'2 đêm');
INSERT INTO NhatKyDuyet (MaPhieu, NguoiXuLy, HanhDong, GhiChu) VALUES (1, 2, N'Duyet', N'Ok, thủ quỹ chi tiền nhé.');

INSERT INTO PhieuDeXuat (NguoiTao, MaPhongBan, LoaiPhieu, TongTien, LyDo, TrangThai, MaPhieuGoc, BuocDuyetHienTai)
VALUES (4, 3, N'HoanUng', 4500000, N'Hoàn ứng chuyến đi Đà Nẵng, xin nộp lại 500k', N'DaDuyet', 1, 1);
INSERT INTO ChiTietPhieu (MaPhieu, HangMuc, SoTien, GhiChu) VALUES 
(2, N'Vé máy bay', 3000000, N'Kèm hóa đơn VAT'), (2, N'Khách sạn', 1500000, N'Chỉ thuê 1 đêm, kèm VAT');

INSERT INTO PhieuDeXuat (NguoiTao, MaPhongBan, LoaiPhieu, TongTien, LyDo, TrangThai, BuocDuyetHienTai)
VALUES (7, 4, N'ThanhToan', 45000000, N'Thanh toán đợt 1 hợp đồng mua máy chủ Dell', N'ChoDuyet', 2);
INSERT INTO ChiTietPhieu (MaPhieu, HangMuc, SoTien, GhiChu) VALUES 
(3, N'Máy chủ Dell PowerEdge', 45000000, N'HĐ số 01/2026');
INSERT INTO NhatKyDuyet (MaPhieu, NguoiXuLy, HanhDong, GhiChu) VALUES (3, 2, N'Duyet', N'Đã check hợp đồng hợp lệ, kính chuyển Sếp An duyệt chi.');

INSERT INTO PhieuDeXuat (NguoiTao, MaPhongBan, LoaiPhieu, TongTien, LyDo, TrangThai, BuocDuyetHienTai)
VALUES (9, 5, N'TamUng', 2000000, N'Tạm ứng mua hoa sinh nhật sếp', N'TuChoi', 1);
INSERT INTO ChiTietPhieu (MaPhieu, HangMuc, SoTien) VALUES (4, N'Hoa sinh nhật', 2000000);
INSERT INTO NhatKyDuyet (MaPhieu, NguoiXuLy, HanhDong, GhiChu) VALUES (4, 2, N'TuChoi', N'Quỹ công đoàn không chi này.');

-- [UPDATE] Phiếu 5: Phiếu Thu Nội Bộ (Tiền phạt/Hoàn trả)
INSERT INTO PhieuDeXuat (NguoiTao, MaPhongBan, LoaiPhieu, TongTien, LyDo, TrangThai, BuocDuyetHienTai)
VALUES (7, 4, N'ThuNoiBo', 1500000, N'Nộp tiền phạt đi muộn tháng 5 của Phòng IT', N'ChoDuyet', 1);
INSERT INTO ChiTietPhieu (MaPhieu, HangMuc, SoTien, GhiChu) VALUES 
(5, N'Quỹ phạt đi muộn', 1500000, N'Phạm Coder nộp tiền mặt');
GO




SELECT MaNhanVien, HoTen, TenDangNhap FROM NhanVien
-- 1. Giám đốc
-- 2. Kế toán trưởng
-- 3. Trưởng phòng Kinh doanh
-- 4. Nhân viên KD (Nhóm 7)
-- 5. Nhân viên KD 2
-- 6. Trưởng phòng IT
-- 7. Nhân viên IT
-- 8. Trưởng phòng HCNS
-- 9. Nhân viên HCNS
-- 10. Nhân viên Kế toán
