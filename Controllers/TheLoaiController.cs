using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using quan_ly_muon_tra_sach_thu_vien.Data;
using quan_ly_muon_tra_sach_thu_vien.Models;
using quan_ly_muon_tra_sach_thu_vien.Models.ViewModels;

namespace quan_ly_muon_tra_sach_thu_vien.Controllers;

[Authorize(Roles = LibraryRoles.NhanVien + "," + LibraryRoles.QuanLy)]
public sealed class TheLoaiController(LibraryFileStore store) : Controller
{
    public async Task<IActionResult> Index(string? search, int page = 1)
    {
        ViewBag.Search = search;
        return View(await store.ReadAsync(data =>
        {
            var categories = data.TheLoai.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                categories = categories.Where(category =>
                    category.TenTheLoai.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    (category.MoTa?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            return PagedResult<TheLoai>.Create(categories.OrderBy(category => category.TenTheLoai), page);
        }));
    }

    public async Task<IActionResult> Details(int id, int page = 1)
    {
        var category = await store.ReadAsync(data =>
        {
            var item = data.TheLoai.FirstOrDefault(category => category.Id == id);
            return item;
        });
        if (category is null)
        {
            return NotFound();
        }

        var books = await store.ReadAsync(data => PagedResult<Sach>.Create(
            data.Sach.Where(book => book.TheLoaiId == id).OrderBy(book => book.TenSach), page));
        return View(new TheLoaiDetailsViewModel { Category = category, Books = books });
    }

    [Authorize(Roles = LibraryRoles.QuanLy)]
    public IActionResult Create() => View("Form", new TheLoai());

    [HttpPost]
    [Authorize(Roles = LibraryRoles.QuanLy)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("TenTheLoai,MoTa")] TheLoai category)
    {
        if (!ModelState.IsValid)
        {
            return View("Form", category);
        }

        var added = await store.UpdateAsync(data =>
        {
            if (data.TheLoai.Any(item => item.TenTheLoai.Equals(category.TenTheLoai.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            category.Id = data.TheLoai.Select(item => item.Id).DefaultIfEmpty(0).Max() + 1;
            category.TenTheLoai = category.TenTheLoai.Trim();
            category.MoTa = string.IsNullOrWhiteSpace(category.MoTa) ? null : category.MoTa.Trim();
            data.TheLoai.Add(category);
            return true;
        });
        if (!added)
        {
            ModelState.AddModelError(nameof(category.TenTheLoai), "Tên thể loại đã tồn tại.");
            return View("Form", category);
        }
        TempData["Success"] = "Đã thêm thể loại.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = LibraryRoles.QuanLy)]
    public async Task<IActionResult> Edit(int id)
    {
        var category = await store.ReadAsync(data => data.TheLoai.FirstOrDefault(item => item.Id == id));
        return category is null ? NotFound() : View("Form", category);
    }

    [HttpPost]
    [Authorize(Roles = LibraryRoles.QuanLy)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,TenTheLoai,MoTa")] TheLoai category)
    {
        if (id != category.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View("Form", category);
        }

        var updated = await store.UpdateAsync(data =>
        {
            var existing = data.TheLoai.FirstOrDefault(item => item.Id == id);
            if (existing is null || data.TheLoai.Any(item => item.Id != id &&
                    item.TenTheLoai.Equals(category.TenTheLoai.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            existing.TenTheLoai = category.TenTheLoai.Trim();
            existing.MoTa = string.IsNullOrWhiteSpace(category.MoTa) ? null : category.MoTa.Trim();
            return true;
        });
        if (!updated)
        {
            ModelState.AddModelError(nameof(category.TenTheLoai), "Không thể cập nhật. Tên có thể đã tồn tại.");
            return View("Form", category);
        }
        TempData["Success"] = "Đã cập nhật thể loại.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = LibraryRoles.QuanLy)]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await store.ReadAsync(data => data.TheLoai.FirstOrDefault(item => item.Id == id));
        return category is null ? NotFound() : View(category);
    }

    [HttpPost, ActionName("Delete")]
    [Authorize(Roles = LibraryRoles.QuanLy)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var category = await store.ReadAsync(data => data.TheLoai.FirstOrDefault(item => item.Id == id));
        if (category is null)
        {
            return NotFound();
        }

        var deleted = await store.UpdateAsync(data =>
        {
            if (data.Sach.Any(book => book.TheLoaiId == id))
            {
                return false;
            }

            return data.TheLoai.RemoveAll(item => item.Id == id) > 0;
        });
        if (!deleted)
        {
            TempData["Error"] = "Không thể xóa thể loại đang được sách sử dụng.";
            return RedirectToAction(nameof(Index));
        }
        TempData["Success"] = "Đã xóa thể loại.";
        return RedirectToAction(nameof(Index));
    }
}