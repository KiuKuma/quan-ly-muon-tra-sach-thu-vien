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
                    Role = account.Roles.FirstOrDefault() ?? string.Empty
                };
            })
            .ToList());
        ViewBag.CurrentAccountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return View(PagedResult<AccountSummaryViewModel>.Create(viewModels, page));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var model = await store.ReadAsync(data =>
        {
            var account = data.Accounts.FirstOrDefault(item => item.Id == id);
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

    [HttpGet]
    public async Task<IActionResult> Delete(string id)
    {
        var model = await store.ReadAsync(data =>
        {
            var account = data.Accounts.FirstOrDefault(item => item.Id == id);
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
                SoDienThoai = profile?.SoDienThoai,
                DiaChi = profile?.DiaChi ?? string.Empty,
                Role = account.Roles.FirstOrDefault() ?? string.Empty
            };
        });
        return model is null ? NotFound() : View(model);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
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

    private async Task PopulateIdentityAsync(LibraryProfileViewModel model, string id)
    {
        var account = await store.ReadAsync(data =>
            data.Accounts.FirstOrDefault(item => item.Id == id));
        model.Username = account?.Username ?? string.Empty;
        model.Role = account?.Roles.FirstOrDefault() ?? string.Empty;
    }
}
