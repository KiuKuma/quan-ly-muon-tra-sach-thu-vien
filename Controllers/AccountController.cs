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
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await accounts.RegisterReaderAsync(model);
        if (result == RegisterResult.DuplicateEmail)
        {
            ModelState.AddModelError(nameof(model.Email), "Email này đã được đăng ký.");
            return View(model);
        }

        var email = LibraryAccountService.NormalizeEmail(model.Email);
        var account = await store.ReadAsync(data => data.Accounts.FirstOrDefault(item =>
            item.Email.Equals(email, StringComparison.OrdinalIgnoreCase)));
        if (account is null)
        {
            ModelState.AddModelError(string.Empty, "Không thể tạo tài khoản. Vui lòng thử lại.");
            return View(model);
        }

        await SignInAsync(account.Id, account.HoTen, account.Email, account.Roles, isPersistent: false);
        return RedirectToAction("Index", "Home");
    }

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

        var result = await accounts.LoginAsync(model.Email, model.Password);
        if (result.Status == LoginStatus.Success && result.Account is not null)
        {
            await SignInAsync(result.Account.Id, result.Account.HoTen,
                result.Account.Email, result.Account.Roles, model.RememberMe);
            return LocalRedirect(model.ReturnUrl is not null && Url.IsLocalUrl(model.ReturnUrl)
                ? model.ReturnUrl
                : Url.Action("Index", "Home")!);
        }

        ModelState.AddModelError(string.Empty,
            result.Status == LoginStatus.LockedOut
                ? "Tài khoản tạm khóa do đăng nhập sai nhiều lần. Vui lòng thử lại sau 15 phút."
                : "Email hoặc mật khẩu không chính xác.");
        return View(model);
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