using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using quan_ly_muon_tra_sach_thu_vien.Data;
using quan_ly_muon_tra_sach_thu_vien.Models.ViewModels;

namespace quan_ly_muon_tra_sach_thu_vien.Controllers;

[Authorize(Roles = LibraryRoles.QuanLy)]
public sealed class TaiKhoanController(
    LibraryFileStore store,
    LibraryAccountService accounts) : Controller
{
    public async Task<IActionResult> Index(int page = 1)
    {
        var viewModels = await store.ReadAsync(data => data.Accounts
            .Where(account => account.Roles.Contains(LibraryRoles.NhanVien))
            .OrderBy(account => account.Username)
            .Select(account =>
            {
                var profile = data.Profiles.FirstOrDefault(item => item.AccountId == account.Id);
                return new AccountSummaryViewModel
                {
                    Id = account.Id,
                    Username = account.Username,
                    HoTen = profile?.HoTen ?? string.Empty,
                    Email = profile?.Email ?? string.Empty,
                    SoDienThoai = profile?.SoDienThoai,
                    DiaChi = profile?.DiaChi ?? string.Empty,
                    Role = account.Roles.FirstOrDefault() ?? string.Empty,
                    IsActive = account.IsActive
                };
            })
            .ToList());
        return View(PagedResult<AccountSummaryViewModel>.Create(viewModels, page));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(string id)
    {
        if (!await IsStaffAccountAsync(id))
        {
            return NotFound();
        }

        var result = await accounts.ResetPasswordAsync(id);
        if (result.Status == AccountPasswordResetStatus.NotFound || result.TemporaryPassword is null)
        {
            TempData["Error"] = "Không tìm thấy tài khoản nhân viên.";
        }
        else
        {
            TempData["TemporaryPassword"] = result.TemporaryPassword;
            TempData["Success"] = "Đã tạo mật khẩu tạm. Hãy sao chép và gửi riêng cho nhân viên; mật khẩu chỉ hiển thị lần này.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetActive(string id, bool isActive)
    {
        if (!await IsStaffAccountAsync(id))
        {
            return NotFound();
        }

        var result = await accounts.SetAccountActiveAsync(
            id, User.FindFirstValue(ClaimTypes.NameIdentifier), isActive);
        TempData[result == AccountStatusChangeResult.Success ? "Success" : "Error"] = result switch
        {
            AccountStatusChangeResult.Success => isActive
                ? "Đã kích hoạt tài khoản nhân viên."
                : "Đã khóa tài khoản nhân viên.",
            AccountStatusChangeResult.NotFound => "Không tìm thấy tài khoản nhân viên.",
            AccountStatusChangeResult.CannotDisableSelf => "Không thể khóa tài khoản đang đăng nhập.",
            AccountStatusChangeResult.LastActiveManager => "Không thể khóa Quản lý đang hoạt động cuối cùng.",
            _ => "Không thể cập nhật trạng thái tài khoản."
        };
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(string id)
    {
        var model = await store.ReadAsync(data =>
        {
            var account = data.Accounts.FirstOrDefault(item =>
                item.Id == id && item.Roles.Contains(LibraryRoles.NhanVien));
            if (account is null)
            {
                return null;
            }

            var profile = data.Profiles.FirstOrDefault(item => item.AccountId == id);
            return new AccountSummaryViewModel
            {
                Id = account.Id,
                Username = account.Username,
                HoTen = profile?.HoTen ?? string.Empty,
                Email = profile?.Email ?? string.Empty,
                Role = account.Roles.FirstOrDefault() ?? string.Empty,
                IsActive = account.IsActive
            };
        });
        return model is null ? NotFound() : View(model);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        if (!await IsStaffAccountAsync(id))
        {
            return NotFound();
        }

        var result = await accounts.DeleteAccountAsync(id, User.FindFirstValue(ClaimTypes.NameIdentifier));
        switch (result)
        {
            case AccountDeleteResult.NotFound:
                return NotFound();
            case AccountDeleteResult.CannotDeleteSelf:
                TempData["Error"] = "Không thể xóa tài khoản đang đăng nhập.";
                break;
            case AccountDeleteResult.LastManager:
                TempData["Error"] = "Không thể xóa tài khoản Quản lý cuối cùng.";
                break;
            case AccountDeleteResult.HasLoanHistory:
                TempData["Error"] = "Không thể xóa nhân viên đã lập phiếu mượn hoặc phiếu trả.";
                break;
            default:
                TempData["Success"] = "Đã xóa tài khoản nhân viên.";
                break;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var model = await store.ReadAsync(data =>
        {
            var account = data.Accounts.FirstOrDefault(item =>
                item.Id == id && item.Roles.Contains(LibraryRoles.NhanVien));
            var profile = data.Profiles.FirstOrDefault(item => item.AccountId == id);
            return account is null || profile is null
                ? null
                : new LibraryProfileViewModel
                {
                    Username = account.Username,
                    Role = account.Roles.FirstOrDefault() ?? string.Empty,
                    HoTen = profile.HoTen,
                    Email = profile.Email,
                    SoDienThoai = profile.SoDienThoai,
                    DiaChi = profile.DiaChi
                };
        });
        return model is null ? NotFound() : View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, LibraryProfileViewModel model)
    {
        if (!await IsStaffAccountAsync(id))
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            await PopulateIdentityAsync(model, id);
            return View(model);
        }

        var result = await accounts.UpdateProfileAsync(id, model);
        if (result == ProfileUpdateResult.NotFound)
        {
            return NotFound();
        }

        if (result == ProfileUpdateResult.DuplicateEmail)
        {
            ModelState.AddModelError(nameof(model.Email), "Email này đã được sử dụng.");
            await PopulateIdentityAsync(model, id);
            return View(model);
        }

        TempData["Success"] = "Đã cập nhật hồ sơ nhân viên.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateIdentityAsync(LibraryProfileViewModel model, string id)
    {
        var account = await store.ReadAsync(data =>
            data.Accounts.FirstOrDefault(item => item.Id == id));
        model.Username = account?.Username ?? string.Empty;
        model.Role = account?.Roles.FirstOrDefault() ?? string.Empty;
    }

    private Task<bool> IsStaffAccountAsync(string id)
    {
        return store.ReadAsync(data => data.Accounts.Any(account =>
            account.Id == id && account.Roles.Contains(LibraryRoles.NhanVien)));
    }
}
