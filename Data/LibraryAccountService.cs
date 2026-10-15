using Microsoft.AspNetCore.Identity;
using quan_ly_muon_tra_sach_thu_vien.Models;
using quan_ly_muon_tra_sach_thu_vien.Models.ViewModels;

namespace quan_ly_muon_tra_sach_thu_vien.Data;

public sealed class LibraryAccountService(
    LibraryFileStore store,
    IPasswordHasher<LibraryAccount> passwordHasher)
{
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private static readonly (string Username, string PasswordHash, string Role, string Name, string Email,
        string Phone, string Address)[] DefaultAccounts =
    [
        ("admin", "AQAAAAIAAYagAAAAEPKlpGZ+bqDia1ra+EWdpHxMYIwP7FrFkyUzKz6ggoU0dbVIa2d9y1r7d0Bm4dq+ag==",
            LibraryRoles.QuanLy, "Nguyễn Minh Anh", "minh.anh@example.test",
            "0901234501", "12 Đường Hoa Phượng, Quận 1"),
        ("nhanvien1", "AQAAAAIAAYagAAAAEHny1VxBaHZO34xxy1RppzmMKX3YF8uW86XyGoEx7tRuFSW0joEnQw8gH/uMiu12cg==",
            LibraryRoles.NhanVien, "Trần Gia Hân", "gia.han@example.test",
            "0901234502", "28 Đường Ánh Dương, Quận 3"),
        ("nhanvien2", "AQAAAAIAAYagAAAAEIt5wtdiI8295/SF+hysP4fvrVIh2OrRz/BRkRUACFIaaDxIvKja6m9o0wO+CkfG8g==",
            LibraryRoles.NhanVien, "Lê Quốc Bảo", "quoc.bao@example.test",
            "0901234503", "45 Đường Bình Minh, Quận 5"),
        ("nhanvien3", "AQAAAAIAAYagAAAAEL9/UdfRzFWo2SLTiRYIVI8EePkOleZoZbITShbYqK9mwnYU7x5VpB1Mz1XhfE6hrw==",
            LibraryRoles.NhanVien, "Phạm Thu Hà", "thu.ha@example.test",
            "0901234504", "73 Đường Mùa Xuân, Quận 7")
    ];

    public Task EnsureDefaultAccountsAsync()
    {
        return store.UpdateAsync(data =>
        {
            var existingAccounts = data.Accounts
                .Where(account => DefaultAccounts.Any(seed =>
                    seed.Username.Equals(account.Username, StringComparison.OrdinalIgnoreCase)))
                .GroupBy(account => account.Username, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
            var existingProfiles = data.Profiles
                .GroupBy(profile => profile.AccountId, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            var provisionedAccounts = new List<LibraryAccount>(DefaultAccounts.Length);
            var provisionedProfiles = new List<LibraryUserProfile>(DefaultAccounts.Length);

            foreach (var seed in DefaultAccounts)
            {
                if (!existingAccounts.TryGetValue(seed.Username, out var account))
                {
                    account = new LibraryAccount
                    {
                        Username = seed.Username,
                        PasswordHash = seed.PasswordHash
                    };
                }

                account.Username = seed.Username;
                account.Roles = [seed.Role];
                if (string.IsNullOrEmpty(account.PasswordHash))
                {
                    account.PasswordHash = seed.PasswordHash;
                }

                existingProfiles.TryGetValue(account.Id, out var profile);
                if (profile is null)
                {
                    profile = new LibraryUserProfile
                    {
                        AccountId = account.Id,
                        HoTen = seed.Name,
                        Email = seed.Email,
                        SoDienThoai = seed.Phone,
                        DiaChi = seed.Address
                    };
                }

                provisionedProfiles.Add(profile);
                provisionedAccounts.Add(account);
                EnsureStaffRecord(data, account, profile);
            }

            data.Accounts = provisionedAccounts;
            data.Profiles = provisionedProfiles;
            return true;
        });
    }

    public Task<LoginResult> LoginAsync(string username, string password)
    {
        return store.UpdateAsync(data =>
        {
            var account = data.Accounts.FirstOrDefault(item =>
                item.Username.Equals(username.Trim(), StringComparison.OrdinalIgnoreCase));
            if (account is null)
            {
                return new LoginResult(LoginStatus.InvalidCredentials, null);
            }

            if (account.LockoutEndUtc is DateTimeOffset lockoutEnd && lockoutEnd > DateTimeOffset.UtcNow)
            {
                return new LoginResult(LoginStatus.LockedOut, null);
            }

            var verification = passwordHasher.VerifyHashedPassword(account, account.PasswordHash, password);
            if (verification == PasswordVerificationResult.Failed)
            {
                account.FailedLoginAttempts++;
                if (account.FailedLoginAttempts >= MaxFailedAttempts)
                {
                    account.FailedLoginAttempts = 0;
                    account.LockoutEndUtc = DateTimeOffset.UtcNow.Add(LockoutDuration);
                    return new LoginResult(LoginStatus.LockedOut, null);
                }

                return new LoginResult(LoginStatus.InvalidCredentials, null);
            }

            if (verification == PasswordVerificationResult.SuccessRehashNeeded)
            {
                account.PasswordHash = passwordHasher.HashPassword(account, password);
            }

            account.FailedLoginAttempts = 0;
            account.LockoutEndUtc = null;
            return new LoginResult(LoginStatus.Success, account);
        });
    }

    public Task<ProfileUpdateResult> UpdateProfileAsync(string accountId, LibraryProfileViewModel model)
    {
        return store.UpdateAsync(data =>
        {
            var account = data.Accounts.FirstOrDefault(item => item.Id == accountId);
            if (account is null)
            {
                return ProfileUpdateResult.NotFound;
            }

            var profile = data.Profiles.FirstOrDefault(item => item.AccountId == accountId);
            if (profile is null)
            {
                return ProfileUpdateResult.NotFound;
            }

            var email = model.Email.Trim();
            if (data.Profiles.Any(item => item.AccountId != accountId &&
                    item.Email.Equals(email, StringComparison.OrdinalIgnoreCase)) ||
                data.DocGia.Any(reader => reader.Email.Equals(email, StringComparison.OrdinalIgnoreCase) &&
                    reader.IdentityUserId != accountId))
            {
                return ProfileUpdateResult.DuplicateEmail;
            }

            profile.HoTen = model.HoTen.Trim();
            profile.Email = email;
            profile.SoDienThoai = model.SoDienThoai?.Trim();
            profile.DiaChi = model.DiaChi.Trim();

            var staff = data.NhanVien.FirstOrDefault(item => item.IdentityUserId == accountId);
            if (staff is not null)
            {
                staff.HoTen = profile.HoTen;
                staff.Email = profile.Email;
                staff.SoDienThoai = profile.SoDienThoai;
            }

            var reader = data.DocGia.FirstOrDefault(item => item.IdentityUserId == accountId);
            if (reader is not null)
            {
                reader.HoTen = profile.HoTen;
                reader.Email = profile.Email;
                reader.SoDienThoai = profile.SoDienThoai;
                reader.DiaChi = profile.DiaChi;
            }

            return ProfileUpdateResult.Success;
        });
    }

    private static void EnsureStaffRecord(
        LibraryData data,
        LibraryAccount account,
        LibraryUserProfile profile)
    {
        var staff = data.NhanVien.FirstOrDefault(item => item.IdentityUserId == account.Id);
        if (staff is null)
        {
            data.NhanVien.Add(new NhanVien
            {
                Id = data.NhanVien.Select(item => item.Id).DefaultIfEmpty(0).Max() + 1,
                HoTen = profile.HoTen,
                Email = profile.Email,
                SoDienThoai = profile.SoDienThoai,
                IdentityUserId = account.Id
            });
            return;
        }

        staff.HoTen = profile.HoTen;
        staff.Email = profile.Email;
        staff.SoDienThoai = profile.SoDienThoai;
    }
}

public enum LoginStatus
{
    Success,
    InvalidCredentials,
    LockedOut
}

public enum ProfileUpdateResult
{
    Success,
    NotFound,
    DuplicateEmail
}

public sealed record LoginResult(LoginStatus Status, LibraryAccount? Account);
