using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QL_ThuChiNoiBo.Models;

[Table("PhongBan")]
public partial class PhongBan
{
    [Key]
    public int MaPhongBan { get; set; }

    [StringLength(150)]
    public string TenPhongBan { get; set; } = null!;

    public int? MaTruongPhong { get; set; }

    public bool? TrangThaiHoatDong { get; set; }

    [InverseProperty("MaPhongBanDuyetNavigation")]
    public virtual ICollection<ChiTietLuongDuyet> ChiTietLuongDuyets { get; set; } = new List<ChiTietLuongDuyet>();

    [ForeignKey("MaTruongPhong")]
    [InverseProperty("PhongBans")]
    public virtual NhanVien? MaTruongPhongNavigation { get; set; }

    [InverseProperty("MaPhongBanNavigation")]
    public virtual ICollection<NganSach> NganSaches { get; set; } = new List<NganSach>();

    [InverseProperty("MaPhongBanNavigation")]
    public virtual ICollection<NhanVien> NhanViens { get; set; } = new List<NhanVien>();

    [InverseProperty("MaPhongBanNavigation")]
    public virtual ICollection<PhieuDeXuat> PhieuDeXuats { get; set; } = new List<PhieuDeXuat>();
}
