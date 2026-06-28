using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QL_ThuChiNoiBo.Models
{
    [Table("ThongBao")]
    public class ThongBao
    {
        [Key]
        public int MaThongBao { get; set; }
        public int NguoiNhan { get; set; }
        [Required]
        [StringLength(500)]
        public string NoiDung { get; set; } = null!;
        public DateTime ThoiGian { get; set; } = DateTime.Now;
        public bool DaDoc { get; set; } = false;
        [StringLength(255)]
        public string? Url { get; set; }

        [ForeignKey("NguoiNhan")]
        public virtual NhanVien? NhanVienNavigation { get; set; }
    }
}
