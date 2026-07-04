using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using QL_ThuChiNoiBo.Models;

namespace QL_ThuChiNoiBo.Data;

public partial class QlThuChiNoiBoContext : DbContext
{
    public QlThuChiNoiBoContext()
    {
    }

    public QlThuChiNoiBoContext(DbContextOptions<QlThuChiNoiBoContext> options)
        : base(options)
    {
    }

    public virtual DbSet<CauHinhLuongDuyet> CauHinhLuongDuyets { get; set; }

    public virtual DbSet<ChiTietLuongDuyet> ChiTietLuongDuyets { get; set; }

    public virtual DbSet<ChiTietPhieu> ChiTietPhieus { get; set; }

    public virtual DbSet<ChucVu> ChucVus { get; set; }

    public virtual DbSet<ChungTuDinhKem> ChungTuDinhKems { get; set; }

    public virtual DbSet<NganSach> NganSaches { get; set; }

    public virtual DbSet<NhanVien> NhanViens { get; set; }

    public virtual DbSet<NhatKyDuyet> NhatKyDuyets { get; set; }

    public virtual DbSet<PhieuDeXuat> PhieuDeXuats { get; set; }

    public virtual DbSet<PhongBan> PhongBans { get; set; }

    public virtual DbSet<ThongBao> ThongBaos { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=DESKTOP-Q7UUVA9; Database=QL_ThuChiNoiBo; Trusted_Connection=True; TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CauHinhLuongDuyet>(entity =>
        {
            entity.HasKey(e => e.MaCauHinh).HasName("PK__CauHinhL__F0685B7DB3791EA7");
        });

        modelBuilder.Entity<ChiTietLuongDuyet>(entity =>
        {
            entity.HasKey(e => e.MaChiTietLuong).HasName("PK__ChiTietL__26BE62364DF45975");

            entity.HasOne(d => d.MaCauHinhNavigation).WithMany(p => p.ChiTietLuongDuyets).HasConstraintName("FK__ChiTietLu__MaCau__5CD6CB2B");

            entity.HasOne(d => d.MaChucVuDuyetNavigation).WithMany(p => p.ChiTietLuongDuyets)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ChiTietLu__MaChu__5DCAEF64");

            entity.HasOne(d => d.MaPhongBanDuyetNavigation).WithMany(p => p.ChiTietLuongDuyets).HasConstraintName("FK__ChiTietLu__MaPho__5EBF139D");
        });

        modelBuilder.Entity<ChiTietPhieu>(entity =>
        {
            entity.HasKey(e => e.MaChiTiet).HasName("PK__ChiTietP__CDF0A11403FF576A");

            entity.HasOne(d => d.MaPhieuNavigation).WithMany(p => p.ChiTietPhieus).HasConstraintName("FK__ChiTietPh__MaPhi__534D60F1");
        });

        modelBuilder.Entity<ChucVu>(entity =>
        {
            entity.HasKey(e => e.MaChucVu).HasName("PK__ChucVu__D463953306364F77");

            entity.Property(e => e.TrangThaiHoatDong).HasDefaultValue(true);
        });

        modelBuilder.Entity<ChungTuDinhKem>(entity =>
        {
            entity.HasKey(e => e.MaChungTu).HasName("PK__ChungTuD__FA38860D392F1943");

            entity.Property(e => e.NgayTaiLen).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.MaPhieuNavigation).WithMany(p => p.ChungTuDinhKems).HasConstraintName("FK__ChungTuDi__MaPhi__571DF1D5");
        });

        modelBuilder.Entity<NganSach>(entity =>
        {
            entity.HasKey(e => e.MaNganSach).HasName("PK__NganSach__E8A10D3289B7453F");

            entity.HasOne(d => d.MaPhongBanNavigation).WithMany(p => p.NganSaches)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__NganSach__MaPhon__47DBAE45");
        });

        modelBuilder.Entity<NhanVien>(entity =>
        {
            entity.HasKey(e => e.MaNhanVien).HasName("PK__NhanVien__77B2CA470022FB75");

            entity.Property(e => e.TrangThaiHoatDong).HasDefaultValue(true);

            entity.HasOne(d => d.MaChucVuNavigation).WithMany(p => p.NhanViens)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__NhanVien__MaChuc__403A8C7D");

            entity.HasOne(d => d.MaPhongBanNavigation).WithMany(p => p.NhanViens)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__NhanVien__MaPhon__3F466844");
        });

        modelBuilder.Entity<NhatKyDuyet>(entity =>
        {
            entity.HasKey(e => e.MaNhatKy).HasName("PK__NhatKyDu__E42EF42E7A3CEDB7");

            entity.Property(e => e.ThoiGian).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.MaPhieuNavigation).WithMany(p => p.NhatKyDuyets)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__NhatKyDuy__MaPhi__628FA481");

            entity.HasOne(d => d.NguoiXuLyNavigation).WithMany(p => p.NhatKyDuyets)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__NhatKyDuy__Nguoi__6383C8BA");
        });

        modelBuilder.Entity<PhieuDeXuat>(entity =>
        {
            entity.HasKey(e => e.MaPhieu).HasName("PK__PhieuDeX__2660BFE0A70992AF");

            entity.Property(e => e.BuocDuyetHienTai).HasDefaultValue(1);
            entity.Property(e => e.NgayTao).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.TrangThai).HasDefaultValue("Nhap");

            entity.HasOne(d => d.MaPhieuGocNavigation).WithMany(p => p.InverseMaPhieuGocNavigation).HasConstraintName("FK__PhieuDeXu__MaPhi__5070F446");

            entity.HasOne(d => d.MaPhongBanNavigation).WithMany(p => p.PhieuDeXuats)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PhieuDeXu__MaPho__4F7CD00D");

            entity.HasOne(d => d.NguoiTaoNavigation).WithMany(p => p.PhieuDeXuats)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PhieuDeXu__Nguoi__4E88ABD4");
        });

        modelBuilder.Entity<PhongBan>(entity =>
        {
            entity.HasKey(e => e.MaPhongBan).HasName("PK__PhongBan__D0910CC891792BAC");

            entity.Property(e => e.TrangThaiHoatDong).HasDefaultValue(true);

            entity.HasOne(d => d.MaTruongPhongNavigation).WithMany(p => p.PhongBans).HasConstraintName("FK_PhongBan_TruongPhong");
        });

        modelBuilder.Entity<ThongBao>(entity =>
        {
            entity.HasKey(e => e.MaThongBao).HasName("PK__ThongBao__04DEB54E2C95C8B9");

            entity.Property(e => e.ThoiGian).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.NguoiNhanNavigation).WithMany(p => p.ThongBaos)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ThongBao__NguoiN__68487DD7");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
