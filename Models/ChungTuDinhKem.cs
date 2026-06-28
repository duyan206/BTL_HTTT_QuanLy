using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QL_ThuChiNoiBo.Models;

[Table("ChungTuDinhKem")]
public partial class ChungTuDinhKem
{
    [Key]
    public int MaChungTu { get; set; }

    public int MaPhieu { get; set; }

    [StringLength(255)]
    public string TenFile { get; set; } = null!;

    public string DuongDan { get; set; } = null!;

    [StringLength(50)]
    public string? LoaiFile { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? NgayTaiLen { get; set; }

    [ForeignKey("MaPhieu")]
    [InverseProperty("ChungTuDinhKems")]
    public virtual PhieuDeXuat MaPhieuNavigation { get; set; } = null!;
}
