using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using quan_ly_muon_tra_sach_thu_vien.Data;
using quan_ly_muon_tra_sach_thu_vien.Models;
using quan_ly_muon_tra_sach_thu_vien.Models.ViewModels;

namespace quan_ly_muon_tra_sach_thu_vien.Controllers;

[Authorize(Roles = LibraryRoles.NhanVien + "," + LibraryRoles.QuanLy)]
public sealed class SachController(LibraryFileStore store) : Controller
{
    public async Task<IActionResult> Index(string? search, int page = 1)
    {
        ViewBag.Search = search;
        return View(await store.ReadAsync(data =>
        {
            var books = data.Sach.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                books = books.Where(book => book.TenSach.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                                            book.TacGia.Contains(term, StringComparison.OrdinalIgnoreCase));
            }

            var categories = data.TheLoai.ToDictionary(category => category.Id);
            var results = PagedResult<Sach>.Create(
                books.OrderBy(book => book.TenSach), page);
            foreach (var book in results.Items)
            {
                if (book.TheLoaiId is int categoryId)
                {
                    categories.TryGetValue(categoryId, out var category);
                    book.TheLoai = category;
                }
            }

            return results;
        }));
    }

    public async Task<IActionResult> Details(int id)
    {
        var book = await store.ReadAsync(data =>
        {
            var item = data.Sach.FirstOrDefault(book => book.Id == id);
            if (item is not null)
            {
                item.TheLoai = data.TheLoai.FirstOrDefault(category => category.Id == item.TheLoaiId);
            }

            return item;
        });
        return book is null ? NotFound() : View(book);
    }

    [Authorize(Roles = LibraryRoles.NhanVien + "," + LibraryRoles.QuanLy)]
    public IActionResult Create() => View("Form", new Sach());

    [HttpPost]
    [Authorize(Roles = LibraryRoles.NhanVien + "," + LibraryRoles.QuanLy)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("TenSach,TacGia,NhaXuatBan,NamXuatBan,SoLuong")] Sach book)
    {
        if (!ModelState.IsValid)
        {
            return View("Form", book);
        }

        await store.UpdateAsync(data =>
        {
            book.Id = data.Sach.Select(item => item.Id).DefaultIfEmpty(0).Max() + 1;
            book.TenSach = book.TenSach.Trim();
            book.TacGia = book.TacGia.Trim();
            book.NhaXuatBan = book.NhaXuatBan?.Trim();
            data.Sach.Add(book);
            return book.Id;
        });
        TempData["Success"] = "Đã thêm sách.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = LibraryRoles.NhanVien + "," + LibraryRoles.QuanLy)]
    public async Task<IActionResult> Edit(int id)
    {
        var book = await store.ReadAsync(data => data.Sach.FirstOrDefault(item => item.Id == id));
        if (book is null)
        {
            return NotFound();
        }

        return View("Form", book);
    }

    [HttpPost]
    [Authorize(Roles = LibraryRoles.NhanVien + "," + LibraryRoles.QuanLy)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,TenSach,TacGia,NhaXuatBan,NamXuatBan,SoLuong")] Sach book)
    {
        if (id != book.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View("Form", book);
        }

        var updated = await store.UpdateAsync(data =>
        {
            var existing = data.Sach.FirstOrDefault(item => item.Id == id);
            if (existing is null)
            {
                return false;
            }

            existing.TenSach = book.TenSach.Trim();
            existing.TacGia = book.TacGia.Trim();
            existing.NhaXuatBan = book.NhaXuatBan?.Trim();
            existing.NamXuatBan = book.NamXuatBan;
            existing.SoLuong = book.SoLuong;
            return true;
        });
        if (!updated)
        {
            ModelState.AddModelError(string.Empty, "Không tìm thấy sách để cập nhật.");
            return View("Form", book);
        }
        TempData["Success"] = "Đã cập nhật sách.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = LibraryRoles.NhanVien + "," + LibraryRoles.QuanLy)]
    public async Task<IActionResult> Delete(int id)
    {
        var book = await store.ReadAsync(data =>
        {
            var item = data.Sach.FirstOrDefault(book => book.Id == id);
            if (item is not null)
            {
                item.TheLoai = data.TheLoai.FirstOrDefault(category => category.Id == item.TheLoaiId);
            }

            return item;
        });
        return book is null ? NotFound() : View(book);
    }

    [HttpPost, ActionName("Delete")]
    [Authorize(Roles = LibraryRoles.NhanVien + "," + LibraryRoles.QuanLy)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var book = await store.ReadAsync(data => data.Sach.FirstOrDefault(item => item.Id == id));
        if (book is null)
        {
            return NotFound();
        }

        var deleted = await store.UpdateAsync(data =>
        {
            if (data.ChiTietPhieuMuon.Any(detail => detail.SachId == id))
            {
                return false;
            }

            return data.Sach.RemoveAll(item => item.Id == id) > 0;
        });
        if (!deleted)
        {
            TempData["Error"] = "Không thể xóa sách đã có trong phiếu mượn.";
            return RedirectToAction(nameof(Index));
        }
        TempData["Success"] = "Đã xóa sách.";
        return RedirectToAction(nameof(Index));
    }
}