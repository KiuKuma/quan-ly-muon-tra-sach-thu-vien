using System.ComponentModel.DataAnnotations;

namespace quan_ly_muon_tra_sach_thu_vien.Models.ViewModels;

public sealed class LoginViewModel
{
    [Required, StringLength(50)]
    [Display(Name = "Tên tài khoản")]
    public string Username { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Ghi nhớ đăng nhập")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}