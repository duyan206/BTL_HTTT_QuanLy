using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QL_ThuChiNoiBo.Data;
using QL_ThuChiNoiBo.ViewModels;
using System.Security.Claims;

namespace QL_ThuChiNoiBo.Controllers
{
    public class AuthController : Controller
    {
        private readonly QlThuChiNoiBoContext _context;

        public AuthController(QlThuChiNoiBoContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = "/")
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return LocalRedirect(returnUrl);
            }
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = "/")
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                var user = await _context.NhanViens
                    .Include(n => n.MaChucVuNavigation)
                    .Include(n => n.MaPhongBanNavigation)
                    .FirstOrDefaultAsync(u => u.TenDangNhap == model.Username && u.TrangThaiHoatDong == true);

                if (user != null && BCrypt.Net.BCrypt.Verify(model.Password, user.MatKhauHash))
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.MaNhanVien.ToString()),
                        new Claim(ClaimTypes.Name, user.HoTen ?? ""),
                        new Claim(ClaimTypes.Role, user.MaChucVuNavigation?.TenChucVu ?? ""),
                        new Claim("PhongBanId", user.MaPhongBan.ToString())
                    };

                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);

                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties
                    {
                        IsPersistent = model.RememberMe
                    });

                    return LocalRedirect(returnUrl);
                }

                ModelState.AddModelError(string.Empty, "Tài khoản hoặc mật khẩu không chính xác.");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Auth");
        }
    }
}
