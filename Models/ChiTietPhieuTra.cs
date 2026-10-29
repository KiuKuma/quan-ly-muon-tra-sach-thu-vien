using System.Text.Json.Serialization;

namespace quan_ly_muon_tra_sach_thu_vien.Models;

public sealed class ChiTietPhieuTra
{
    public int Id { get; set; }
    public int PhieuTraId { get; set; }
    [JsonIgnore]
    public PhieuTra? PhieuTra { get; set; }
    public int ChiTietPhieuMuonId { get; set; }
    [JsonIgnore]
    public ChiTietPhieuMuon? ChiTietPhieuMuon { get; set; }
    public int SoLuongTra { get; set; }
    public decimal TienPhat { get; set; }
    public string TinhTrang { get; set; } = "Tot";
    public string? GhiChu { get; set; }
}
