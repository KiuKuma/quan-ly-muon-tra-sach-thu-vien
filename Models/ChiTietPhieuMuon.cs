using System.Text.Json.Serialization;

namespace quan_ly_muon_tra_sach_thu_vien.Models;

public sealed class ChiTietPhieuMuon
{
    public int Id { get; set; }
    public int PhieuMuonId { get; set; }
    [JsonIgnore]
    public PhieuMuon? PhieuMuon { get; set; }
    public int SachId { get; set; }
    [JsonIgnore]
    public Sach? Sach { get; set; }
    public int SoLuong { get; set; } = 1;
}