using Microsoft.AspNetCore.Mvc;
using quan_ly_muon_tra_sach_thu_vien.Data;
using quan_ly_muon_tra_sach_thu_vien.Models.ViewModels;

namespace quan_ly_muon_tra_sach_thu_vien.Controllers;

public sealed class TraCuuController(LibraryFileStore store) : Controller
{
    public async Task<IActionResult> Index(string? search, int? categoryId, bool availableOnly = false, int page = 1)
    {
        var term = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        var model = await store.ReadAsync(data =>
        {
            var categories = data.TheLoai
                .OrderBy(category => category.TenTheLoai)
                .Select(category => new LibraryCatalogCategoryViewModel
                {
                    Id = category.Id,
                    Name = category.TenTheLoai,
                    BookCount = data.Sach.Count(book => book.TheLoaiId == category.Id)
                })
                .ToList();
            var categoryNames = categories.ToDictionary(category => category.Id, category => category.Name);
            var terms = VietnameseText.SplitTerms(term);
            var books = data.Sach
                .Where(book => !categoryId.HasValue || book.TheLoaiId == categoryId.Value)
                .Where(book => !availableOnly || book.SoLuong > 0)
                .Where(book => VietnameseText.ContainsAllTerms(terms,
                    book.TenSach,
                    book.TacGia,
                    book.NhaXuatBan,
                    book.TheLoaiId.HasValue ? categoryNames.GetValueOrDefault(book.TheLoaiId.Value) : null))
                .OrderBy(book => book.TenSach)
                .Select(book => new LibraryCatalogBookViewModel
                {
                    Id = book.Id,
                    Title = book.TenSach,
                    Author = book.TacGia,
                    Publisher = book.NhaXuatBan,
                    PublishedYear = book.NamXuatBan,
                    AvailableCopies = book.SoLuong,
                    Category = book.TheLoaiId.HasValue
                        ? categoryNames.GetValueOrDefault(book.TheLoaiId.Value) ?? "Chưa phân loại"
                        : "Chưa phân loại"
                });

            return new LibraryCatalogViewModel
            {
                Books = PagedResult<LibraryCatalogBookViewModel>.Create(books, page),
                Categories = categories,
                Search = term,
                CategoryId = categoryId,
                AvailableOnly = availableOnly
            };
        });

        return View(model);
    }
}
