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

                private async Task<List<int>> GetApprovalChain(int creatorId, string loaiPhieu, decimal tongTien)
        {
            var creator = await _context.NhanViens.FindAsync(creatorId);
            if (creator == null) return new List<int> { 1 };

            int role = creator.MaChucVu;
            
            bool isGiamDoc = (role == 1);
            bool isKeToanTruong = (role == 2);
            bool isTruongPhong = (role == 3 || role == 6 || role == 8);

            if (isGiamDoc) return new List<int> { 2 };
            if (isKeToanTruong) return new List<int> { 1 };
            if (isTruongPhong) return new List<int> { 2, 1 };

            // Nhân viên
            if (loaiPhieu == "ThanhToan")
            {
                if (tongTien > 5000000)
                    return new List<int> { 3, 2, 1 }; // 3 is code for Trưởng phòng step
                else
                    return new List<int> { 3, 2 }; 
            }
            else
            {
                // Tạm ứng, Hoàn ứng, ThuNộiBộ -> Không qua Trưởng phòng. Thẳng tới Kế toán trưởng và Giám đốc
                return new List<int> { 2, 1 };
            }
        }

                public async Task NotifyNextApproverAsync(PhieuDeXuat p)
        {
            var chain = await GetApprovalChain(p.NguoiTao, p.LoaiPhieu, p.TongTien);
            int currentZeroIndex = (p.BuocDuyetHienTai ?? 1) - 1;
            
            if (currentZeroIndex >= 0 && currentZeroIndex < chain.Count)
            {
                int expectedRole = chain[currentZeroIndex];
                List<int> recipientIds = new List<int>();

                if (expectedRole == 3)
                {
                    recipientIds = await _context.NhanViens
                        .Where(n => n.MaChucVu == 3 && n.MaPhongBan == p.MaPhongBan)
                        .Select(n => n.MaNhanVien)
                        .ToListAsync();
                }
                else
                {
                    recipientIds = await _context.NhanViens
                        .Where(n => n.MaChucVu == expectedRole)
                        .Select(n => n.MaNhanVien)
                        .ToListAsync();
                }

                foreach (var id in recipientIds)
                {
                    _context.Set<ThongBao>().Add(new ThongBao {
                        NguoiNhan = id,
                        NoiDung = "Có 1 phiếu đề xuất (Mã: #" + p.MaPhieu + ") đang chờ bạn phê duyệt!",
                        Url = "/DuyetPhieu",
                        ThoiGian = DateTime.Now
                    });
                }
            }
        }

        public async Task<List<PhieuDeXuat>> GetPendingRequestsAsync(int userId, int userChucVuId, int? userPhongBanId)
        {
            var pendingPhieus = await _context.PhieuDeXuats
                .Include(p => p.NguoiTaoNavigation)
                .ThenInclude(n => n.MaPhongBanNavigation)
                .Where(p => p.TrangThai == "Chờ duyệt")
                .AsNoTracking()
                .ToListAsync();

            var result = new List<PhieuDeXuat>();
            foreach (var p in pendingPhieus)
            {
                var chain = await GetApprovalChain(p.NguoiTao, p.LoaiPhieu, p.TongTien);
                int currentZeroIndex = (p.BuocDuyetHienTai ?? 1) - 1; // BuocDuyetHienTai starts at 1

                if (currentZeroIndex >= 0 && currentZeroIndex < chain.Count)
                {
                    int expectedRole = chain[currentZeroIndex];
                    if (expectedRole == userChucVuId)
                    {
                        if (expectedRole == 3)
                        {
                            // Trưởng phòng bộ phận chỉ được duyệt phiếu của nhân viên trong chính phòng đó
                            if (p.NguoiTaoNavigation.MaPhongBan == userPhongBanId.GetValueOrDefault())
                            {
                                result.Add(p);
                            }
                        }
                        else
                        {
                            result.Add(p);
                        }
                    }
                }
            }
            return result.OrderByDescending(x => x.NgayTao).ToList();
        }

        public async Task<bool> CanApproveAsync(int phieuId, int userChucVuId, int? userPhongBanId)
        {
            var phieu = await _context.PhieuDeXuats.Include(p => p.NguoiTaoNavigation).FirstOrDefaultAsync(x => x.MaPhieu == phieuId);
            if (phieu == null || phieu.TrangThai != "Chờ duyệt") return false;

            var chain = await GetApprovalChain(phieu.NguoiTao, phieu.LoaiPhieu, phieu.TongTien);
            int currentZeroIndex = (phieu.BuocDuyetHienTai ?? 1) - 1;

            if (currentZeroIndex >= 0 && currentZeroIndex < chain.Count)
            {
                int expectedRole = chain[currentZeroIndex];
                if (expectedRole == userChucVuId)
                {
                    if (expectedRole == 3 && phieu.NguoiTaoNavigation.MaPhongBan != userPhongBanId.GetValueOrDefault()) return false;
                    return true;
                }
            }
            return false;
        }

        public async Task<bool> ApproveAsync(int phieuId, int userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var phieu = await _context.PhieuDeXuats.FirstOrDefaultAsync(p => p.MaPhieu == phieuId);
                if (phieu == null || phieu.TrangThai != "Chờ duyệt") return false;

                var chain = await GetApprovalChain(phieu.NguoiTao, phieu.LoaiPhieu, phieu.TongTien);
                if ((phieu.BuocDuyetHienTai ?? 1) < 1 || (phieu.BuocDuyetHienTai ?? 1) > chain.Count) return false;

                phieu.BuocDuyetHienTai++;
                
                var nhatKy = new NhatKyDuyet
                {
                    MaPhieu = phieuId,
                    NguoiXuLy = userId,
                    HanhDong = "Phê Duyệt",
                    ThoiGian = DateTime.Now,
                    GhiChu = "Dynamic Workflow Mode - Chấp thuận"
                };
                _context.NhatKyDuyets.Add(nhatKy);

                if ((phieu.BuocDuyetHienTai ?? 1) > chain.Count)
                {
                    phieu.TrangThai = "Đã duyệt";
                    _context.Set<ThongBao>().Add(new ThongBao { NguoiNhan = phieu.NguoiTao, NoiDung = $"Xin chúc mừng! Phiếu #" + phieu.MaPhieu + " của bạn đã được PHÊ DUYỆT TỔNG!", Url = "/PhieuDeXuat/Details/" + phieu.MaPhieu, ThoiGian = DateTime.Now });
                    await _budgetService.CommitBudgetAsync(phieu.MaPhongBan, phieu.TongTien, phieu.LoaiPhieu);
                }

                _context.PhieuDeXuats.Update(phieu);
                if (phieu.TrangThai == "Chờ duyệt") { await NotifyNextApproverAsync(phieu); } await _context.SaveChangesAsync();
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
                _context.Set<ThongBao>().Add(new ThongBao { NguoiNhan = phieu.NguoiTao, NoiDung = $"Rất tiếc! Phiếu #" + phieu.MaPhieu + " của bạn đã bị TỪ CHỐI. Lý do: " + reason, Url = "/PhieuDeXuat/Details/" + phieu.MaPhieu, ThoiGian = DateTime.Now });
                
                var nhatKy = new NhatKyDuyet
                {
                    MaPhieu = phieuId,
                    NguoiXuLy = userId,
                    HanhDong = "Từ Chối",
                    GhiChu = reason,
                    ThoiGian = DateTime.Now
                };
                _context.NhatKyDuyets.Add(nhatKy);

                await _budgetService.ReleaseHoldBudgetAsync(phieu.MaPhongBan, phieu.TongTien, phieu.LoaiPhieu);

                _context.PhieuDeXuats.Update(phieu);
                if (phieu.TrangThai == "Chờ duyệt") { await NotifyNextApproverAsync(phieu); } await _context.SaveChangesAsync();
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








