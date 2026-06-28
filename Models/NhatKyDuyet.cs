using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QL_ThuChiNoiBo.Models;

[Table("NhatKyDuyet")]
public partial class NhatKyDuyet
{
    [Key]
    public int MaNhatKy { get; set; }

    public int MaPhieu { get; set; }

    public int NguoiXuLy { get; set; }

    [StringLength(50)]
    public string HanhDong { get; set; } = null!;

    public string? GhiChu { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? ThoiGian { get; set; }

    [ForeignKey("MaPhieu")]
    [InverseProperty("NhatKyDuyets")]
    public virtual PhieuDeXuat MaPhieuNavigation { get; set; } = null!;

    [ForeignKey("NguoiXuLy")]
    [InverseProperty("NhatKyDuyets")]
    public virtual NhanVien NguoiXuLyNavigation { get; set; } = null!;
}
