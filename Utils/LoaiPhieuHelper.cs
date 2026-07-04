using Microsoft.AspNetCore.Html;
using System;

namespace QL_ThuChiNoiBo.Utils
{
    public enum LoaiPhieuEnum
    {
        TamUng = 1,
        HoanUng = 2,
        ThanhToan = 3,
        ThuNoiBo = 4
    }

    public static class LoaiPhieuHelper
    {
        public static LoaiPhieuEnum? Parse(string loaiPhieuStr)
        {
            if (string.IsNullOrEmpty(loaiPhieuStr)) return null;
            if (Enum.TryParse(typeof(LoaiPhieuEnum), loaiPhieuStr, true, out var result))
            {
                return (LoaiPhieuEnum)result;
            }
            return null;
        }

        public static HtmlString RenderBadge(string loaiPhieuStr)
        {
            var loaiPhieu = Parse(loaiPhieuStr);
            string bgClass = "bg-secondary";
            string textClass = "text-secondary";
            string icon = "bi-file-earmark-text";
            string displayName = loaiPhieuStr;

            if (loaiPhieu.HasValue)
            {
                switch (loaiPhieu.Value)
                {
                    case LoaiPhieuEnum.TamUng:
                        bgClass = "bg-primary";
                        textClass = "text-primary";
                        icon = "bi-wallet2";
                        displayName = "Tạm Ứng";
                        break;
                    case LoaiPhieuEnum.HoanUng:
                        bgClass = "bg-info";
                        textClass = "text-info";
                        icon = "bi-arrow-repeat";
                        displayName = "Hoàn Ứng";
                        break;
                    case LoaiPhieuEnum.ThanhToan:
                        bgClass = "bg-warning";
                        textClass = "text-warning";
                        icon = "bi-receipt";
                        displayName = "Thanh Toán";
                        break;
                    case LoaiPhieuEnum.ThuNoiBo:
                        bgClass = "bg-success";
                        textClass = "text-success";
                        icon = "bi-box-arrow-in-down";
                        displayName = "Thu Nội Bộ";
                        break;
                }
            }

            string html = $"<span class=\"badge {bgClass} bg-opacity-10 {textClass} border border-{bgClass.Replace("bg-", "")} border-opacity-25\"><i class=\"bi {icon} me-1\"></i> {displayName}</span>";
            return new HtmlString(html);
        }
    }
}
