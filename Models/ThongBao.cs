using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QL_ThuChiNoiBo.Models;

[Table("ThongBao")]
public partial class ThongBao
{
    [Key]
    public int MaThongBao { get; set; }

    public int NguoiNhan { get; set; }

    [StringLength(500)]
    public string NoiDung { get; set; } = null!;

    [StringLength(255)]
    public string? Url { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? ThoiGian { get; set; }

    public bool DaDoc { get; set; }

    [ForeignKey("NguoiNhan")]
    [InverseProperty("ThongBaos")]
    public virtual NhanVien NguoiNhanNavigation { get; set; } = null!;
}
