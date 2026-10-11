using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace quan_ly_muon_tra_sach_thu_vien.Models;

public sealed class TheLoai
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    [Display(Name = "Tên thể loại")]
    public string TenTheLoai { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Mô tả")]
    public string? MoTa { get; set; }

    [JsonIgnore]
    public ICollection<Sach> Sach { get; set; } = new List<Sach>();
}