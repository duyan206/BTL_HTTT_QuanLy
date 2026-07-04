using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QL_ThuChiNoiBo.Models;

[Table("NganSach")]
public partial class NganSach
{
    [Key]
    public int MaNganSach { get; set; }

    public int MaPhongBan { get; set; }

    public int NamTaiChinh { get; set; }

    public int? Thang { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal TongNganSach { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal DaChi { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal DaThu { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal TienDangTreo { get; set; }

    [ForeignKey("MaPhongBan")]
    [InverseProperty("NganSaches")]
    public virtual PhongBan MaPhongBanNavigation { get; set; } = null!;
}
