using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using quan_ly_muon_tra_sach_thu_vien.Data;
using quan_ly_muon_tra_sach_thu_vien.Models;

namespace quan_ly_muon_tra_sach_thu_vien.Controllers;

[Authorize(Roles = LibraryRoles.ThuThu + "," + LibraryRoles.QuanTriVien)]
public sealed class DocGiaController(
    LibraryFileStore store) : Controller
{
    public async Task<IActionResult> Index(string? search)
    {
        ViewBag.Search = search;
        return View(await store.ReadAsync(data =>
        {
            var readers = data.DocGia.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                readers = readers.Where(reader => reader.HoTen.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                                                  reader.Email.Contains(term, StringComparison.OrdinalIgnoreCase));
            }

            return readers.OrderBy(reader => reader.HoTen).ToList();
        }));
    }

    public async Task<IActionResult> Details(int id)
    {
        var reader = await store.ReadAsync(data => data.DocGia.FirstOrDefault(item => item.Id == id));
        return reader is null ? NotFound() : View(reader);
    }

    public IActionResult Create() => View("Form", new DocGia());

    [HttpPost]
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
                data.Accounts.Any(account => account.Email.Equals(reader.Email.Trim(), StringComparison.OrdinalIgnoreCase)))
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

    public async Task<IActionResult> Edit(int id)
    {
        var reader = await store.ReadAsync(data => data.DocGia.FirstOrDefault(item => item.Id == id));
        return reader is null ? NotFound() : View("Form", reader);
    }

    [HttpPost]
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
                data.Accounts.Any(account => account.Id != current.IdentityUserId &&
                    account.Email.Equals(reader.Email.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            current.HoTen = reader.HoTen.Trim();
            current.Email = reader.Email.Trim();
            current.SoDienThoai = reader.SoDienThoai?.Trim();
            current.DiaChi = reader.DiaChi?.Trim();
            var account = data.Accounts.FirstOrDefault(item => item.Id == current.IdentityUserId);
            if (account is not null)
            {
                account.HoTen = current.HoTen;
                account.Email = current.Email;
                account.SoDienThoai = current.SoDienThoai;
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

    public async Task<IActionResult> Delete(int id)
    {
        var reader = await store.ReadAsync(data => data.DocGia.FirstOrDefault(item => item.Id == id));
        return reader is null ? NotFound() : View(reader);
    }

    [HttpPost, ActionName("Delete")]
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

            data.Accounts.RemoveAll(account => account.Id == reader.IdentityUserId);
            return data.DocGia.RemoveAll(item => item.Id == id) > 0;
        });

        if (!deleted)
        {
            TempData["Error"] = "Không thể xóa độc giả đã có phiếu mượn.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = "Đã xóa độc giả và tài khoản liên kết (nếu có).";
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateEmailAsync(DocGia reader, int? currentId = null)
    {
        if (await store.ReadAsync(data => data.DocGia.Any(item => item.Id != currentId &&
            item.Email.Equals(reader.Email.Trim(), StringComparison.OrdinalIgnoreCase)) ||
            data.Accounts.Any(account => account.Id != reader.IdentityUserId &&
            account.Email.Equals(reader.Email.Trim(), StringComparison.OrdinalIgnoreCase))))
        {
            ModelState.AddModelError(nameof(reader.Email), "Email này đã có trong danh sách độc giả.");
        }
    }

}