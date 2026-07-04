using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QL_ThuChiNoiBo.Data;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System;

namespace QL_ThuChiNoiBo.Controllers
{
    [Authorize]
    public class ThongBaoController : Controller
    {
        private readonly QlThuChiNoiBoContext _context;

        public ThongBaoController(QlThuChiNoiBoContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetLatest()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
                return Unauthorized();

            var thongBaos = await _context.Set<Models.ThongBao>()
                .Where(t => t.NguoiNhan == userId)
                .OrderByDescending(t => t.ThoiGian)
                .Take(10)
                .ToListAsync();

            var tbList = thongBaos.Select(t => new {
                t.MaThongBao,
                t.NoiDung,
                t.Url,
                t.DaDoc,
                ThoiGianStr = GetTimeAgo(t.ThoiGian ?? DateTime.Now)
            }).ToList();

            var unreadCount = await _context.Set<Models.ThongBao>().CountAsync(t => t.NguoiNhan == userId && !t.DaDoc);

            return Json(new { unreadCount, notifications = tbList });
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
                return Unauthorized();

            var tb = await _context.Set<Models.ThongBao>().FirstOrDefaultAsync(t => t.MaThongBao == id && t.NguoiNhan == userId);
            if (tb != null && !tb.DaDoc)
            {
                tb.DaDoc = true;
                await _context.SaveChangesAsync();
            }
            
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
                return Unauthorized();

            var unreads = await _context.Set<Models.ThongBao>().Where(t => t.NguoiNhan == userId && !t.DaDoc).ToListAsync();
            foreach(var tb in unreads) tb.DaDoc = true;
            
            if (unreads.Any()) await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        private static string GetTimeAgo(DateTime time)
        {
            var span = DateTime.Now - time;
            if (span.TotalMinutes < 1) return "Vừa xong";
            if (span.TotalHours < 1) return $"{(int)span.TotalMinutes} phút trước";
            if (span.TotalDays < 1) return $"{(int)span.TotalHours} giờ trước";
            if (span.TotalDays < 7) return $"{(int)span.TotalDays} ngày trước";
            return time.ToString("dd/MM/yyyy HH:mm");
        }
    }
}


