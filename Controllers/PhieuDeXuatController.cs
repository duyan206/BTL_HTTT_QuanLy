using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QL_ThuChiNoiBo.Data;
using QL_ThuChiNoiBo.Models;
using QL_ThuChiNoiBo.ViewModels;
using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using QL_ThuChiNoiBo.Services;

namespace QL_ThuChiNoiBo.Controllers
{
    [Authorize]
    public class PhieuDeXuatController : Controller
    {
        private readonly QlThuChiNoiBoContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly BudgetService _budgetService;
        private readonly WorkflowService _workflowService;

        public PhieuDeXuatController(QlThuChiNoiBoContext context, IWebHostEnvironment env, BudgetService budgetService, WorkflowService workflowService)
        {
            _context = context;
            _env = env;
            _budgetService = budgetService;
            _workflowService = workflowService;
        }

        public async Task<IActionResult> Index(int page = 1)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToAction("Login", "Auth");
            var userId = int.Parse(userIdStr);

            int pageSize = 10;
            var query = _context.PhieuDeXuats
                .Include(p => p.MaPhongBanNavigation)
                .Where(p => p.NguoiTao == userId)
                .OrderByDescending(p => p.NgayTao);

            int totalItems = await query.CountAsync();
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            ViewBag.CurrentPage = page;

            var phieus = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return View(phieus);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new PhieuDeXuatViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PhieuDeXuatViewModel model, string action)
        {
            if (ModelState.IsValid)
            {
                if (model.ChiTiets == null || !model.ChiTiets.Any())
                {
                    ModelState.AddModelError(string.Empty, "Phải có ít nhất một hạng mục chi tiết.");
                    return View(model);
                }
                foreach (var ct in model.ChiTiets)
                {
                    if (ct.SoTien <= 0)
                    {
                        ModelState.AddModelError(string.Empty, $"Hạng mục '{ct.HangMuc}' có số tiền không hợp lệ (phải > 0).");
                        return View(model);
                    }
                }
                decimal tongTien = model.ChiTiets.Sum(c => c.SoTien);
                if (tongTien <= 0)
                {
                    ModelState.AddModelError(string.Empty, "Tổng tiền đề xuất phải lớn hơn 0.");
                    return View(model);
                }

                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var phongBanIdStr = User.FindFirstValue("PhongBanId");
                
                if (string.IsNullOrEmpty(userIdStr) || string.IsNullOrEmpty(phongBanIdStr))
                    return RedirectToAction("Login", "Auth");

                var userId = int.Parse(userIdStr);
                var phongBanId = int.Parse(phongBanIdStr);

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    if (action == "Submit")
                    {
                        bool canHold = await _budgetService.CheckAndHoldBudgetAsync(phongBanId, tongTien, model.LoaiPhieu);
                        if (!canHold)
                        {
                            ModelState.AddModelError(string.Empty, "Ngân sách năm của phòng ban không đủ để duyệt khoản tiền này.");
                            return View(model);
                        }
                    }

                    var phieu = new PhieuDeXuat
                    {
                        NguoiTao = userId,
                        MaPhongBan = phongBanId,
                        LoaiPhieu = model.LoaiPhieu,
                        LyDo = model.LyDo,
                        TongTien = tongTien,
                        TrangThai = action == "Submit" ? "Chờ duyệt" : "Nháp",
                        NgayTao = DateTime.Now,
                        BuocDuyetHienTai = 1
                    };

                    _context.PhieuDeXuats.Add(phieu);
                    await _context.SaveChangesAsync();

                    foreach (var ct in model.ChiTiets)
                    {
                        if (!string.IsNullOrWhiteSpace(ct.HangMuc))
                        {
                            _context.ChiTietPhieus.Add(new ChiTietPhieu
                            {
                                MaPhieu = phieu.MaPhieu,
                                HangMuc = ct.HangMuc,
                                SoTien = ct.SoTien,
                                GhiChu = ct.GhiChu
                            });
                        }
                    }

                    if (model.DinhKems != null && model.DinhKems.Count > 0)
                    {
                        var uploadPath = Path.Combine(_env.WebRootPath, "uploads");
                        if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

                        foreach (var file in model.DinhKems)
                        {
                            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                            var filePath = Path.Combine(uploadPath, fileName);
                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(stream);
                            }
                            _context.ChungTuDinhKems.Add(new ChungTuDinhKem
                            {
                                MaPhieu = phieu.MaPhieu,
                                TenFile = file.FileName,
                                DuongDan = "/uploads/" + fileName,
                                LoaiFile = file.ContentType,
                                NgayTaiLen = DateTime.Now
                            });
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    TempData["SuccessMessage"] = action == "Submit" ? "Đã thao tác gửi thành công!" : "Đã lưu nháp!";
                    return RedirectToAction(nameof(Index));
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToAction("Login", "Auth");
            var userId = int.Parse(userIdStr);

            var phieu = await _context.PhieuDeXuats
                .Include(p => p.ChiTietPhieus)
                .Include(p => p.ChungTuDinhKems)
                .FirstOrDefaultAsync(p => p.MaPhieu == id && p.NguoiTao == userId && p.TrangThai == "Nháp");

            if (phieu == null) return NotFound();

            var model = new PhieuDeXuatViewModel
            {
                MaPhieu = phieu.MaPhieu,
                LoaiPhieu = phieu.LoaiPhieu,
                LyDo = phieu.LyDo,
                ChiTiets = phieu.ChiTietPhieus.Select(c => new ChiTietPhieuViewModel
                {
                    HangMuc = c.HangMuc,
                    SoTien = c.SoTien,
                    GhiChu = c.GhiChu
                }).ToList()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PhieuDeXuatViewModel model, string action)
        {
            if (id != model.MaPhieu) return BadRequest();

            if (ModelState.IsValid)
            {
                if (model.ChiTiets == null || !model.ChiTiets.Any())
                {
                    ModelState.AddModelError(string.Empty, "Phải có ít nhất một hạng mục chi tiết.");
                    return View(model);
                }
                foreach (var ct in model.ChiTiets)
                {
                    if (ct.SoTien <= 0)
                    {
                        ModelState.AddModelError(string.Empty, $"Hạng mục '{ct.HangMuc}' có số tiền không hợp lệ (phải > 0).");
                        return View(model);
                    }
                }
                decimal tongTien = model.ChiTiets.Sum(c => c.SoTien);
                if (tongTien <= 0)
                {
                    ModelState.AddModelError(string.Empty, "Tổng tiền đề xuất phải lớn hơn 0.");
                    return View(model);
                }

                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdStr)) return RedirectToAction("Login", "Auth");
                var userId = int.Parse(userIdStr);

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var phieu = await _context.PhieuDeXuats
                        .Include(p => p.ChiTietPhieus)
                        .FirstOrDefaultAsync(p => p.MaPhieu == id && p.NguoiTao == userId && p.TrangThai == "Nháp");

                    if (phieu == null) return NotFound();

                    if (action == "Submit")
                    {
                        bool canHold = await _budgetService.CheckAndHoldBudgetAsync(phieu.MaPhongBan, tongTien, model.LoaiPhieu);
                        if (!canHold)
                        {
                            ModelState.AddModelError(string.Empty, "Ngân sách năm của phòng ban không đủ để duyệt khoản tiền này.");
                            return View(model);
                        }
                    }

                    phieu.LoaiPhieu = model.LoaiPhieu;
                    phieu.LyDo = model.LyDo;
                    phieu.TongTien = tongTien;
                    phieu.TrangThai = action == "Submit" ? "Chờ duyệt" : "Nháp";
                    phieu.BuocDuyetHienTai = 1;

                    _context.ChiTietPhieus.RemoveRange(phieu.ChiTietPhieus);
                    
                    if (model.ChiTiets != null)
                    {
                        foreach (var ct in model.ChiTiets)
                        {
                            if (!string.IsNullOrWhiteSpace(ct.HangMuc))
                            {
                                _context.ChiTietPhieus.Add(new ChiTietPhieu
                                {
                                    MaPhieu = phieu.MaPhieu,
                                    HangMuc = ct.HangMuc,
                                    SoTien = ct.SoTien,
                                    GhiChu = ct.GhiChu
                                });
                            }
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    TempData["SuccessMessage"] = action == "Submit" ? "Đã thao tác gửi thành công!" : "Đã lưu nháp!";
                    return RedirectToAction(nameof(Index));
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToAction("Login", "Auth");
            var userId = int.Parse(userIdStr);
            var user = await _context.NhanViens.FindAsync(userId);

            var phieu = await _context.PhieuDeXuats
                .Include(p => p.ChiTietPhieus)
                .Include(p => p.ChungTuDinhKems)
                .Include(p => p.NguoiTaoNavigation)
                .Include(p => p.NhatKyDuyets)
                    .ThenInclude(n => n.NguoiXuLyNavigation)
                .FirstOrDefaultAsync(p => p.MaPhieu == id);
            
            if (phieu == null) return NotFound();

            var workflowService = HttpContext.RequestServices.GetService(typeof(QL_ThuChiNoiBo.Services.WorkflowService)) as QL_ThuChiNoiBo.Services.WorkflowService;
            if (workflowService != null && user != null)
            {
                ViewBag.CanApprove = await workflowService.CanApproveAsync(phieu.MaPhieu, user.MaChucVu, user.MaPhongBan);
            }
            else
            {
                ViewBag.CanApprove = false;
            }

            return View(phieu);
        }
    }
}





