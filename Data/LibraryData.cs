using quan_ly_muon_tra_sach_thu_vien.Models;

namespace quan_ly_muon_tra_sach_thu_vien.Data;

public sealed class LibraryData
{
    public List<LibraryAccount> Accounts { get; set; } = [];
    public List<LibraryUserProfile> Profiles { get; set; } = [];
    public List<DocGia> DocGia { get; set; } = [];
    public List<TheLoai> TheLoai { get; set; } = [];
    public List<Sach> Sach { get; set; } = [];
    public List<NhanVien> NhanVien { get; set; } = [];
    public List<PhieuMuon> PhieuMuon { get; set; } = [];
    public List<ChiTietPhieuMuon> ChiTietPhieuMuon { get; set; } = [];
    public List<PhieuTra> PhieuTra { get; set; } = [];
    public List<ChiTietPhieuTra> ChiTietPhieuTra { get; set; } = [];
}

public sealed class LibraryAccount
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = [];
    public int FailedLoginAttempts { get; set; }
    public DateTimeOffset? LockoutEndUtc { get; set; }
}

public sealed class LibraryUserProfile
{
    public string AccountId { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? SoDienThoai { get; set; }
    public string DiaChi { get; set; } = string.Empty;
}