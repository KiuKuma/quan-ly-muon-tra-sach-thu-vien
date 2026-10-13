namespace quan_ly_muon_tra_sach_thu_vien.Models.ViewModels;

public sealed class AccountSummaryViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? SoDienThoai { get; set; }
    public string Role { get; set; } = string.Empty;
}