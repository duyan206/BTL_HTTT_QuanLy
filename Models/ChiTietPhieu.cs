using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QL_ThuChiNoiBo.Models;

[Table("ChiTietPhieu")]
public partial class ChiTietPhieu
{
    [Key]
    public int MaChiTiet { get; set; }

    public int MaPhieu { get; set; }

    [StringLength(255)]
    public string HangMuc { get; set; } = null!;

    [Column(TypeName = "decimal(18, 2)")]
    public decimal SoTien { get; set; }

    [StringLength(500)]
    public string? GhiChu { get; set; }

    [ForeignKey("MaPhieu")]
    [InverseProperty("ChiTietPhieus")]
    public virtual PhieuDeXuat MaPhieuNavigation { get; set; } = null!;
}
