using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using quan_ly_muon_tra_sach_thu_vien.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<LibraryFileStore>();
builder.Services.AddSingleton<LibrarySampleDataSeeder>();
builder.Services.AddSingleton<LibraryAccountService>();
builder.Services.AddSingleton<IPasswordHasher<LibraryAccount>, PasswordHasher<LibraryAccount>>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.SlidingExpiration = true;
    options.Events.OnValidatePrincipal = async context =>
    {
        var accountId = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var store = context.HttpContext.RequestServices.GetRequiredService<LibraryFileStore>();
        var account = string.IsNullOrWhiteSpace(accountId)
            ? null
            : await store.ReadAsync(data => data.Accounts.FirstOrDefault(item => item.Id == accountId));

        if (account is null)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return;
        }

        var currentRoles = context.Principal!.FindAll(ClaimTypes.Role)
            .Select(claim => claim.Value)
            .OrderBy(role => role, StringComparer.Ordinal)
            .ToArray();
        var savedRoles = account.Roles
            .OrderBy(role => role, StringComparer.Ordinal)
            .ToArray();
        var currentName = context.Principal.FindFirst(ClaimTypes.Name)?.Value;
        var currentEmail = context.Principal.FindFirst(ClaimTypes.Email)?.Value;

        if (currentRoles.SequenceEqual(savedRoles, StringComparer.Ordinal) &&
            currentName == account.HoTen && currentEmail == account.Email)
        {
            return;
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, account.Id),
            new(ClaimTypes.Name, account.HoTen),
            new(ClaimTypes.Email, account.Email)
        };
        claims.AddRange(savedRoles.Select(role => new Claim(ClaimTypes.Role, role)));
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        context.ReplacePrincipal(new ClaimsPrincipal(identity));
        context.ShouldRenew = true;
    };
});
builder.Services.AddAuthorization();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

await LibraryDataInitializer.InitializeAsync(
    app.Services.GetRequiredService<LibraryFileStore>(),
    app.Services.GetRequiredService<LibrarySampleDataSeeder>(),
    app.Services.GetRequiredService<LibraryAccountService>(),
    app.Configuration,
    app.Logger);

app.Run();
