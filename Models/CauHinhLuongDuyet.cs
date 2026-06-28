using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QL_ThuChiNoiBo.Models;

[Table("CauHinhLuongDuyet")]
public partial class CauHinhLuongDuyet
{
    [Key]
    public int MaCauHinh { get; set; }

    [StringLength(50)]
    public string LoaiPhieu { get; set; } = null!;

    [Column(TypeName = "decimal(18, 2)")]
    public decimal TienToiThieu { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal TienToiDa { get; set; }

    public int TongSoBuoc { get; set; }

    [InverseProperty("MaCauHinhNavigation")]
    public virtual ICollection<ChiTietLuongDuyet> ChiTietLuongDuyets { get; set; } = new List<ChiTietLuongDuyet>();
}
