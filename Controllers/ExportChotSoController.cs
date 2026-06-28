using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using OfficeOpenXml.Drawing.Chart;
using QL_ThuChiNoiBo.Data;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QL_ThuChiNoiBo.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace QL_ThuChiNoiBo.Controllers
{
    [Authorize]
    public class ExportChotSoController : Controller
    {
        private readonly QlThuChiNoiBoContext _context;

        public ExportChotSoController(QlThuChiNoiBoContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> XuatBaoCaoThang(int month, int year)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            
            var nganSaches = await _context.NganSaches
                .Include(n => n.MaPhongBanNavigation)
                .Where(n => n.Thang == month && n.NamTaiChinh == year)
                .ToListAsync();

            var phieuDeXuats = await _context.PhieuDeXuats
                .Include(p => p.NguoiTaoNavigation)
                .Include(p => p.MaPhongBanNavigation)
                .Where(p => p.NgayTao.Value.Month == month && p.NgayTao.Value.Year == year)
                .ToListAsync();

            using var package = new ExcelPackage();
            
            // Sheet 1: Dữ liệu tổng quan
            var ws1 = package.Workbook.Worksheets.Add("Tong_Quan");
            ws1.Cells[1, 1].Value = "Phòng Ban";
            ws1.Cells[1, 2].Value = "Ngân Sách Cấp Nội Bộ (Kỳ đầu)";
            ws1.Cells[1, 3].Value = "Tổng Đã Thu Vào";
            ws1.Cells[1, 4].Value = "Tổng Đã Chi Ra";
            ws1.Cells[1, 5].Value = "Tiền Đang Treo Chờ Duyệt";
            ws1.Cells[1, 6].Value = "Khấu Dư Nguồn Còn Lại";
            
            int r1 = 2;
            foreach(var ns in nganSaches) {
                ws1.Cells[r1, 1].Value = ns.MaPhongBanNavigation.TenPhongBan;
                ws1.Cells[r1, 2].Value = ns.TongNganSach;
                ws1.Cells[r1, 3].Value = ns.DaThu;
                ws1.Cells[r1, 4].Value = ns.DaChi;
                ws1.Cells[r1, 5].Value = ns.TienDangTreo;
                ws1.Cells[r1, 6].Value = ns.TongNganSach + ns.DaThu - ns.DaChi - ns.TienDangTreo;
                r1++;
            }
            if (r1 > 2) {
                ws1.Cells[$"B2:F{r1-1}"].Style.Numberformat.Format = "#,##0";
            }
            ws1.Cells[$"A1:F1"].Style.Font.Bold = true;
            ws1.Cells.AutoFitColumns();

            // Sheet 2: Sổ Chi Tiết Giao Dịch
            var ws2 = package.Workbook.Worksheets.Add("So_Chi_Tiet");
            ws2.Cells[1, 1].Value = "Mã Phiếu";
            ws2.Cells[1, 2].Value = "Ngày Giao Dịch";
            ws2.Cells[1, 3].Value = "Người Thực Hiện";
            ws2.Cells[1, 4].Value = "Phòng Ban";
            ws2.Cells[1, 5].Value = "Loại Giao Dịch";
            ws2.Cells[1, 6].Value = "Số Tiền (VNĐ)";
            ws2.Cells[1, 7].Value = "Lý Do / Ghi Chú";
            
            int r2 = 2;
            foreach(var p in phieuDeXuats) {
                ws2.Cells[r2, 1].Value = p.MaPhieu;
                ws2.Cells[r2, 2].Value = p.NgayTao.Value.ToString("dd/MM/yyyy");
                ws2.Cells[r2, 3].Value = p.NguoiTaoNavigation?.HoTen;
                ws2.Cells[r2, 4].Value = p.MaPhongBanNavigation?.TenPhongBan;
                
                string loaiDisplay = p.LoaiPhieu;
                if (p.LoaiPhieu == "TamUng") loaiDisplay = "Rút Tạm Ứng";
                else if (p.LoaiPhieu == "ThuNoiBo") loaiDisplay = "Nộp Thu Nội Bộ";
                else if (p.LoaiPhieu == "ThanhToan") loaiDisplay = "Thanh Toán";
                
                ws2.Cells[r2, 5].Value = loaiDisplay;
                ws2.Cells[r2, 6].Value = p.TongTien;
                ws2.Cells[r2, 7].Value = p.LyDo;
                r2++;
            }
            if (r2 > 2) {
                ws2.Cells[$"F2:F{r2-1}"].Style.Numberformat.Format = "#,##0";
                var borderRanges = ws2.Cells[$"A1:G{r2-1}"];
                borderRanges.Style.Border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                borderRanges.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                borderRanges.Style.Border.Left.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                borderRanges.Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
            }
            ws2.Cells[$"A1:G1"].Style.Font.Bold = true;
            ws2.Cells.AutoFitColumns();

            // Sheet 3: Dashboard Charts (EPPlus Chart Engine)
            var ws3 = package.Workbook.Worksheets.Add("Bieu_Do_Thong_Ke");
            
            var sumDaChi = nganSaches.Sum(x => x.DaChi);
            var sumSoDu = nganSaches.Sum(x => x.TongNganSach + x.DaThu - x.DaChi - x.TienDangTreo);
            
            ws3.Cells["A20"].Value = "Đã Chi Toàn Công Ty"; ws3.Cells["B20"].Value = sumDaChi;
            ws3.Cells["A21"].Value = "Sĩ Số Lượng Nguồn Dư Bỏ Túi"; ws3.Cells["B21"].Value = sumSoDu;
            
            var pieChart = ws3.Drawings.AddPieChart("PieChart", ePieChartType.Pie);
            pieChart.Title.Text = "Cơ Cấu Quỹ - Rót Dòng Tiền (VNĐ)";
            pieChart.SetPosition(1, 0, 1, 0);
            pieChart.SetSize(400, 400);
            var seriesPie = pieChart.Series.Add(ws3.Cells["B20:B21"], ws3.Cells["A20:A21"]);
            
            if (nganSaches.Any() && r1 > 2)
            {
                var barChart = ws3.Drawings.AddBarChart("barChart", eBarChartType.ColumnClustered);
                barChart.Title.Text = "Phân Khúc Tiêu Hao & Trữ Ngân Sách Giữa Các Phòng Ban";
                barChart.SetPosition(1, 0, 8, 0);
                barChart.SetSize(600, 400);
                
                var rngNames = ws1.Cells[$"A2:A{r1-1}"];
                var rngTongNganSach = ws1.Cells[$"B2:B{r1-1}"];
                var rngDaChi = ws1.Cells[$"D2:D{r1-1}"];
                
                var s1 = barChart.Series.Add(rngTongNganSach, rngNames);
                s1.Header = "Ngân Mục Giao Phát Mở Cửa (VND)";
                var s2 = barChart.Series.Add(rngDaChi, rngNames);
                s2.Header = "Ngân Mục Đã Cứ Cắt Hao Hụt (VND)";
            }
            
            var stream = new System.IO.MemoryStream();
            await package.SaveAsAsync(stream);
            var content = stream.ToArray();
            
            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"BaoCaoVaChotSo_Thang{month}_{year}.xlsx");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XacNhanChotSo(int month, int year)
        {
            var role = User.FindFirstValue(ClaimTypes.Role) ?? "";
            if (role != "Kế toán trưởng" && role != "Giám đốc")
            {
                TempData["ErrorMessage"] = "Bạn không có đặc quyền chốt sổ tài chính!";
                return RedirectToAction("Index", "Home");
            }

            int nextMonth = month + 1;
            int nextYear = year;
            if (nextMonth > 12)
            {
                nextMonth = 1;
                nextYear++;
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. PhieuDeXuat -> Đã Chốt Sổ
                var phieus = await _context.PhieuDeXuats
                    .Where(p => p.NgayTao.Value.Month == month && p.NgayTao.Value.Year == year && p.TrangThai == "Đã duyệt")
                    .ToListAsync();
                foreach(var p in phieus)
                {
                    p.TrangThai = "Đã Chốt Sổ";
                }

                
                                
                // 2. NganSach -> Next Month Creation (Reset 0 & Keep Original Budget)
                var nganSaches = await _context.NganSaches
                    .Where(n => n.Thang == month && n.NamTaiChinh == year)
                    .ToListAsync();

                foreach(var ns in nganSaches)
                {
                    // "Cột TongNganSach giữ nguyên định mức ban đầu, DaThu, DaChi, DangTreo về 0"
                    var exists = await _context.NganSaches.AnyAsync(n => n.Thang == nextMonth && n.NamTaiChinh == nextYear && n.MaPhongBan == ns.MaPhongBan);
                    if (!exists)
                    {
                        var newNs = new NganSach
                        {
                            MaPhongBan = ns.MaPhongBan,
                            Thang = nextMonth,
                            NamTaiChinh = nextYear,
                            TongNganSach = ns.TongNganSach, // Giữ nguyên định mức phân bổ 
                            DaChi = 0,
                            DaThu = 0,
                            TienDangTreo = 0
                        };
                        _context.NganSaches.Add(newNs);
                    }
                }
                
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Trả về JSON để Frontend tự động nổ file tải xuống
                return Json(new { success = true, month = month, year = year, message = $"[ĐÃ CHỐT SỔ ĐÓNG GÓI] Tháng {month}/{year}. Dữ liệu các cột chi tiêu đã được cắt băng về 0 cho dòng đời tháng {nextMonth}/{nextYear}." });
            }
            catch(Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = "Lỗi khi chốt sổ: " + ex.Message });
            }


        }
    }
}






