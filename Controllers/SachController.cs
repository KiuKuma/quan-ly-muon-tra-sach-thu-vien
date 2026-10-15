using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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

    [Authorize(Roles = LibraryRoles.QuanLy)]
    public async Task<IActionResult> Create()
    {
        await PopulateCategoriesAsync();
        return View("Form", new Sach());
    }

    [HttpPost]
    [Authorize(Roles = LibraryRoles.QuanLy)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("TenSach,TacGia,NhaXuatBan,NamXuatBan,SoLuong,TheLoaiId")] Sach book)
    {
        await ValidateBookAsync(book);
        if (!ModelState.IsValid)
        {
            await PopulateCategoriesAsync(book.TheLoaiId);
            return View("Form", book);
        }

        var added = await store.UpdateAsync(data =>
        {
            if (book.TheLoaiId is int categoryId && data.TheLoai.All(category => category.Id != categoryId))
            {
                return false;
            }

            book.Id = data.Sach.Select(item => item.Id).DefaultIfEmpty(0).Max() + 1;
            book.TenSach = book.TenSach.Trim();
            book.TacGia = book.TacGia.Trim();
            book.NhaXuatBan = string.IsNullOrWhiteSpace(book.NhaXuatBan) ? null : book.NhaXuatBan.Trim();
            data.Sach.Add(book);
            return true;
        });
        if (!added)
        {
            ModelState.AddModelError(nameof(book.TheLoaiId), "Thể loại vừa bị xóa. Vui lòng chọn lại.");
            await PopulateCategoriesAsync();
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
    public async Task<IActionResult> Edit(int id, [Bind("Id,TenSach,TacGia,NhaXuatBan,NamXuatBan,SoLuong,TheLoaiId")] Sach book)
    {
        if (id != book.Id)
        {
            return NotFound();
        }

        await ValidateBookAsync(book);
        if (!ModelState.IsValid)
        {
            await PopulateCategoriesAsync(book.TheLoaiId);
            return View("Form", book);
        }

        var updated = await store.UpdateAsync(data =>
        {
            var existing = data.Sach.FirstOrDefault(item => item.Id == id);
            if (existing is null ||
                (book.TheLoaiId is int categoryId && data.TheLoai.All(category => category.Id != categoryId)))
            {
                return false;
            }

            existing.TenSach = book.TenSach.Trim();
            existing.TacGia = book.TacGia.Trim();
            existing.NhaXuatBan = string.IsNullOrWhiteSpace(book.NhaXuatBan) ? null : book.NhaXuatBan.Trim();
            existing.NamXuatBan = book.NamXuatBan;
            existing.SoLuong = book.SoLuong;
            existing.TheLoaiId = book.TheLoaiId;
            return true;
        });
        if (!updated)
        {
            ModelState.AddModelError(string.Empty, "Không tìm thấy sách hoặc thể loại đã chọn không còn tồn tại.");
            await PopulateCategoriesAsync();
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

    private async Task ValidateBookAsync(Sach book)
    {
        if (book.NamXuatBan > DateTime.Now.Year)
        {
            ModelState.AddModelError(nameof(book.NamXuatBan), "Năm xuất bản không được lớn hơn năm hiện tại.");
        }

        if (book.TheLoaiId is int categoryId &&
            !await store.ReadAsync(data => data.TheLoai.Any(category => category.Id == categoryId)))
        {
            ModelState.AddModelError(nameof(book.TheLoaiId), "Thể loại đã chọn không tồn tại.");
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
