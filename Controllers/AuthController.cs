using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QL_ThuChiNoiBo.Data;
using QL_ThuChiNoiBo.ViewModels;
using System.Security.Claims;
using System.Threading.Tasks;

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
                return RedirectToAction("Index", "Home");
            }
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = "/")
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _context.NhanViens
                .Include(n => n.MaChucVuNavigation)
                .FirstOrDefaultAsync(u => u.TenDangNhap == model.Username);

            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.MatKhauHash))
            {
                ModelState.AddModelError(string.Empty, "Tài khoản hoặc mật khẩu không chính xác.");
                return View(model);
            }

            var claims = new System.Collections.Generic.List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.MaNhanVien.ToString()),
                new Claim(ClaimTypes.Name, user.HoTen),
                new Claim(ClaimTypes.Role, user.MaChucVuNavigation?.TenChucVu ?? ""),
                new Claim("PhongBanId", user.MaPhongBan.ToString()),
                new Claim("ChucVuId", user.MaChucVu.ToString())
            };

                        var permissions = new System.Collections.Generic.List<string>();
            using (var command = _context.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = @"
                    SELECT p.Permission_Code 
                    FROM User_Roles ur
                    JOIN Role_Permissions rp ON ur.Role_ID = rp.Role_ID
                    JOIN Permissions p ON rp.Permission_ID = p.Permission_ID
                    WHERE ur.User_ID = @userId";
                    
                var param = command.CreateParameter();
                param.ParameterName = "@userId";
                param.Value = user.MaNhanVien;
                command.Parameters.Add(param);

                await _context.Database.OpenConnectionAsync();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        permissions.Add(reader.GetString(0));
                    }
                }
                await _context.Database.CloseConnectionAsync();
            }

            foreach (var perm in permissions)
            {
                claims.Add(new Claim("Permission", perm));
            }
            
            // Ghi Audit_Logs 
            using (var cmd = _context.Database.GetDbConnection().CreateCommand())
            {
                cmd.CommandText = "INSERT INTO Audit_Logs (Action, Timestamp, Performed_By) VALUES ('User Login', GETDATE(), @uid)";
                var p = cmd.CreateParameter();
                p.ParameterName = "@uid";
                p.Value = user.HoTen;
                cmd.Parameters.Add(p);

                await _context.Database.OpenConnectionAsync();
                await cmd.ExecuteNonQueryAsync();
                await _context.Database.CloseConnectionAsync();
            }

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

                        if (string.IsNullOrEmpty(returnUrl) || returnUrl == "/")
            {
                if (permissions.Contains("USER_MANAGE") || permissions.Contains("ROLE_MANAGE") || permissions.Contains("AUDIT_VIEW"))
                {
                    return LocalRedirect("/Admin/Home/Index");
                }
                return RedirectToAction("Index", "Home");
            }

            return LocalRedirect(returnUrl);
        }

        [HttpGet]
        [Authorize]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            {
                return RedirectToAction("Login");
            }

            var user = await _context.NhanViens.FindAsync(userId);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            if (!BCrypt.Net.BCrypt.Verify(model.CurrentPassword, user.MatKhauHash))
            {
                ModelState.AddModelError("CurrentPassword", "Mật khẩu hiện tại không chính xác.");
                return View(model);
            }

            user.MatKhauHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
            _context.NhanViens.Update(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đổi mật khẩu thành công! Bạn có thể sử dụng mật khẩu mới cho các phiên làm việc tiếp theo.";
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View(); // Trả về trang 403 Access Denied
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Auth");
        }
    }
}







