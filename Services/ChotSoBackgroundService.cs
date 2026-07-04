using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;
using QL_ThuChiNoiBo.Data;
using QL_ThuChiNoiBo.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace QL_ThuChiNoiBo.Services
{
    public class ChotSoBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _services;

        public ChotSoBackgroundService(IServiceProvider services)
        {
            _services = services;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.Now;
                var lastDayOfMonth = DateTime.DaysInMonth(now.Year, now.Month);

                if (now.Day == lastDayOfMonth && now.Hour == 23 && now.Minute == 59)
                {
                    using (var scope = _services.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<QlThuChiNoiBoContext>();
                        var keToanTruongIds = await context.NhanViens
                            .Where(x => x.MaChucVu == 2)
                            .Select(x => x.MaNhanVien)
                            .ToListAsync();

                        foreach(var id in keToanTruongIds)
                        {
                            bool hasNotified = await context.Set<ThongBao>().AnyAsync(t => t.NguoiNhan == id && t.ThoiGian.Value.Date == now.Date);
                            if (!hasNotified)
                            {
                                context.Set<ThongBao>().Add(new ThongBao
                                {
                                    NguoiNhan = id,
                                    NoiDung = $"Đã hết kỳ kế toán tháng {now.Month}/{now.Year}. Kế toán trưởng vui lòng bấm Xuất Báo Cáo KQKD định kỳ.",
                                    Url = "/Home/Index"
                                });
                            }
                        }
                        await context.SaveChangesAsync();
                    }
                }
                await Task.Delay(TimeSpan.FromSeconds(45), stoppingToken);
            }
        }
    }
}


