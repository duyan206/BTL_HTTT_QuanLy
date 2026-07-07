using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QL_ThuChiNoiBo.Data;
using System.Security.Claims;
using System;
using System.Linq;
using System.Threading.Tasks;
using QL_ThuChiNoiBo.Constants;

namespace QL_ThuChiNoiBo.Areas.Director.Controllers
{
    [Area("Director")]
    [Authorize(Roles = RoleConstants.Director)]
    public class NganSachController : Controller
    {
        private readonly QlThuChiNoiBoContext _context;

        public NganSachController(QlThuChiNoiBoContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var role = User.FindFirstValue(System.Security.Claims.ClaimTypes.Role) ?? "";
            var allowedRoles = new[] { RoleConstants.Director };
            if (!allowedRoles.Contains(role))
            {
                TempData["Error"] = "Lỗ hổng RBAC bị chặn: Nhân viên tuyệt đối không được cấp phép truy cập Quản trị Ngân Sách!";
                return RedirectToAction("Index", "PhieuDeXuat");
            }

            var month = DateTime.Now.Month; var year = DateTime.Now.Year;
            var nganSaches = await _context.NganSaches
                .Include(n => n.MaPhongBanNavigation)
                .Where(n => n.Thang == month && n.NamTaiChinh == year)
                .ToListAsync();

            // Tự động Add thêm những phòng ban rỗng chưa có bản ghi ngân sách trong database nếu bị sót
            var allDepartments = await _context.PhongBans.ToListAsync();
            foreach (var dept in allDepartments)
            {
                if (!nganSaches.Any(n => n.MaPhongBan == dept.MaPhongBan))
                {
                    var newNganSach = new Models.NganSach
                    {
                        MaPhongBan = dept.MaPhongBan,
                        Thang = month, NamTaiChinh = year, TongNganSach = 0,
                        DaChi = 0,
                        TienDangTreo = 0
                    };
                    _context.NganSaches.Add(newNganSach);
                    nganSaches.Add(newNganSach);
                }
            }
            if (_context.ChangeTracker.HasChanges())
            {
                await _context.SaveChangesAsync();
            }

            return View(nganSaches.OrderBy(n => n.MaPhongBanNavigation?.TenPhongBan).ToList());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateBudget(int maPhongBan, decimal tongNganSachMoi)
        {
            var role = User.FindFirstValue(System.Security.Claims.ClaimTypes.Role) ?? "";
            var allowedRoles = new[] { RoleConstants.Director };
            if (!allowedRoles.Contains(role))
            {
                TempData["Error"] = "Cảnh báo bảo mật: Bạn không có chức năng Cấp phát ngân sách!";
                return RedirectToAction("Index", "PhieuDeXuat");
            }

            if (tongNganSachMoi < 0)
            {
                TempData["Error"] = "Thất bại: Tổng ngân sách mới không hợp lệ (Phải lớn hơn hoặc bằng 0).";
                return RedirectToAction(nameof(Index));
            }

            var month = DateTime.Now.Month; var year = DateTime.Now.Year;
            var nganSach = await _context.NganSaches
                .Include(n => n.MaPhongBanNavigation)
                .FirstOrDefaultAsync(n => n.MaPhongBan == maPhongBan && n.Thang == DateTime.Now.Month && n.NamTaiChinh == year);

            if (nganSach == null)
            {
                TempData["Error"] = "Lỗi hệ thống: Không tìm thấy dữ liệu ngân sách của phòng ban này rớt lại năm " + year;
                return RedirectToAction(nameof(Index));
            }

            var soTienDaSuDung = nganSach.DaChi + nganSach.TienDangTreo;
            if (tongNganSachMoi < soTienDaSuDung)
            {
                TempData["Error"] = $"Cập nhật thất bại, phát hiện Logic lỗi trầm trọng! Ngân sách mới không được phép thấp hơn tổng số tiền phòng này đã sử dụng (Đã chi + Đang treo = {soTienDaSuDung:N0} VNĐ). Hãy Cấp phát nhiều hơn số đó!";
                return RedirectToAction(nameof(Index));
            }

            nganSach.TongNganSach = tongNganSachMoi;
            _context.NganSaches.Update(nganSach);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Tuyệt vời! Đã cập nhật khoản cấp phát ngân sách siêu tốc cho phòng ban [{nganSach.MaPhongBanNavigation?.TenPhongBan}] thành công!";
            return RedirectToAction(nameof(Index));
        }
    }
}


