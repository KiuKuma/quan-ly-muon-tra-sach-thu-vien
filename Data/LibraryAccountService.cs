using Microsoft.AspNetCore.Identity;
using System.Security.Cryptography;
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
            // Chỉ dựng bốn tài khoản mặc định ở lần chạy đầu (chưa có tài khoản nào), hoặc dựng lại
            // tài khoản Quản lý nếu hệ thống không còn Quản lý nào. Nhờ vậy tài khoản đã bị xóa không
            // tự quay lại và tài khoản tạo thêm không bị mất mỗi lần khởi động.
            var firstRun = data.Accounts.Count == 0;
            var hasManager = data.Accounts.Any(account =>
                account.IsActive && account.Roles.Contains(LibraryRoles.QuanLy));
            var seeds = DefaultAccounts
                .Where(seed => firstRun || (!hasManager && seed.Role == LibraryRoles.QuanLy))
                .ToArray();

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

            foreach (var seed in seeds)
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
                if (!hasManager && seed.Role == LibraryRoles.QuanLy)
                {
                    account.IsActive = true;
                }
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

            foreach (var account in provisionedAccounts.Where(item => !data.Accounts.Contains(item)))
            {
                data.Accounts.Add(account);
            }

            foreach (var profile in provisionedProfiles.Where(item => !data.Profiles.Contains(item)))
            {
                data.Profiles.Add(profile);
            }

            // Tài khoản mặc định bị mất hồ sơ thì dựng lại hồ sơ; mọi tài khoản có hồ sơ đều có bản ghi nhân viên.
            foreach (var account in data.Accounts)
            {
                var profile = data.Profiles.FirstOrDefault(item => item.AccountId == account.Id);
                var seed = DefaultAccounts.FirstOrDefault(item =>
                    item.Username.Equals(account.Username, StringComparison.OrdinalIgnoreCase));
                if (profile is null && seed.Username is not null)
                {
                    profile = new LibraryUserProfile
                    {
                        AccountId = account.Id,
                        HoTen = seed.Name,
                        Email = seed.Email,
                        SoDienThoai = seed.Phone,
                        DiaChi = seed.Address
                    };
                    data.Profiles.Add(profile);
                }

                if (profile is not null)
                {
                    EnsureStaffRecord(data, account, profile);
                }
            }

            return true;
        });
    }

    public Task<StaffAccountRegistrationResult> RegisterStaffAccountAsync(RegisterStaffAccountViewModel model)
    {
        return store.UpdateAsync(data =>
        {
            var username = model.Username.Trim();
            var email = model.Email.Trim();
            if (data.Accounts.Any(account =>
                    account.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
            {
                return StaffAccountRegistrationResult.DuplicateUsername;
            }

            if (data.Profiles.Any(profile =>
                    profile.Email.Equals(email, StringComparison.OrdinalIgnoreCase)) ||
                data.DocGia.Any(reader =>
                    reader.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
            {
                return StaffAccountRegistrationResult.DuplicateEmail;
            }

            var account = new LibraryAccount
            {
                Username = username,
                Roles = [LibraryRoles.NhanVien]
            };
            account.PasswordHash = passwordHasher.HashPassword(account, model.Password);

            var profile = new LibraryUserProfile
            {
                AccountId = account.Id,
                HoTen = model.HoTen.Trim(),
                Email = email,
                SoDienThoai = model.SoDienThoai?.Trim(),
                DiaChi = model.DiaChi.Trim()
            };
            var staff = new NhanVien
            {
                Id = data.NhanVien.Select(item => item.Id).DefaultIfEmpty(0).Max() + 1,
                HoTen = profile.HoTen,
                Email = profile.Email,
                SoDienThoai = profile.SoDienThoai,
                IdentityUserId = account.Id
            };

            data.Accounts.Add(account);
            data.Profiles.Add(profile);
            data.NhanVien.Add(staff);
            return StaffAccountRegistrationResult.Success;
        });
    }

    public Task<AccountDeleteResult> DeleteAccountAsync(string accountId, string? currentAccountId)
    {
        return store.UpdateAsync(data =>
        {
            var account = data.Accounts.FirstOrDefault(item => item.Id == accountId);
            if (account is null)
            {
                return AccountDeleteResult.NotFound;
            }

            if (accountId == currentAccountId)
            {
                return AccountDeleteResult.CannotDeleteSelf;
            }

            if (account.Roles.Contains(LibraryRoles.QuanLy) &&
                account.IsActive && !data.Accounts.Any(item => item.Id != accountId && item.IsActive &&
                    item.Roles.Contains(LibraryRoles.QuanLy)))
            {
                return AccountDeleteResult.LastManager;
            }

            var staff = data.NhanVien.FirstOrDefault(item => item.IdentityUserId == accountId);
            if (staff is not null && data.PhieuMuon.Any(loan => loan.NhanVienId == staff.Id))
            {
                return AccountDeleteResult.HasLoanHistory;
            }

            if (staff is not null)
            {
                data.NhanVien.Remove(staff);
            }

            data.Profiles.RemoveAll(item => item.AccountId == accountId);
            data.Accounts.Remove(account);
            foreach (var reader in data.DocGia.Where(item => item.IdentityUserId == accountId))
            {
                reader.IdentityUserId = null;
            }

            return AccountDeleteResult.Success;
        });
    }

    public Task<AccountStatusChangeResult> SetAccountActiveAsync(
        string accountId,
        string? currentAccountId,
        bool isActive)
    {
        return store.UpdateAsync(data =>
        {
            var account = data.Accounts.FirstOrDefault(item => item.Id == accountId);
            if (account is null)
            {
                return AccountStatusChangeResult.NotFound;
            }

            if (!isActive && accountId == currentAccountId)
            {
                return AccountStatusChangeResult.CannotDisableSelf;
            }

            if (!isActive && account.Roles.Contains(LibraryRoles.QuanLy) &&
                !data.Accounts.Any(item => item.Id != accountId && item.IsActive &&
                    item.Roles.Contains(LibraryRoles.QuanLy)))
            {
                return AccountStatusChangeResult.LastActiveManager;
            }

            account.IsActive = isActive;
            return AccountStatusChangeResult.Success;
        });
    }

    public Task<AccountPasswordResetResult> ResetPasswordAsync(string accountId)
    {
        return store.UpdateAsync(data =>
        {
            var account = data.Accounts.FirstOrDefault(item => item.Id == accountId);
            if (account is null)
            {
                return new AccountPasswordResetResult(AccountPasswordResetStatus.NotFound, null);
            }

            var temporaryPassword = Convert.ToBase64String(RandomNumberGenerator.GetBytes(18))
                .TrimEnd('=').Replace('+', '-').Replace('/', '_');
            account.PasswordHash = passwordHasher.HashPassword(account, temporaryPassword);
            account.FailedLoginAttempts = 0;
            account.LockoutEndUtc = null;
            return new AccountPasswordResetResult(AccountPasswordResetStatus.Success, temporaryPassword);
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

            if (!account.IsActive)
            {
                return new LoginResult(LoginStatus.Disabled, null);
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
    LockedOut,
    Disabled
}

public enum ProfileUpdateResult
{
    Success,
    NotFound,
    DuplicateEmail
}

public enum StaffAccountRegistrationResult
{
    Success,
    DuplicateUsername,
    DuplicateEmail
}

public enum AccountDeleteResult
{
    Success,
    NotFound,
    CannotDeleteSelf,
    LastManager,
    HasLoanHistory
}

public enum AccountStatusChangeResult
{
    Success,
    NotFound,
    CannotDisableSelf,
    LastActiveManager
}

public enum AccountPasswordResetStatus
{
    Success,
    NotFound
}

public sealed record AccountPasswordResetResult(
    AccountPasswordResetStatus Status,
    string? TemporaryPassword);

public sealed record LoginResult(LoginStatus Status, LibraryAccount? Account);
