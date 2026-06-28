using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QL_ThuChiNoiBo.Models;

[Table("PhieuDeXuat")]
public partial class PhieuDeXuat
{
    [Key]
    public int MaPhieu { get; set; }

    public int NguoiTao { get; set; }

    public int MaPhongBan { get; set; }

    [StringLength(50)]
    public string LoaiPhieu { get; set; } = null!;

    [Column(TypeName = "decimal(18, 2)")]
    public decimal TongTien { get; set; }

    public string? LyDo { get; set; }

    [StringLength(50)]
    public string TrangThai { get; set; } = null!;

    [Column(TypeName = "datetime")]
    public DateTime? NgayTao { get; set; }

    public int? MaPhieuGoc { get; set; }

    public int? BuocDuyetHienTai { get; set; }

    [InverseProperty("MaPhieuNavigation")]
    public virtual ICollection<ChiTietPhieu> ChiTietPhieus { get; set; } = new List<ChiTietPhieu>();

    [InverseProperty("MaPhieuNavigation")]
    public virtual ICollection<ChungTuDinhKem> ChungTuDinhKems { get; set; } = new List<ChungTuDinhKem>();

    [InverseProperty("MaPhieuGocNavigation")]
    public virtual ICollection<PhieuDeXuat> InverseMaPhieuGocNavigation { get; set; } = new List<PhieuDeXuat>();

    [ForeignKey("MaPhieuGoc")]
    [InverseProperty("InverseMaPhieuGocNavigation")]
    public virtual PhieuDeXuat? MaPhieuGocNavigation { get; set; }

    [ForeignKey("MaPhongBan")]
    [InverseProperty("PhieuDeXuats")]
    public virtual PhongBan MaPhongBanNavigation { get; set; } = null!;

    [ForeignKey("NguoiTao")]
    [InverseProperty("PhieuDeXuats")]
    public virtual NhanVien NguoiTaoNavigation { get; set; } = null!;

    [InverseProperty("MaPhieuNavigation")]
    public virtual ICollection<NhatKyDuyet> NhatKyDuyets { get; set; } = new List<NhatKyDuyet>();
}
