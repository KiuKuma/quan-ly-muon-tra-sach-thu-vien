using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace quan_ly_muon_tra_sach_thu_vien.Models;

public sealed class NhanVien
{
    public int Id { get; set; }

    [Required, StringLength(120)]
    [Display(Name = "Họ tên")]
    public string HoTen { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Phone, StringLength(20)]
    [Display(Name = "Số điện thoại")]
    public string? SoDienThoai { get; set; }

    public string? IdentityUserId { get; set; }
    [JsonIgnore]
    public ICollection<PhieuMuon> PhieuMuon { get; set; } = new List<PhieuMuon>();
}