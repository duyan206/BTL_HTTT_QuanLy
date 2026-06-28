using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QL_ThuChiNoiBo.Services;
using System.Security.Claims;
using System;
using ClosedXML.Excel;
using System.Text.Json;
using System.Threading.Tasks;

namespace QL_ThuChiNoiBo.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly BudgetService _budgetService;

        public HomeController(BudgetService budgetService)
        {
            _budgetService = budgetService;
        }

        public async Task<IActionResult> Index()
        {
            var role = User.FindFirstValue(System.Security.Claims.ClaimTypes.Role) ?? "";
            var allowedRoles = new[] { "Giám đốc", "Kế toán trưởng" };
            if (!allowedRoles.Contains(role))
            {
                TempData["Error"] = "Cảnh báo bảo mật: Bạn không có quyền truy cập Dashboard phân tích dữ liệu công ty!";
                return RedirectToAction("Index", "PhieuDeXuat");
            }

            var year = DateTime.Now.Year;
            var dashboardData = await _budgetService.GetBudgetDashboardAsync(year);

            ViewBag.ChartDataJson = JsonSerializer.Serialize(dashboardData);

            return View(dashboardData);
        }

        [HttpGet]
        public async Task<IActionResult> ExportBaoCaoChiTieu()
        {
            var role = User.FindFirstValue(System.Security.Claims.ClaimTypes.Role) ?? "";
            var allowedRoles = new[] { "Giám đốc", "Kế toán trưởng" };
            if (!allowedRoles.Contains(role)) return Unauthorized();

            var year = DateTime.Now.Year;
            var dashboardData = await _budgetService.GetBudgetDashboardAsync(year);

            using (var workbook = new XLWorkbook())
            {
                var ws = workbook.Worksheets.Add($"BaoCao_QuyMoNS_{year}");
                
                ws.Cell(1, 1).Value = "Phòng Ban";
                ws.Cell(1, 2).Value = "Tổng Ngân Sách (VNĐ)";
                ws.Cell(1, 3).Value = "Đã Thu (VNĐ)";
                ws.Cell(1, 4).Value = "Đã Chi Tiêu (VNĐ)";
                ws.Cell(1, 5).Value = "Đang Treo (VNĐ)";
                ws.Cell(1, 6).Value = "Số Dư Còn Lại (VNĐ)";

                var headerRng = ws.Range("A1:F1");
                headerRng.Style.Font.Bold = true;
                headerRng.Style.Fill.BackgroundColor = XLColor.FromHtml("#4F81BD");
                headerRng.Style.Font.FontColor = XLColor.White;
                headerRng.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                int row = 2;
                foreach(var dept in dashboardData.DepartmentBudgets)
                {
                    ws.Cell(row, 1).Value = dept.TenPhongBan;
                    ws.Cell(row, 2).Value = dept.NganSach;
                    ws.Cell(row, 3).Value = dept.DaThu;
                    ws.Cell(row, 4).Value = dept.DaChi;
                    ws.Cell(row, 5).Value = dept.DangTreo;
                    ws.Cell(row, 6).Value = dept.ConLai;

                    ws.Cell(row, 2).Style.NumberFormat.Format = "#,##0";
                    ws.Cell(row, 3).Style.NumberFormat.Format = "#,-##0";
                    ws.Cell(row, 4).Style.NumberFormat.Format = "#,-##0";
                    ws.Cell(row, 5).Style.NumberFormat.Format = "#,-##0";
                    ws.Cell(row, 6).Style.NumberFormat.Format = "#,-##0";
                    row++;
                }

                ws.Columns().AdjustToContents();

                using (var stream = new System.IO.MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"BaoCaoChiTieu_{year}.xlsx");
                }
            }
        }
    }
}

