using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace quan_ly_muon_tra_sach_thu_vien.Models.ViewModels;

public sealed class LibraryProfileViewModel
{
    [Required, StringLength(120)]
    [Display(Name = "Họ tên")]
    public string HoTen { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Phone, StringLength(20)]
    [Display(Name = "Số điện thoại")]
    public string? SoDienThoai { get; set; }

    [Required, StringLength(300)]
    [Display(Name = "Địa chỉ")]
    public string DiaChi { get; set; } = string.Empty;

    // Chỉ để hiển thị; form không gửi lại hai trường này nên không được kiểm tra "bắt buộc".
    [ValidateNever]
    public string Username { get; set; } = string.Empty;

    [ValidateNever]
    public string Role { get; set; } = string.Empty;
}
