using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using quan_ly_muon_tra_sach_thu_vien.Data;
using quan_ly_muon_tra_sach_thu_vien.Models.ViewModels;

namespace quan_ly_muon_tra_sach_thu_vien.Controllers;

[Authorize(Roles = LibraryRoles.QuanTriVien)]
public sealed class TaiKhoanController(
    LibraryFileStore store,
    LibraryAccountService accounts) : Controller
{
    public async Task<IActionResult> Index()
    {
        var viewModels = await store.ReadAsync(data => data.Accounts
            .OrderBy(account => account.HoTen)
            .Select(account => new AccountSummaryViewModel
            {
                Id = account.Id,
                HoTen = account.HoTen,
                Email = account.Email,
                Role = account.Roles.FirstOrDefault() ?? LibraryRoles.DocGia
            })
            .ToList());
        return View(viewModels);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetRole(string id, string role)
    {
        var currentAccountId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (currentAccountId == id && role != LibraryRoles.QuanTriVien)
        {
            TempData["Error"] = "Bạn không thể tự gỡ quyền Quản trị viên đang sử dụng.";
            return RedirectToAction(nameof(Index));
        }

        var result = await accounts.SetRoleAsync(id, role);
        TempData[result switch
        {
            RoleChangeResult.Success => "Success",
            _ => "Error"
        }] = result switch
        {
            RoleChangeResult.Success => $"Đã cập nhật vai trò thành {role}.",
            RoleChangeResult.NotFound => "Không tìm thấy tài khoản.",
            RoleChangeResult.LastAdministrator => "Không thể gỡ quyền Quản trị viên cuối cùng.",
            RoleChangeResult.InvalidRole => "Vai trò được chọn không hợp lệ.",
            RoleChangeResult.HasLoans => "Không thể gỡ quyền vì nhân viên này đang có phiếu mượn.",
            _ => "Không thể thay đổi quyền tài khoản."
        };
        return RedirectToAction(nameof(Index));
    }
}