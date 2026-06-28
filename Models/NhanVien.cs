using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QL_ThuChiNoiBo.Models;

[Table("NhanVien")]
[Index("TenDangNhap", Name = "UQ__NhanVien__55F68FC04840683B", IsUnique = true)]
public partial class NhanVien
{
    [Key]
    public int MaNhanVien { get; set; }

    [StringLength(100)]
    public string HoTen { get; set; } = null!;

    public int MaPhongBan { get; set; }

    public int MaChucVu { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string TenDangNhap { get; set; } = null!;

    [StringLength(255)]
    [Unicode(false)]
    public string MatKhauHash { get; set; } = null!;

    [StringLength(100)]
    [Unicode(false)]
    public string? Email { get; set; }

    [StringLength(20)]
    [Unicode(false)]
    public string? SoDienThoai { get; set; }

    public bool? TrangThaiHoatDong { get; set; }

    [ForeignKey("MaChucVu")]
    [InverseProperty("NhanViens")]
    public virtual ChucVu MaChucVuNavigation { get; set; } = null!;

    [ForeignKey("MaPhongBan")]
    [InverseProperty("NhanViens")]
    public virtual PhongBan MaPhongBanNavigation { get; set; } = null!;

    [InverseProperty("NguoiXuLyNavigation")]
    public virtual ICollection<NhatKyDuyet> NhatKyDuyets { get; set; } = new List<NhatKyDuyet>();

    [InverseProperty("NguoiTaoNavigation")]
    public virtual ICollection<PhieuDeXuat> PhieuDeXuats { get; set; } = new List<PhieuDeXuat>();

    [InverseProperty("MaTruongPhongNavigation")]
    public virtual ICollection<PhongBan> PhongBans { get; set; } = new List<PhongBan>();
}
