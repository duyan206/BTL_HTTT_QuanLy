using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QL_ThuChiNoiBo.Models;

[Table("ChiTietLuongDuyet")]
public partial class ChiTietLuongDuyet
{
    [Key]
    public int MaChiTietLuong { get; set; }

    public int MaCauHinh { get; set; }

    public int ThuTuBuoc { get; set; }

    public int MaChucVuDuyet { get; set; }

    public int? MaPhongBanDuyet { get; set; }

    [ForeignKey("MaCauHinh")]
    [InverseProperty("ChiTietLuongDuyets")]
    public virtual CauHinhLuongDuyet MaCauHinhNavigation { get; set; } = null!;

    [ForeignKey("MaChucVuDuyet")]
    [InverseProperty("ChiTietLuongDuyets")]
    public virtual ChucVu MaChucVuDuyetNavigation { get; set; } = null!;

    [ForeignKey("MaPhongBanDuyet")]
    [InverseProperty("ChiTietLuongDuyets")]
    public virtual PhongBan? MaPhongBanDuyetNavigation { get; set; }
}
