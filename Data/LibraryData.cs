using quan_ly_muon_tra_sach_thu_vien.Models;

namespace quan_ly_muon_tra_sach_thu_vien.Data;

public sealed class LibraryData
{
    public List<LibraryAccount> Accounts { get; set; } = [];
    public List<DocGia> DocGia { get; set; } = [];
    public List<TheLoai> TheLoai { get; set; } = [];
    public List<Sach> Sach { get; set; } = [];
    public List<NhanVien> NhanVien { get; set; } = [];
    public List<PhieuMuon> PhieuMuon { get; set; } = [];
    public List<ChiTietPhieuMuon> ChiTietPhieuMuon { get; set; } = [];
}

public sealed class LibraryAccount
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string HoTen { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? SoDienThoai { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = [];
    public int FailedLoginAttempts { get; set; }
    public DateTimeOffset? LockoutEndUtc { get; set; }
}