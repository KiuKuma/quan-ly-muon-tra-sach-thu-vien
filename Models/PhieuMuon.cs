using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace quan_ly_muon_tra_sach_thu_vien.Models;

public sealed class PhieuMuon
{
    public int Id { get; set; }
    public int DocGiaId { get; set; }
    [JsonIgnore]
    public DocGia? DocGia { get; set; }
    public int NhanVienId { get; set; }
    [JsonIgnore]
    public NhanVien? NhanVien { get; set; }

    [Display(Name = "Ngày mượn")]
    public DateTime NgayMuon { get; set; } = DateTime.UtcNow;

    [Display(Name = "Hạn trả")]
    public DateTime HanTra { get; set; } = DateTime.UtcNow.AddDays(14);

    [StringLength(30)]
    public string TrangThai { get; set; } = "DangMuon";

    [JsonIgnore]
    public ICollection<ChiTietPhieuMuon> ChiTietPhieuMuon { get; set; } = new List<ChiTietPhieuMuon>();
}