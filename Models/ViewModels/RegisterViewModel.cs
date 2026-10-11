using System.ComponentModel.DataAnnotations;

namespace quan_ly_muon_tra_sach_thu_vien.Models.ViewModels;

public sealed class RegisterViewModel
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

    [Required, DataType(DataType.Password), MinLength(8)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).+$",
        ErrorMessage = "Mật khẩu cần có chữ hoa, chữ thường, số và ký tự đặc biệt.")]
    [Display(Name = "Mật khẩu")]
    public string Password { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), Compare(nameof(Password))]
    [Display(Name = "Nhập lại mật khẩu")]
    public string ConfirmPassword { get; set; } = string.Empty;
}