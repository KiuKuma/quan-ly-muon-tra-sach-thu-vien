using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace quan_ly_muon_tra_sach_thu_vien.Models;

public sealed class Sach
{
    public int Id { get; set; }

    [Required, StringLength(200)]
    [Display(Name = "Tên sách")]
    public string TenSach { get; set; } = string.Empty;

    [Required, StringLength(120)]
    [Display(Name = "Tác giả")]
    public string TacGia { get; set; } = string.Empty;

    [StringLength(160)]
    [Display(Name = "Nhà xuất bản")]
    public string? NhaXuatBan { get; set; }

    [Range(1450, 2200)]
    [Display(Name = "Năm xuất bản")]
    public int? NamXuatBan { get; set; }

    [Range(0, 100000)]
    [Display(Name = "Số lượng")]
    public int SoLuong { get; set; }

    [Display(Name = "Thể loại")]
    public int? TheLoaiId { get; set; }
    [JsonIgnore]
    public TheLoai? TheLoai { get; set; }

    [JsonIgnore]
    public ICollection<ChiTietPhieuMuon> ChiTietPhieuMuon { get; set; } = new List<ChiTietPhieuMuon>();
}