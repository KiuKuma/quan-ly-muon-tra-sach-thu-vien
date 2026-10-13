using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using quan_ly_muon_tra_sach_thu_vien.Data;
using quan_ly_muon_tra_sach_thu_vien.Models;

namespace quan_ly_muon_tra_sach_thu_vien.Controllers;

[Authorize(Roles = LibraryRoles.NhanVien + "," + LibraryRoles.QuanLy)]
public sealed class SachController(LibraryFileStore store) : Controller
{
    public async Task<IActionResult> Index(string? search)
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
            var results = books.OrderBy(book => book.TenSach).ToList();
            foreach (var book in results)
            {
                categories.TryGetValue(book.TheLoaiId, out var category);
                book.TheLoai = category;
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

    [Authorize(Roles = LibraryRoles.QuanLy)]
    public async Task<IActionResult> Create()
    {
        await PopulateCategoriesAsync();
        return View("Form", new Sach());
    }

    [HttpPost]
    [Authorize(Roles = LibraryRoles.QuanLy)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("TenSach,TacGia,ISBN,NhaXuatBan,NamXuatBan,SoLuong,TheLoaiId")] Sach book)
    {
        await ValidateBookAsync(book);
        if (!ModelState.IsValid)
        {
            await PopulateCategoriesAsync(book.TheLoaiId);
            return View("Form", book);
        }

        var added = await store.UpdateAsync(data =>
        {
            if (!data.TheLoai.Any(category => category.Id == book.TheLoaiId) ||
                (!string.IsNullOrWhiteSpace(book.ISBN) && data.Sach.Any(item => item.ISBN == book.ISBN.Trim())))
            {
                return false;
            }

            book.Id = data.Sach.Select(item => item.Id).DefaultIfEmpty(0).Max() + 1;
            book.ISBN = book.ISBN?.Trim();
            book.TheLoai = null;
            data.Sach.Add(book);
            return true;
        });
        if (!added)
        {
            await ValidateBookAsync(book);
            ModelState.AddModelError(string.Empty, "Dữ liệu danh mục vừa thay đổi. Vui lòng kiểm tra lại.");
            await PopulateCategoriesAsync(book.TheLoaiId);
            return View("Form", book);
        }
        TempData["Success"] = "Đã thêm sách.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = LibraryRoles.QuanLy)]
    public async Task<IActionResult> Edit(int id)
    {
        var book = await store.ReadAsync(data => data.Sach.FirstOrDefault(item => item.Id == id));
        if (book is null)
        {
            return NotFound();
        }

        await PopulateCategoriesAsync(book.TheLoaiId);
        return View("Form", book);
    }

    [HttpPost]
    [Authorize(Roles = LibraryRoles.QuanLy)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,TenSach,TacGia,ISBN,NhaXuatBan,NamXuatBan,SoLuong,TheLoaiId")] Sach book)
    {
        if (id != book.Id)
        {
            return NotFound();
        }

        await ValidateBookAsync(book, id);
        if (!ModelState.IsValid)
        {
            await PopulateCategoriesAsync(book.TheLoaiId);
            return View("Form", book);
        }

        var updated = await store.UpdateAsync(data =>
        {
            var existing = data.Sach.FirstOrDefault(item => item.Id == id);
            if (existing is null || !data.TheLoai.Any(category => category.Id == book.TheLoaiId) ||
                (!string.IsNullOrWhiteSpace(book.ISBN) && data.Sach.Any(item =>
                    item.Id != id && item.ISBN == book.ISBN.Trim())))
            {
                return false;
            }

            existing.TenSach = book.TenSach.Trim();
            existing.TacGia = book.TacGia.Trim();
            existing.ISBN = book.ISBN?.Trim();
            existing.NhaXuatBan = book.NhaXuatBan?.Trim();
            existing.NamXuatBan = book.NamXuatBan;
            existing.SoLuong = book.SoLuong;
            existing.TheLoaiId = book.TheLoaiId;
            return true;
        });
        if (!updated)
        {
            await ValidateBookAsync(book, id);
            ModelState.AddModelError(string.Empty, "Không tìm thấy sách, thể loại hoặc ISBN không hợp lệ.");
            await PopulateCategoriesAsync(book.TheLoaiId);
            return View("Form", book);
        }
        TempData["Success"] = "Đã cập nhật sách.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = LibraryRoles.QuanLy)]
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
    [Authorize(Roles = LibraryRoles.QuanLy)]
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

    private async Task ValidateBookAsync(Sach book, int? currentId = null)
    {
        var issues = await store.ReadAsync(data => (
            CategoryExists: data.TheLoai.Any(category => category.Id == book.TheLoaiId),
            IsbnExists: !string.IsNullOrWhiteSpace(book.ISBN) && data.Sach.Any(item =>
                item.Id != currentId && item.ISBN == book.ISBN.Trim())));
        if (!issues.CategoryExists)
        {
            ModelState.AddModelError(nameof(book.TheLoaiId), "Vui lòng chọn thể loại hợp lệ.");
        }

        if (issues.IsbnExists)
        {
            ModelState.AddModelError(nameof(book.ISBN), "ISBN này đã được sử dụng.");
        }
    }

    private async Task PopulateCategoriesAsync(int? selectedId = null)
    {
        var categories = await store.ReadAsync(data =>
            data.TheLoai.OrderBy(category => category.TenTheLoai).ToList());
        ViewBag.TheLoaiId = new SelectList(categories,
            nameof(TheLoai.Id), nameof(TheLoai.TenTheLoai), selectedId);
    }
}