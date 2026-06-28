using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace QL_ThuChiNoiBo.ViewModels
{
    public class PhieuDeXuatViewModel
    {
        public int MaPhieu { get; set; }
        public string LoaiPhieu { get; set; } = "Tạm Ứng";
        public string? LyDo { get; set; }
        
        public List<ChiTietPhieuViewModel> ChiTiets { get; set; } = new List<ChiTietPhieuViewModel>();
        
        public IFormFileCollection? DinhKems { get; set; }
    }

    public class ChiTietPhieuViewModel
    {
        public string HangMuc { get; set; } = null!;
        public decimal SoTien { get; set; }
        public string? GhiChu { get; set; }
    }
}
