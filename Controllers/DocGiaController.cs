using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using quan_ly_muon_tra_sach_thu_vien.Data;
using quan_ly_muon_tra_sach_thu_vien.Models;
using quan_ly_muon_tra_sach_thu_vien.Models.ViewModels;

namespace quan_ly_muon_tra_sach_thu_vien.Controllers;

[Authorize(Roles = LibraryRoles.NhanVien + "," + LibraryRoles.QuanLy)]
public sealed class DocGiaController(
    LibraryFileStore store) : Controller
{
    public async Task<IActionResult> Index(string? search, int page = 1)
    {
        ViewBag.Search = search;
        return View(await store.ReadAsync(data =>
        {
            var readers = data.DocGia.AsEnumerable();
            var terms = VietnameseText.SplitTerms(search);
            if (terms.Length > 0)
            {
                readers = readers.Where(reader => VietnameseText.ContainsAllTerms(
                    terms, reader.HoTen, reader.Email, reader.SoDienThoai, reader.DiaChi));
            }

            return PagedResult<DocGia>.Create(readers.Select(reader =>
            {
                reader.NgayHetHan ??= reader.NgayDangKy.AddMonths(data.Settings.ReaderCardValidityMonths);
                return reader;
            }).OrderBy(reader => reader.HoTen), page);
        }));
    }

    public async Task<IActionResult> Details(int id)
    {
        var reader = await store.ReadAsync(data =>
        {
            var item = data.DocGia.FirstOrDefault(reader => reader.Id == id);
            if (item is not null)
            {
                item.NgayHetHan ??= item.NgayDangKy.AddMonths(data.Settings.ReaderCardValidityMonths);
            }

            return item;
        });
        return reader is null ? NotFound() : View(reader);
    }

    [Authorize(Roles = LibraryRoles.NhanVien + "," + LibraryRoles.QuanLy)]
    public IActionResult Create() => View("Form", new DocGia());

    [HttpPost]
    [Authorize(Roles = LibraryRoles.NhanVien + "," + LibraryRoles.QuanLy)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("HoTen,Email,SoDienThoai,DiaChi,NgaySinh")] DocGia reader)
    {
        await ValidateEmailAsync(reader);
        await ValidateBirthDateAsync(reader.NgaySinh, required: true);
        if (!ModelState.IsValid)
        {
            return View("Form", reader);
        }

        var added = await store.UpdateAsync(data =>
        {
            if (reader.NgaySinh is not DateTime birthDate ||
                !IsAgeEligible(birthDate, data.Settings, DateTime.Today))
            {
                return false;
            }

            if (data.DocGia.Any(item => item.Email.Equals(reader.Email.Trim(), StringComparison.OrdinalIgnoreCase)) ||
                data.Profiles.Any(profile => profile.Email.Equals(reader.Email.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            reader.Id = data.DocGia.Select(item => item.Id).DefaultIfEmpty(0).Max() + 1;
            reader.HoTen = reader.HoTen.Trim();
            reader.Email = reader.Email.Trim();
            reader.SoDienThoai = reader.SoDienThoai?.Trim();
            reader.DiaChi = reader.DiaChi?.Trim();
            reader.NgayDangKy = DateTime.UtcNow;
            reader.NgayHetHan = DateTime.UtcNow.AddMonths(data.Settings.ReaderCardValidityMonths);
            data.DocGia.Add(reader);
            return true;
        });
        if (!added)
        {
            ModelState.AddModelError(string.Empty, "Email đã được sử dụng hoặc tuổi độc giả không còn hợp lệ theo quy định.");
            return View("Form", reader);
        }
        TempData["Success"] = "Đã thêm độc giả.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = LibraryRoles.NhanVien + "," + LibraryRoles.QuanLy)]
    public async Task<IActionResult> Edit(int id)
    {
        var reader = await store.ReadAsync(data => data.DocGia.FirstOrDefault(item => item.Id == id));
        return reader is null ? NotFound() : View("Form", reader);
    }

    [HttpPost]
    [Authorize(Roles = LibraryRoles.NhanVien + "," + LibraryRoles.QuanLy)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,HoTen,Email,SoDienThoai,DiaChi,NgaySinh")] DocGia reader)
    {
        if (id != reader.Id)
        {
            return NotFound();
        }

        var existing = await store.ReadAsync(data => data.DocGia.FirstOrDefault(item => item.Id == id));
        if (existing is null)
        {
            return NotFound();
        }

        await ValidateBirthDateAsync(reader.NgaySinh, required: existing.NgaySinh is not null);
        if (!ModelState.IsValid)
        {
            reader.NgayDangKy = existing.NgayDangKy;
            reader.NgayHetHan = existing.NgayHetHan;
            reader.IdentityUserId = existing.IdentityUserId;
            return View("Form", reader);
        }

        var updated = await store.UpdateAsync(data =>
        {
            var current = data.DocGia.FirstOrDefault(item => item.Id == id);
            if (current is null ||
                (reader.NgaySinh is DateTime birthDate &&
                    !IsAgeEligible(birthDate, data.Settings, DateTime.Today)) ||
                data.DocGia.Any(item => item.Id != id &&
                    item.Email.Equals(reader.Email.Trim(), StringComparison.OrdinalIgnoreCase)) ||
                data.Profiles.Any(profile => profile.AccountId != current.IdentityUserId &&
                    profile.Email.Equals(reader.Email.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            current.HoTen = reader.HoTen.Trim();
            current.Email = reader.Email.Trim();
            current.SoDienThoai = reader.SoDienThoai?.Trim();
            current.DiaChi = reader.DiaChi?.Trim();
            current.NgaySinh = reader.NgaySinh;
            var profile = data.Profiles.FirstOrDefault(item => item.AccountId == current.IdentityUserId);
            if (profile is not null)
            {
                profile.HoTen = current.HoTen;
                profile.Email = current.Email;
                profile.SoDienThoai = current.SoDienThoai;
                profile.DiaChi = current.DiaChi ?? string.Empty;
            }

            return true;
        });
        if (!updated)
        {
            ModelState.AddModelError(nameof(reader.Email), "Email này đã được sử dụng hoặc hồ sơ không còn tồn tại.");
            reader.NgayDangKy = existing.NgayDangKy;
            reader.NgayHetHan = existing.NgayHetHan;
            reader.IdentityUserId = existing.IdentityUserId;
            return View("Form", reader);
        }

        TempData["Success"] = "Đã cập nhật độc giả.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = LibraryRoles.NhanVien + "," + LibraryRoles.QuanLy)]
    public async Task<IActionResult> Delete(int id)
    {
        var reader = await store.ReadAsync(data => data.DocGia.FirstOrDefault(item => item.Id == id));
        return reader is null ? NotFound() : View(reader);
    }

    [HttpPost, ActionName("Delete")]
    [Authorize(Roles = LibraryRoles.NhanVien + "," + LibraryRoles.QuanLy)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var reader = await store.ReadAsync(data => data.DocGia.FirstOrDefault(item => item.Id == id));
        if (reader is null)
        {
            return NotFound();
        }

        var deleted = await store.UpdateAsync(data =>
        {
            if (data.PhieuMuon.Any(loan => loan.DocGiaId == id))
            {
                return false;
            }

            return data.DocGia.RemoveAll(item => item.Id == id) > 0;
        });

        if (!deleted)
        {
            TempData["Error"] = "Không thể xóa độc giả đã có phiếu mượn.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = "Đã xóa độc giả.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RenewCard(int id)
    {
        var result = await store.UpdateAsync(data =>
        {
            var reader = data.DocGia.FirstOrDefault(item => item.Id == id);
            if (reader is null)
            {
                return (Exists: false, AgeEligible: false);
            }

            if (reader.NgaySinh is not DateTime birthDate ||
                !IsAgeEligible(birthDate, data.Settings, DateTime.Today))
            {
                return (Exists: true, AgeEligible: false);
            }

            var currentExpiry = reader.NgayHetHan?.Date ??
                reader.NgayDangKy.AddMonths(data.Settings.ReaderCardValidityMonths).Date;
            var baseDate = currentExpiry > DateTime.UtcNow.Date
                ? currentExpiry
                : DateTime.UtcNow.Date;
            reader.NgayHetHan = baseDate.AddMonths(data.Settings.ReaderCardValidityMonths);
            return (Exists: true, AgeEligible: true);
        });

        if (!result.Exists)
        {
            return NotFound();
        }

        if (!result.AgeEligible)
        {
            TempData["Error"] = "Cần cập nhật ngày sinh hợp lệ theo quy định trước khi gia hạn thẻ.";
            return RedirectToAction(nameof(Details), new { id });
        }

        TempData["Success"] = "Đã gia hạn thẻ độc giả.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> History(int id)
    {
        var model = await store.ReadAsync(data =>
        {
            var reader = data.DocGia.FirstOrDefault(item => item.Id == id);
            if (reader is null)
            {
                return null;
            }

            var loans = data.PhieuMuon.Where(item => item.DocGiaId == id)
                .OrderByDescending(item => item.NgayMuon)
                .Select(loan =>
                {
                    var details = data.ChiTietPhieuMuon.Where(item => item.PhieuMuonId == loan.Id).ToList();
                    var returns = data.PhieuTra.Where(item => item.PhieuMuonId == loan.Id).ToList();
                    var returnIds = returns.Select(item => item.Id).ToHashSet();
                    var returnDetails = data.ChiTietPhieuTra.Where(item => returnIds.Contains(item.PhieuTraId)).ToList();
                    var paid = returns.Sum(receipt =>
                    {
                        var recordedPayments = data.FinePayments
                            .Where(payment => payment.ReturnId == receipt.Id)
                            .Sum(payment => Math.Max(payment.Amount, 0m));
                        return Math.Max(recordedPayments, Math.Max(receipt.TienPhatDaThanhToan, 0m));
                    });
                    var fine = returnDetails.Sum(item => Math.Max(item.TienPhat, 0m));
                    var accruedFine = GetLoanOutstandingFine(data, loan, DateTime.UtcNow.Date);
                    var status = loan.TrangThai == "DaTra"
                        ? "Đã trả"
                        : loan.HanTra.Date < DateTime.UtcNow.Date ? "Quá hạn" : "Đang mượn";
                    return new ReaderHistoryLoanViewModel
                    {
                        Id = loan.Id,
                        BorrowedAt = loan.NgayMuon,
                        DueAt = loan.HanTra,
                        Status = status,
                        Books = details.Select(detail =>
                        {
                            var book = data.Sach.FirstOrDefault(item => item.Id == detail.SachId);
                            return $"{book?.TenSach ?? $"Sách #{detail.SachId}"} × {detail.SoLuong}";
                        }).ToList(),
                        FinePaid = paid,
                        FineOutstanding = Math.Max(fine - paid, 0m) + accruedFine
                    };
                }).ToList();

            return new ReaderHistoryViewModel
            {
                Reader = reader,
                Loans = loans,
                OutstandingFine = loans.Sum(item => item.FineOutstanding)
            };
        });
        return model is null ? NotFound() : View(model);
    }

    private async Task ValidateEmailAsync(DocGia reader, int? currentId = null)
    {
        if (await store.ReadAsync(data => data.DocGia.Any(item => item.Id != currentId &&
            item.Email.Equals(reader.Email.Trim(), StringComparison.OrdinalIgnoreCase)) ||
            data.Profiles.Any(profile => profile.AccountId != reader.IdentityUserId &&
            profile.Email.Equals(reader.Email.Trim(), StringComparison.OrdinalIgnoreCase))))
        {
            ModelState.AddModelError(nameof(reader.Email), "Email này đã có trong danh sách độc giả.");
        }
    }

    private async Task ValidateBirthDateAsync(DateTime? birthDate, bool required)
    {
        if (birthDate is null)
        {
            if (required)
            {
                ModelState.AddModelError(nameof(DocGia.NgaySinh), "Vui lòng nhập ngày sinh để xác minh độ tuổi làm thẻ.");
            }

            return;
        }

        var settings = await store.ReadAsync(data => data.Settings);
        if (!IsAgeEligible(birthDate.Value, settings, DateTime.Today))
        {
            ModelState.AddModelError(nameof(DocGia.NgaySinh),
                $"Độ tuổi làm thẻ hợp lệ từ {settings.ReaderMinimumAge} đến {settings.ReaderMaximumAge} tuổi.");
        }
    }

    private static bool IsAgeEligible(DateTime birthDate, LibrarySettings settings, DateTime today)
    {
        var age = today.Year - birthDate.Date.Year;
        if (birthDate.Date > today.AddYears(-age))
        {
            age--;
        }

        return birthDate.Date <= today &&
               age >= settings.ReaderMinimumAge &&
               age <= settings.ReaderMaximumAge;
    }

    private static decimal GetLoanOutstandingFine(LibraryData data, PhieuMuon loan, DateTime today)
    {
        var lateDays = Math.Max((today - loan.HanTra.Date).Days, 0);
        if (lateDays == 0)
        {
            return 0m;
        }

        var returnIds = data.PhieuTra.Where(item => item.PhieuMuonId == loan.Id)
            .Select(item => item.Id).ToHashSet();
        var returnedByDetail = data.ChiTietPhieuTra
            .Where(item => returnIds.Contains(item.PhieuTraId))
            .GroupBy(item => item.ChiTietPhieuMuonId)
            .ToDictionary(group => group.Key, group => group.Sum(item => item.SoLuongTra));
        return data.ChiTietPhieuMuon
            .Where(item => item.PhieuMuonId == loan.Id)
            .Sum(item => Math.Max(item.SoLuong - returnedByDetail.GetValueOrDefault(item.Id), 0) *
                         loan.MucPhatMoiNgay * lateDays);
    }

}