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

            return PagedResult<DocGia>.Create(readers.OrderBy(reader => reader.HoTen), page);
        }));
    }

    public async Task<IActionResult> Details(int id)
    {
        var reader = await store.ReadAsync(data => data.DocGia.FirstOrDefault(item => item.Id == id));
        return reader is null ? NotFound() : View(reader);
    }

    [Authorize(Roles = LibraryRoles.QuanLy)]
    public IActionResult Create() => View("Form", new DocGia());

    [HttpPost]
    [Authorize(Roles = LibraryRoles.QuanLy)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("HoTen,Email,SoDienThoai,DiaChi")] DocGia reader)
    {
        await ValidateEmailAsync(reader);
        if (!ModelState.IsValid)
        {
            return View("Form", reader);
        }

        var added = await store.UpdateAsync(data =>
        {
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
            data.DocGia.Add(reader);
            return true;
        });
        if (!added)
        {
            ModelState.AddModelError(nameof(reader.Email), "Email này đã được sử dụng.");
            return View("Form", reader);
        }
        TempData["Success"] = "Đã thêm độc giả.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = LibraryRoles.QuanLy)]
    public async Task<IActionResult> Edit(int id)
    {
        var reader = await store.ReadAsync(data => data.DocGia.FirstOrDefault(item => item.Id == id));
        return reader is null ? NotFound() : View("Form", reader);
    }

    [HttpPost]
    [Authorize(Roles = LibraryRoles.QuanLy)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,HoTen,Email,SoDienThoai,DiaChi")] DocGia reader)
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

        if (!ModelState.IsValid)
        {
            reader.NgayDangKy = existing.NgayDangKy;
            reader.IdentityUserId = existing.IdentityUserId;
            return View("Form", reader);
        }

        var updated = await store.UpdateAsync(data =>
        {
            var current = data.DocGia.FirstOrDefault(item => item.Id == id);
            if (current is null || data.DocGia.Any(item => item.Id != id &&
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
            reader.IdentityUserId = existing.IdentityUserId;
            return View("Form", reader);
        }

        TempData["Success"] = "Đã cập nhật độc giả.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = LibraryRoles.QuanLy)]
    public async Task<IActionResult> Delete(int id)
    {
        var reader = await store.ReadAsync(data => data.DocGia.FirstOrDefault(item => item.Id == id));
        return reader is null ? NotFound() : View(reader);
    }

    [HttpPost, ActionName("Delete")]
    [Authorize(Roles = LibraryRoles.QuanLy)]
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

}