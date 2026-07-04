using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QL_ThuChiNoiBo.Data;
using QL_ThuChiNoiBo.Services;
using System.Security.Claims;

namespace QL_ThuChiNoiBo.Controllers
{
    [Authorize]
    public class DuyetPhieuController : Controller
    {
        private readonly QlThuChiNoiBoContext _context;
        private readonly WorkflowService _workflowService;

        public DuyetPhieuController(QlThuChiNoiBoContext context, WorkflowService workflowService)
        {
            _context = context;
            _workflowService = workflowService;
        }

        public async Task<IActionResult> Index(int page = 1)
        {
            var role = User.FindFirstValue(ClaimTypes.Role) ?? "";
            if (role == "Nhân viên" || string.IsNullOrEmpty(role))
            {
                TempData["ErrorMessage"] = "Cảnh báo bảo mật: Chỉ Quản lý/Trưởng phòng/Ban Giám đốc mới có thẩm quyền truy cập danh sách duyệt.";
                return RedirectToAction("Index", "PhieuDeXuat");
            }

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToAction("Login", "Auth");

            var userId = int.Parse(userIdStr);
            var user = await _context.NhanViens.FindAsync(userId);
            if(user == null) return NotFound();

            var allPhieus = await _workflowService.GetPendingRequestsAsync(user.MaNhanVien, user.MaChucVu, user.MaPhongBan);

            int pageSize = 10;
            ViewBag.TotalPages = (int)Math.Ceiling(allPhieus.Count / (double)pageSize);
            ViewBag.CurrentPage = page;

            var pagedPhieus = allPhieus.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return View(pagedPhieus);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var role = User.FindFirstValue(ClaimTypes.Role) ?? "";
            if (role == "Nhân viên" || string.IsNullOrEmpty(role)) return RedirectToAction("Login", "Auth");

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToAction("Login", "Auth");

            var userId = int.Parse(userIdStr);
            bool success = await _workflowService.ApproveAsync(id, userId);

            if(success)
                TempData["SuccessMessage"] = "Phê duyệt phiếu thành công!";
            else
                TempData["ErrorMessage"] = "Phê duyệt thất bại. Bạn không có quyền hoặc phiếu không hợp lệ.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string reason)
        {
            var role = User.FindFirstValue(ClaimTypes.Role) ?? "";
            if (role == "Nhân viên" || string.IsNullOrEmpty(role)) return RedirectToAction("Login", "Auth");

            if (string.IsNullOrWhiteSpace(reason))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập lý do từ chối.";
                return RedirectToAction("Details", "PhieuDeXuat", new { id });
            }

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToAction("Login", "Auth");

            var userId = int.Parse(userIdStr);
            bool success = await _workflowService.RejectAsync(id, userId, reason);

            if(success)
                TempData["SuccessMessage"] = "Đã từ chối phiếu duyệt!";
            else
                TempData["ErrorMessage"] = "Từ chối thất bại. Có lỗi xảy ra.";

            return RedirectToAction(nameof(Index));
        }
    }
}


