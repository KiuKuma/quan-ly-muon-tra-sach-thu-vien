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
    public List<FinePayment> FinePayments { get; set; } = [];
    public List<BookInventoryAdjustment> BookInventoryAdjustments { get; set; } = [];
    public LibrarySettings Settings { get; set; } = new();
}

public sealed class LibraryAccount
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = [];
    public bool IsActive { get; set; } = true;
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

public sealed class LibrarySettings
{
    public int LoanTermDays { get; set; } = 14;
    public int MaxBooksPerReader { get; set; } = 5;
    public decimal DailyLateFine { get; set; } = 2000m;
    public decimal DamagedBookFine { get; set; } = 50000m;
    public decimal LostBookFine { get; set; } = 100000m;
    public int ReaderMinimumAge { get; set; } = 6;
    public int ReaderMaximumAge { get; set; } = 100;
    public int ReaderCardValidityMonths { get; set; } = 12;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}

public sealed class FinePayment
{
    public int Id { get; set; }
    public int ReturnId { get; set; }
    public int LoanId { get; set; }
    public int ReaderId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaidAt { get; set; } = DateTime.UtcNow;
    public string StaffUsername { get; set; } = string.Empty;
}

public sealed class BookInventoryAdjustment
{
    public int Id { get; set; }
    public int BookId { get; set; }
    public int Quantity { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Note { get; set; }
    public string StaffUsername { get; set; } = string.Empty;
    public DateTime AdjustedAt { get; set; } = DateTime.UtcNow;
}