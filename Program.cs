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
        var accountData = string.IsNullOrWhiteSpace(accountId)
            ? (Account: (LibraryAccount?)null, Profile: (LibraryUserProfile?)null)
            : await store.ReadAsync(data =>
            {
                var account = data.Accounts.FirstOrDefault(item => item.Id == accountId);
                var profile = data.Profiles.FirstOrDefault(item => item.AccountId == accountId);
                return (Account: account, Profile: profile);
            });

        if (accountData.Account is null || accountData.Profile is null)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return;
        }

        var currentRoles = context.Principal!.FindAll(ClaimTypes.Role)
            .Select(claim => claim.Value)
            .OrderBy(role => role, StringComparer.Ordinal)
            .ToArray();
        var savedRoles = accountData.Account.Roles
            .OrderBy(role => role, StringComparer.Ordinal)
            .ToArray();
        var currentName = context.Principal.FindFirst(ClaimTypes.Name)?.Value;
        var currentEmail = context.Principal.FindFirst(ClaimTypes.Email)?.Value;

        if (currentRoles.SequenceEqual(savedRoles, StringComparer.Ordinal) &&
            currentName == accountData.Account.Username && currentEmail == accountData.Profile.Email)
        {
            return;
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, accountData.Account.Id),
            new(ClaimTypes.Name, accountData.Account.Username),
            new(ClaimTypes.Email, accountData.Profile.Email)
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
    app.Logger);

app.Run();
