using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QL_ThuChiNoiBo.Models;

[Table("ChucVu")]
public partial class ChucVu
{
    [Key]
    public int MaChucVu { get; set; }

    [StringLength(100)]
    public string TenChucVu { get; set; } = null!;

    public bool? TrangThaiHoatDong { get; set; }

    [InverseProperty("MaChucVuDuyetNavigation")]
    public virtual ICollection<ChiTietLuongDuyet> ChiTietLuongDuyets { get; set; } = new List<ChiTietLuongDuyet>();

    [InverseProperty("MaChucVuNavigation")]
    public virtual ICollection<NhanVien> NhanViens { get; set; } = new List<NhanVien>();
}
