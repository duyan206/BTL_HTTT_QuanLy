using System;
using System.Collections.Generic;

namespace QL_ThuChiNoiBo.ViewModels
{
    public class BudgetDashboardViewModel
    {
        public decimal TongNganSach { get; set; }
        public decimal TongDaThu { get; set; }
        public decimal TongDaChi { get; set; }
        public decimal TongDangTreo { get; set; }
        public decimal TongConLai => (TongNganSach + TongDaThu) - (TongDaChi + TongDangTreo);

        public List<DepartmentBudgetViewModel> DepartmentBudgets { get; set; } = new List<DepartmentBudgetViewModel>();
    }

    public class DepartmentBudgetViewModel
    {
        public string TenPhongBan { get; set; } = null!;
        public decimal NganSach { get; set; }
        public decimal DaThu { get; set; }
        public decimal DaChi { get; set; }
        public decimal DangTreo { get; set; }
        public decimal ConLai => Math.Max(0, (NganSach + DaThu) - (DaChi + DangTreo));
        public double TyLeSuDung => (NganSach + DaThu) > 0 ? (double)((DaChi + DangTreo) / (NganSach + DaThu)) * 100 : 0;
    }
}
