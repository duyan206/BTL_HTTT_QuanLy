using Microsoft.EntityFrameworkCore;
using QL_ThuChiNoiBo.Data;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace QL_ThuChiNoiBo.Services
{
    public class BudgetService
    {
        private readonly QlThuChiNoiBoContext _context;

        public BudgetService(QlThuChiNoiBoContext context)
        {
            _context = context;
        }

        public async Task<bool> CheckAndHoldBudgetAsync(int phongBanId, decimal amount, string loaiPhieu)
        {
            if (loaiPhieu == "ThuNoiBo") return true;

            var year = DateTime.Now.Year;
            var nganSach = await _context.NganSaches
                .FirstOrDefaultAsync(n => n.MaPhongBan == phongBanId && n.NamTaiChinh == year && n.Thang == DateTime.Now.Month);

            if (nganSach == null) return false;

            if (nganSach.DaChi + nganSach.TienDangTreo + amount > (nganSach.TongNganSach + nganSach.DaThu))
            {
                return false;
            }

            nganSach.TienDangTreo += amount;
            _context.NganSaches.Update(nganSach);
            return true;
        }

        public async Task ReleaseHoldBudgetAsync(int phongBanId, decimal amount, string loaiPhieu)
        {
            if (loaiPhieu == "ThuNoiBo") return;

            var year = DateTime.Now.Year;
            var nganSach = await _context.NganSaches
                .FirstOrDefaultAsync(n => n.MaPhongBan == phongBanId && n.NamTaiChinh == year && n.Thang == DateTime.Now.Month);

            if (nganSach != null)
            {
                nganSach.TienDangTreo -= amount;
                if(nganSach.TienDangTreo < 0) nganSach.TienDangTreo = 0;
                _context.NganSaches.Update(nganSach);
            }
        }

        public async Task CommitBudgetAsync(int phongBanId, decimal amount, string loaiPhieu)
        {
            var year = DateTime.Now.Year;
            var nganSach = await _context.NganSaches
                .FirstOrDefaultAsync(n => n.MaPhongBan == phongBanId && n.NamTaiChinh == year && n.Thang == DateTime.Now.Month);

            if (nganSach != null)
            {
                if (loaiPhieu == "ThuNoiBo")
                {
                    nganSach.DaThu += amount;
                }
                else
                {
                    nganSach.TienDangTreo -= amount;
                    if(nganSach.TienDangTreo < 0) nganSach.TienDangTreo = 0;
                    nganSach.DaChi += amount;
                }
                _context.NganSaches.Update(nganSach);
            }
        }

        public async Task<QL_ThuChiNoiBo.ViewModels.BudgetDashboardViewModel> GetBudgetDashboardAsync(int year)
        {
            var nganSaches = await _context.NganSaches
                .Include(n => n.MaPhongBanNavigation)
                .Where(n => n.NamTaiChinh == year && n.Thang == DateTime.Now.Month && n.MaPhongBanNavigation.TenPhongBan != "System Administration")
                .ToListAsync();

            var result = new QL_ThuChiNoiBo.ViewModels.BudgetDashboardViewModel();
            foreach (var ns in nganSaches)
            {
                result.TongNganSach += ns.TongNganSach;
                result.TongDaChi += ns.DaChi;
                result.TongDangTreo += ns.TienDangTreo;
                result.TongDaThu += ns.DaThu;

                result.DepartmentBudgets.Add(new QL_ThuChiNoiBo.ViewModels.DepartmentBudgetViewModel
                {
                    TenPhongBan = ns.MaPhongBanNavigation?.TenPhongBan ?? "Không xác định",
                    NganSach = ns.TongNganSach,
                    DaChi = ns.DaChi,
                    DangTreo = ns.TienDangTreo,
                    DaThu = ns.DaThu
                });
            }

            return result;
        }
    }
}


