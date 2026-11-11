using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using quan_ly_muon_tra_sach_thu_vien.Data;
using quan_ly_muon_tra_sach_thu_vien.Models.ViewModels;

namespace quan_ly_muon_tra_sach_thu_vien.Controllers;

public sealed class AccountController(
    LibraryAccountService accounts,
    LibraryFileStore store) : Controller
{
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null) => View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await accounts.LoginAsync(model.Username, model.Password);
        if (result.Status == LoginStatus.Success && result.Account is not null)
        {
            var profile = await store.ReadAsync(data =>
                data.Profiles.FirstOrDefault(item => item.AccountId == result.Account.Id));
            if (profile is null)
            {
                throw new InvalidDataException($"Tài khoản {result.Account.Username} chưa có hồ sơ người dùng.");
            }

            await SignInAsync(result.Account.Id, result.Account.Username,
                profile.Email, result.Account.Roles, model.RememberMe);
            return LocalRedirect(model.ReturnUrl is not null && Url.IsLocalUrl(model.ReturnUrl)
                ? model.ReturnUrl
                : Url.Action("Index", "Home")!);
        }

        ModelState.AddModelError(string.Empty,
            result.Status == LoginStatus.LockedOut
                ? "Tài khoản tạm khóa do đăng nhập sai nhiều lần. Vui lòng thử lại sau 15 phút."
                : "Tên tài khoản hoặc mật khẩu không chính xác.");
        return View(model);
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Profile()
    {
        var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var profileData = await store.ReadAsync(data =>
        {
            var account = data.Accounts.FirstOrDefault(item => item.Id == accountId);
            var profile = data.Profiles.FirstOrDefault(item => item.AccountId == accountId);
            return (Account: account, Profile: profile);
        });
        if (profileData.Account is null || profileData.Profile is null)
        {
            return NotFound();
        }

        return View(new LibraryProfileViewModel
        {
            Username = profileData.Account.Username,
            HoTen = profileData.Profile.HoTen,
            Email = profileData.Profile.Email,
            SoDienThoai = profileData.Profile.SoDienThoai,
            DiaChi = profileData.Profile.DiaChi,
            Role = profileData.Account.Roles.FirstOrDefault() ?? string.Empty
        });
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(LibraryProfileViewModel model)
    {
        var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(accountId))
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            await PopulateProfileIdentityAsync(model, accountId);
            return View(model);
        }

        var result = await accounts.UpdateProfileAsync(accountId, model);
        if (result == ProfileUpdateResult.DuplicateEmail)
        {
            ModelState.AddModelError(nameof(model.Email), "Email này đã được sử dụng.");
            await PopulateProfileIdentityAsync(model, accountId);
            return View(model);
        }

        if (result == ProfileUpdateResult.NotFound)
        {
            return NotFound();
        }

        TempData["Success"] = "Đã cập nhật hồ sơ người dùng.";
        return RedirectToAction(nameof(Profile));
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied() => View();

    private async Task PopulateProfileIdentityAsync(LibraryProfileViewModel model, string accountId)
    {
        var account = await store.ReadAsync(data =>
            data.Accounts.FirstOrDefault(item => item.Id == accountId));
        model.Username = account?.Username ?? string.Empty;
        model.Role = account?.Roles.FirstOrDefault() ?? string.Empty;
    }

    private async Task SignInAsync(
        string id,
        string name,
        string email,
        IEnumerable<string> roles,
        bool isPersistent)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, id),
            new(ClaimTypes.Name, name),
            new(ClaimTypes.Email, email)
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = isPersistent });
    }
}