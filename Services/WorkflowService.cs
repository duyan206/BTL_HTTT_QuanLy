using Microsoft.EntityFrameworkCore;
using QL_ThuChiNoiBo.Data;
using QL_ThuChiNoiBo.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QL_ThuChiNoiBo.Services
{
    public class WorkflowService
    {
        private readonly QlThuChiNoiBoContext _context;
        private readonly BudgetService _budgetService;

        public WorkflowService(QlThuChiNoiBoContext context, BudgetService budgetService)
        {
            _context = context;
            _budgetService = budgetService;
        }

        public async Task<List<PhieuDeXuat>> GetPendingRequestsAsync(int userId, int userChucVuId, int userPhongBanId)
        {
            var phieus = await _context.PhieuDeXuats
                .Include(p => p.NguoiTaoNavigation)
                .Where(p => p.TrangThai == "Chờ duyệt")
                .ToListAsync();

            var pendingPhieus = new List<PhieuDeXuat>();
            var configs = await _context.CauHinhLuongDuyets.Include(c => c.ChiTietLuongDuyets).ToListAsync();

            foreach (var p in phieus)
            {
                var config = configs.FirstOrDefault(c => c.LoaiPhieu == p.LoaiPhieu && p.TongTien >= c.TienToiThieu && p.TongTien <= c.TienToiDa);
                if (config != null)
                {
                    var currentStep = config.ChiTietLuongDuyets.FirstOrDefault(ct => ct.ThuTuBuoc == p.BuocDuyetHienTai);
                    if (currentStep != null)
                    {
                        if (currentStep.MaChucVuDuyet == userChucVuId && 
                            (currentStep.MaPhongBanDuyet == null || currentStep.MaPhongBanDuyet == userPhongBanId))
                        {
                            pendingPhieus.Add(p);
                        }
                    }
                }
            }
            return pendingPhieus.OrderByDescending(p => p.NgayTao).ToList();
        }

        public async Task<bool> CanApproveAsync(int phieuId, int userChucVuId, int userPhongBanId)
        {
            var p = await _context.PhieuDeXuats.FirstOrDefaultAsync(x => x.MaPhieu == phieuId && x.TrangThai == "Chờ duyệt");
            if (p == null) return false;

            var config = await _context.CauHinhLuongDuyets.Include(c => c.ChiTietLuongDuyets)
                .FirstOrDefaultAsync(c => c.LoaiPhieu == p.LoaiPhieu && p.TongTien >= c.TienToiThieu && p.TongTien <= c.TienToiDa);
            if(config == null) return false;

            var currentStep = config.ChiTietLuongDuyets.FirstOrDefault(ct => ct.ThuTuBuoc == p.BuocDuyetHienTai);
            if(currentStep == null) return false;

            return currentStep.MaChucVuDuyet == userChucVuId && (currentStep.MaPhongBanDuyet == null || currentStep.MaPhongBanDuyet == userPhongBanId);
        }

        public async Task<bool> ApproveAsync(int phieuId, int userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var phieu = await _context.PhieuDeXuats.FirstOrDefaultAsync(p => p.MaPhieu == phieuId);
                if (phieu == null || phieu.TrangThai != "Chờ duyệt") return false;

                var config = await _context.CauHinhLuongDuyets
                    .Include(c => c.ChiTietLuongDuyets)
                    .FirstOrDefaultAsync(c => c.LoaiPhieu == phieu.LoaiPhieu && phieu.TongTien >= c.TienToiThieu && phieu.TongTien <= c.TienToiDa);

                if (config == null) throw new Exception("Config not found");

                phieu.BuocDuyetHienTai++;
                
                var nhatKy = new NhatKyDuyet
                {
                    MaPhieu = phieuId,
                    NguoiXuLy = userId,
                    HanhDong = "Phê Duyệt",
                    ThoiGian = DateTime.Now
                };
                _context.NhatKyDuyets.Add(nhatKy);

                if (phieu.BuocDuyetHienTai > config.TongSoBuoc)
                {
                    phieu.TrangThai = "Đã duyệt";
                    await _budgetService.CommitBudgetAsync(phieu.MaPhongBan, phieu.TongTien, phieu.LoaiPhieu);
                }

                _context.PhieuDeXuats.Update(phieu);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> RejectAsync(int phieuId, int userId, string reason)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var phieu = await _context.PhieuDeXuats.FirstOrDefaultAsync(p => p.MaPhieu == phieuId);
                if (phieu == null || phieu.TrangThai != "Chờ duyệt") return false;

                phieu.TrangThai = "Từ chối";
                
                var nhatKy = new NhatKyDuyet
                {
                    MaPhieu = phieuId,
                    NguoiXuLy = userId,
                    HanhDong = "Từ chối",
                    GhiChu = reason,
                    ThoiGian = DateTime.Now
                };
                _context.NhatKyDuyets.Add(nhatKy);

                await _budgetService.ReleaseHoldBudgetAsync(phieu.MaPhongBan, phieu.TongTien, phieu.LoaiPhieu);

                _context.PhieuDeXuats.Update(phieu);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}

