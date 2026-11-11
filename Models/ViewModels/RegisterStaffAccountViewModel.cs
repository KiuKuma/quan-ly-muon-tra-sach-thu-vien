using System.ComponentModel.DataAnnotations;

namespace quan_ly_muon_tra_sach_thu_vien.Models.ViewModels;

public sealed class RegisterStaffAccountViewModel
{
    [Required, StringLength(50)]
    [Display(Name = "Tên tài khoản")]
    public string Username { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 8)]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    public string Password { get; set; } = string.Empty;

    [Required, Compare(nameof(Password), ErrorMessage = "Mật khẩu xác nhận không khớp.")]
    [DataType(DataType.Password)]
    [Display(Name = "Xác nhận mật khẩu")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required, StringLength(120)]
    [Display(Name = "Họ và tên")]
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
}
