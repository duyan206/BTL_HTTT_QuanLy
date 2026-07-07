using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QL_ThuChiNoiBo.Filters;
using QL_ThuChiNoiBo.Models;
using QL_ThuChiNoiBo.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace QL_ThuChiNoiBo.Controllers
{
    public class AdminController : Controller
    {
        private readonly QlThuChiNoiBoContext _context;

        public AdminController(QlThuChiNoiBoContext context)
        {
            _context = context;
        }

        private void LogAudit(string action)
        {
            var user = User.Identity?.Name ?? "Unknown";
            _context.Database.ExecuteSqlRaw("INSERT INTO Audit_Logs (Action, Performed_By) VALUES ({0}, {1})", action, user);
        }

        // ==========================================
        // 1. MODULE USER MANAGEMENT
        // ==========================================
        [HasPermission("USER_MANAGE")]
        [HttpGet]
        public async Task<IActionResult> Users()
        {
            // Lấy danh sách nhân viên bằng EF, trừ tên đăng nhập root
            var users = await _context.NhanViens
                .Include(n => n.MaChucVuNavigation)
                .Include(n => n.MaPhongBanNavigation)
                .Where(u => u.TenDangNhap != "admin" && u.TenDangNhap != "sysadmin_root")
                .AsNoTracking()
                .ToListAsync();
            return View(users);
        }

        [HasPermission("USER_MANAGE")]
        [HttpPost]
        public async Task<IActionResult> SuspendUser(int id)
        {
            var user = await _context.NhanViens.FindAsync(id);
            if (user != null)
            {
                user.Status = "Inactive";
                user.TrangThaiHoatDong = false;
                await _context.SaveChangesAsync();
                LogAudit($"Khóa tài khoản: {user.TenDangNhap}");
                TempData["SuccessMessage"] = "Khóa tài khoản thành công!";
            }
            return RedirectToAction(nameof(Users));
        }

        [HasPermission("USER_MANAGE")]
        [HttpPost]
        public async Task<IActionResult> UnlockUser(int id)
        {
            var user = await _context.NhanViens.FindAsync(id);
            if (user != null)
            {
                user.Status = "Active";
                user.TrangThaiHoatDong = true;
                await _context.SaveChangesAsync();
                LogAudit($"Mở khóa tài khoản: {user.TenDangNhap}");
                TempData["SuccessMessage"] = "Mở khóa tài khoản thành công!";
            }
            return RedirectToAction(nameof(Users));
        }

        [HasPermission("USER_MANAGE")]
        [HttpPost]
        public async Task<IActionResult> ResetPassword(int id)
        {
            var user = await _context.NhanViens.FindAsync(id);
            if (user != null)
            {
                user.MatKhauHash = BCrypt.Net.BCrypt.HashPassword("123456");
                await _context.SaveChangesAsync();
                LogAudit($"Reset mật khẩu User: {user.TenDangNhap}");
                TempData["SuccessMessage"] = "Mật khẩu đã reset về: 123456";
            }
            return RedirectToAction(nameof(Users));
        }

        [HasPermission("USER_MANAGE")]
        [HttpPost]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync<IActionResult>(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var user = await _context.NhanViens.FindAsync(id);
                    if (user != null)
                    {
                        // Kiểm tra nếu là ROOT thì không cho xóa
                        if (user.TenDangNhap == "admin" || user.TenDangNhap == "sysadmin_root")
                            throw new Exception("Không thể xóa tài khoản Quản trị tối cao!");
                        
                        await _context.Database.ExecuteSqlRawAsync("DELETE FROM User_Roles WHERE User_ID = {0}", id);
                        _context.NhanViens.Remove(user);
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                        LogAudit($"Xóa vĩnh viễn tài khoản: {user.TenDangNhap}");
                        TempData["SuccessMessage"] = "Đã xóa vĩnh viễn người dùng!";
                    }
                    return Ok();
                }
                catch(Exception ex)
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = "Lỗi khi xóa người dùng: " + ex.Message;
                    return BadRequest();
                }
            });
            return RedirectToAction(nameof(Users));
        }

        public class UpdateUserViewModel
        {
            public int MaNhanVien { get; set; }
            public string HoTen { get; set; }
            public string Email { get; set; }
            public string MatKhau { get; set; } 
            public int MaPhongBan { get; set; }
            public int RoleId { get; set; }
            public int MaChucVu { get; set; }
        }

        [HasPermission("USER_MANAGE")]
        [HttpGet]
        public async Task<IActionResult> GetEditUserFormData(int id)
        {
            var user = await _context.NhanViens.FindAsync(id);
            if (user == null) return NotFound();

            int roleId = 0;
            await _context.Database.OpenConnectionAsync();
            using (var cmd = _context.Database.GetDbConnection().CreateCommand())
            {
                cmd.CommandText = "SELECT TOP 1 Role_ID FROM User_Roles WHERE User_ID = " + id;
                var result = await cmd.ExecuteScalarAsync();
                if (result != null && result != DBNull.Value) roleId = Convert.ToInt32(result);
            }
            await _context.Database.CloseConnectionAsync();

            var formData = new {
                user.MaNhanVien, user.TenDangNhap, user.HoTen, user.Email, user.MaPhongBan, user.MaChucVu, roleId = roleId
            };
            return Json(formData);
        }

        [HasPermission("USER_MANAGE")]
        [HttpPost]
        public async Task<IActionResult> UpdateUser([FromBody] UpdateUserViewModel model)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync<IActionResult>(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var user = await _context.NhanViens.FindAsync(model.MaNhanVien);
                    if (user == null) return BadRequest("Không tìm thấy người dùng!");

                    user.HoTen = model.HoTen;
                    user.Email = model.Email;
                    user.MaPhongBan = model.MaPhongBan;
                    user.MaChucVu = model.MaChucVu;

                    if (!string.IsNullOrEmpty(model.MatKhau))
                    {
                        user.MatKhauHash = BCrypt.Net.BCrypt.HashPassword(model.MatKhau);
                    }

                    _context.NhanViens.Update(user);
                    await _context.SaveChangesAsync();

                    await _context.Database.ExecuteSqlRawAsync("DELETE FROM User_Roles WHERE User_ID = {0}", user.MaNhanVien);
                    if (model.RoleId > 0)
                    {
                        await _context.Database.ExecuteSqlRawAsync("INSERT INTO User_Roles (User_ID, Role_ID) VALUES ({0}, {1})", user.MaNhanVien, model.RoleId);
                    }

                    LogAudit($"Chỉnh sửa người dùng: {user.TenDangNhap}");

                    await transaction.CommitAsync();
                    TempData["SuccessMessage"] = $"Đã cập nhật nhân sự {user.TenDangNhap} thành công!";
                    return Ok();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, "Lỗi cập nhật: " + ex.Message);
                }
            });
        }

        // ==========================================
        // 2. MODULE RBAC CONFIGURATION
        // ==========================================
        public class CreateUserViewModel
        {
            public string TenDangNhap { get; set; }
            public string HoTen { get; set; }
            public string Email { get; set; }
            public string MatKhau { get; set; }
            public int MaPhongBan { get; set; }
            public int RoleId { get; set; }
            public int MaChucVu { get; set; }
        }

        [HasPermission("USER_MANAGE")]
        [HttpGet]
        public async Task<IActionResult> GetCreateUserFormData()
        {
            var phongBans = await _context.PhongBans.Select(p => new { p.MaPhongBan, p.TenPhongBan }).ToListAsync();
            var chucVus = await _context.ChucVus.Select(c => new { c.MaChucVu, c.TenChucVu }).ToListAsync();
            
            var roles = new List<object>();
            await _context.Database.OpenConnectionAsync();
            using (var cmd = _context.Database.GetDbConnection().CreateCommand())
            {
                cmd.CommandText = "SELECT Role_ID, Description FROM Roles";
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        roles.Add(new { RoleId = reader.GetInt32(0), RoleName = reader.GetString(1) });
                    }
                }
            }
            await _context.Database.CloseConnectionAsync();

            return Json(new { phongBans, roles, chucVus });
        }

        [HasPermission("USER_MANAGE")]
        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserViewModel model)
        {
            if (string.IsNullOrEmpty(model.TenDangNhap) || string.IsNullOrEmpty(model.MatKhau))
                return BadRequest("Thiếu thông tin bắt buộc!");

            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync<IActionResult>(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Bước 1: Hash Mật Khẩu
                    string hash = BCrypt.Net.BCrypt.HashPassword(model.MatKhau);

                    // Bước 2: Insert NhanVien
                    var checkExist = await _context.NhanViens.AnyAsync(n => n.TenDangNhap == model.TenDangNhap);
                    if (checkExist)
                    {
                        return BadRequest("Tên đăng nhập đã tồn tại!");
                    }

                    var newUser = new NhanVien
                    {
                        TenDangNhap = model.TenDangNhap,
                        HoTen = model.HoTen,
                        Email = model.Email,
                        MatKhauHash = hash,
                        MaPhongBan = model.MaPhongBan,
                        MaChucVu = model.MaChucVu, // Require theo schema
                        Status = "Active",
                        TrangThaiHoatDong = true
                    };

                    _context.NhanViens.Add(newUser);
                    await _context.SaveChangesAsync();
                    int newUserId = newUser.MaNhanVien;

                    // Bước 3: Insert User_Roles
                    await _context.Database.ExecuteSqlRawAsync("INSERT INTO User_Roles (User_ID, Role_ID) VALUES ({0}, {1})", newUserId, model.RoleId);
                    
                    LogAudit($"Thêm người dùng mới: {model.TenDangNhap}");

                    await transaction.CommitAsync();
                    TempData["SuccessMessage"] = $"Đã tạo mới nhân sự {model.TenDangNhap} thành công!";
                    return Ok();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, "Lỗi hệ thống khi tạo người dùng: " + ex.Message + " | Inner: " + (ex.InnerException?.Message ?? "No inner exception"));
                }
            });
        }

        // ==========================================
        // 2. MODULE RBAC CONFIGURATION
        public class RoleViewModel
        {
            public int RoleId { get; set; }
            public string RoleName { get; set; }
            public string Description { get; set; }
            public List<int> PermissionIds { get; set; } = new List<int>();
        }

        public class PermissionViewModel
        {
            public int PermissionId { get; set; }
            public string PermissionCode { get; set; }
            public string Description { get; set; }
        }

        [HasPermission("ROLE_MANAGE")]
        [HttpGet]
        public async Task<IActionResult> Roles()
        {
            var roles = new List<RoleViewModel>();
            var perms = new List<PermissionViewModel>();

            await _context.Database.OpenConnectionAsync();

            // Load Permissions
            using (var cmd = _context.Database.GetDbConnection().CreateCommand())
            {
                cmd.CommandText = "SELECT Permission_ID, Permission_Code, Description FROM Permissions";
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        perms.Add(new PermissionViewModel {
                            PermissionId = reader.GetInt32(0),
                            PermissionCode = reader.GetString(1),
                            Description = reader.IsDBNull(2) ? "" : reader.GetString(2)
                        });
                    }
                }
            }

            // Load Roles
            using (var cmd = _context.Database.GetDbConnection().CreateCommand())
            {
                cmd.CommandText = "SELECT Role_ID, Role_Name, Description FROM Roles";
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        roles.Add(new RoleViewModel {
                            RoleId = reader.GetInt32(0),
                            RoleName = reader.GetString(1),
                            Description = reader.IsDBNull(2) ? "" : reader.GetString(2)
                        });
                    }
                }
            }

            // Load Role_Permissions
            using (var cmd = _context.Database.GetDbConnection().CreateCommand())
            {
                cmd.CommandText = "SELECT Role_ID, Permission_ID FROM Role_Permissions";
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        int roleId = reader.GetInt32(0);
                        int permId = reader.GetInt32(1);
                        var targetRole = roles.FirstOrDefault(r => r.RoleId == roleId);
                        if (targetRole != null) targetRole.PermissionIds.Add(permId);
                    }
                }
            }

            await _context.Database.CloseConnectionAsync();

            ViewBag.AllPermissions = perms;
            return View(roles);
        }

        [HasPermission("ROLE_MANAGE")]
        [HttpPost]
        public async Task<IActionResult> UpdateRolePermissions(int roleId, List<int> permissions)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Database.ExecuteSqlRaw("DELETE FROM Role_Permissions WHERE Role_ID = {0}", roleId);
                foreach (var permId in permissions)
                {
                    _context.Database.ExecuteSqlRaw("INSERT INTO Role_Permissions (Role_ID, Permission_ID) VALUES ({0}, {1})", roleId, permId);
                }
                LogAudit($"Cập nhật quyền cho RoleID: {roleId} (Số lượng: {permissions.Count})");
                await transaction.CommitAsync();
                TempData["SuccessMessage"] = "Cập nhật phân quyền thành công!";
            }
            catch
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "Lỗi cập nhật phân quyền!";
            }
            return RedirectToAction(nameof(Roles));
        }

        // ==========================================
        // 3. MODULE AUDIT LOG
        // ==========================================
        public class AuditLogViewModel
        {
            public int LogId { get; set; }
            public string Action { get; set; }
            public DateTime Timestamp { get; set; }
            public string PerformedBy { get; set; }
        }

        
        // ==========================================
        // 4. MODULE DASHBOARD STATS
        // ==========================================
        [HasPermission("AUDIT_VIEW")]
        [HttpGet]
        public async Task<IActionResult> GetSystemHealthStats()
        {
            var result = new int[24];
            await _context.Database.OpenConnectionAsync();
            using (var cmd = _context.Database.GetDbConnection().CreateCommand())
            {
                // Lọc action Login và thuộc timestamp ngày hôm nay
                cmd.CommandText = @"
                    SELECT DATEPART(HOUR, Timestamp) as LogHour, COUNT(Log_ID) as ViewCount
                    FROM Audit_Logs
                    WHERE Action LIKE '%Login%' AND CAST(Timestamp AS DATE) = CAST(GETDATE() AS DATE)
                    GROUP BY DATEPART(HOUR, Timestamp)";
                
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        int hour = reader.GetInt32(0);
                        int count = reader.GetInt32(1);
                        if(hour >= 0 && hour < 24) result[hour] = count;
                    }
                }
            }
            await _context.Database.CloseConnectionAsync();
            return Json(new { success = true, data = result });
        }
    }
}






