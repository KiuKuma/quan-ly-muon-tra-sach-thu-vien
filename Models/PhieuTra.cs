using System.Text.Json.Serialization;

namespace quan_ly_muon_tra_sach_thu_vien.Models;

public sealed class PhieuTra
{
    public int Id { get; set; }
    public int PhieuMuonId { get; set; }
    [JsonIgnore]
    public PhieuMuon? PhieuMuon { get; set; }
    public int NhanVienId { get; set; }
    [JsonIgnore]
    public NhanVien? NhanVien { get; set; }
    public string? TenTaiKhoanNhanVien { get; set; }
    public DateTime NgayTra { get; set; } = DateTime.UtcNow;
    public decimal TienPhatDaThanhToan { get; set; }
    public DateTime? NgayThanhToanTienPhat { get; set; }
    public string? TenTaiKhoanThuTienPhat { get; set; }
}
