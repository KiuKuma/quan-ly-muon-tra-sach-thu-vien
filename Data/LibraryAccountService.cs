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

    public Task<RegisterResult> RegisterReaderAsync(RegisterViewModel model)
    {
        return store.UpdateAsync(data =>
        {
            var email = model.Email.Trim();
            if (data.Accounts.Any(account => NormalizeEmail(account.Email) == NormalizeEmail(email)) ||
                data.DocGia.Any(reader => NormalizeEmail(reader.Email) == NormalizeEmail(email)))
            {
                return RegisterResult.DuplicateEmail;
            }

            var account = new LibraryAccount
            {
                HoTen = model.HoTen.Trim(),
                Email = email,
                SoDienThoai = model.SoDienThoai?.Trim(),
                Roles = [LibraryRoles.DocGia]
            };
            account.PasswordHash = passwordHasher.HashPassword(account, model.Password);
            data.Accounts.Add(account);
            data.DocGia.Add(new DocGia
            {
                Id = NextId(data.DocGia.Select(reader => reader.Id)),
                HoTen = account.HoTen,
                Email = email,
                SoDienThoai = account.SoDienThoai,
                DiaChi = model.DiaChi.Trim(),
                IdentityUserId = account.Id,
                NgayDangKy = DateTime.UtcNow
            });
            return RegisterResult.Success;
        });
    }

    public Task<LoginResult> LoginAsync(string email, string password)
    {
        return store.UpdateAsync(data =>
        {
            var normalizedEmail = NormalizeEmail(email);
            var account = data.Accounts.FirstOrDefault(item => NormalizeEmail(item.Email) == normalizedEmail);
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

    public Task<bool> EnsureLibrarianAsync(string email, string password)
    {
        return store.UpdateAsync(data =>
        {
            var normalizedEmail = NormalizeEmail(email);
            var account = data.Accounts.FirstOrDefault(item => NormalizeEmail(item.Email) == normalizedEmail);
            if (account is null)
            {
                account = new LibraryAccount
                {
                    HoTen = "Thủ thư quản trị",
                    Email = email.Trim(),
                    Roles = [LibraryRoles.ThuThu]
                };
                account.PasswordHash = passwordHasher.HashPassword(account, password);
                data.Accounts.Add(account);
            }
            else if (!account.Roles.Contains(LibraryRoles.ThuThu))
            {
                account.Roles.Add(LibraryRoles.ThuThu);
            }

            if (!data.NhanVien.Any(staff => staff.IdentityUserId == account.Id))
            {
                data.NhanVien.Add(new NhanVien
                {
                    Id = NextId(data.NhanVien.Select(staff => staff.Id)),
                    HoTen = account.HoTen,
                    Email = account.Email,
                    SoDienThoai = account.SoDienThoai,
                    IdentityUserId = account.Id
                });
            }

            return true;
        });
    }

    public Task EnsureInitialAdministratorAsync(string email)
    {
        return store.UpdateAsync(data =>
        {
            if (data.Accounts.Any(item => item.Roles.Contains(LibraryRoles.QuanTriVien)))
            {
                return true;
            }

            var account = data.Accounts.FirstOrDefault(item =>
                NormalizeEmail(item.Email) == NormalizeEmail(email));
            if (account is null)
            {
                return false;
            }

            account.Roles.Clear();
            account.Roles.Add(LibraryRoles.QuanTriVien);
            EnsureStaffRecord(data, account);
            return true;
        });
    }

    public Task<RoleChangeResult> SetRoleAsync(string accountId, string role)
    {
        if (role is not (LibraryRoles.QuanTriVien or LibraryRoles.ThuThu or LibraryRoles.DocGia))
        {
            return Task.FromResult(RoleChangeResult.InvalidRole);
        }

        return store.UpdateAsync(data =>
        {
            var account = data.Accounts.FirstOrDefault(item => item.Id == accountId);
            if (account is null)
            {
                return RoleChangeResult.NotFound;
            }

            var staff = data.NhanVien.FirstOrDefault(item => item.IdentityUserId == accountId);
            var hadAdministratorRole = account.Roles.Contains(LibraryRoles.QuanTriVien);
            var hadStaffRole = account.Roles.Contains(LibraryRoles.ThuThu);

            if (hadAdministratorRole && role != LibraryRoles.QuanTriVien &&
                data.Accounts.Count(item => item.Roles.Contains(LibraryRoles.QuanTriVien)) <= 1)
            {
                return RoleChangeResult.LastAdministrator;
            }

            if (hadStaffRole && role == LibraryRoles.DocGia && staff is not null &&
                data.PhieuMuon.Any(loan => loan.NhanVienId == staff.Id))
            {
                return RoleChangeResult.HasLoans;
            }

            account.Roles.Clear();
            account.Roles.Add(role);

            if (role is LibraryRoles.QuanTriVien or LibraryRoles.ThuThu)
            {
                EnsureStaffRecord(data, account);
            }
            else if (staff is not null)
            {
                data.NhanVien.Remove(staff);
            }

            return RoleChangeResult.Success;
        });
    }

    private static void EnsureStaffRecord(LibraryData data, LibraryAccount account)
    {
        if (data.NhanVien.Any(staff => staff.IdentityUserId == account.Id))
        {
            return;
        }

        data.NhanVien.Add(new NhanVien
        {
            Id = NextId(data.NhanVien.Select(staff => staff.Id)),
            HoTen = account.HoTen,
            Email = account.Email,
            SoDienThoai = account.SoDienThoai,
            IdentityUserId = account.Id
        });
    }

    public static string NormalizeEmail(string email) => email.Trim().ToUpperInvariant();

    private static int NextId(IEnumerable<int> ids) => ids.DefaultIfEmpty(0).Max() + 1;
}

public enum RegisterResult
{
    Success,
    DuplicateEmail
}

public enum LoginStatus
{
    Success,
    InvalidCredentials,
    LockedOut
}

public enum RoleChangeResult
{
    Success,
    NotFound,
    LastAdministrator,
    InvalidRole,
    HasLoans
}

public sealed record LoginResult(LoginStatus Status, LibraryAccount? Account);