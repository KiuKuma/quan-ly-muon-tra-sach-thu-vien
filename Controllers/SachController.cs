using System.Security.Claims;
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
    public async Task<IActionResult> Index(
        string? search, int? categoryId, bool availableOnly = false, int page = 1)
    {
        var term = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        var result = await store.ReadAsync(data =>
        {
            var categoryNames = data.TheLoai.ToDictionary(category => category.Id);

            var books = data.Sach.AsEnumerable();
            books = books
                .Where(book => !categoryId.HasValue || book.TheLoaiId == categoryId.Value)
                .Where(book => !availableOnly || book.SoLuong > 0)
                .Where(book => VietnameseText.ContainsAllTerms(VietnameseText.SplitTerms(term),
                    book.TenSach,
                    book.TacGia,
                    book.NhaXuatBan,
                    book.TheLoaiId is int bookCategoryId &&
                    categoryNames.TryGetValue(bookCategoryId, out var category) ? category.TenTheLoai : null));

            var results = PagedResult<Sach>.Create(
                books.OrderBy(book => book.TenSach), page);
            foreach (var book in results.Items)
            {
                if (book.TheLoaiId is int categoryId)
                {
                    categoryNames.TryGetValue(categoryId, out var category);
                    book.TheLoai = category;
                }
            }

            return (Books: results, Categories: data.TheLoai.OrderBy(category => category.TenTheLoai).ToList());
        });
        ViewBag.Categories = new SelectList(result.Categories,
            nameof(TheLoai.Id), nameof(TheLoai.TenTheLoai), categoryId);
        ViewBag.Search = term;
        ViewBag.CategoryId = categoryId;
        ViewBag.AvailableOnly = availableOnly;
        return View(result.Books);
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

    [HttpGet]
    public async Task<IActionResult> BorrowingHistory(string? search, int page = 1)
    {
        var term = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        var model = await store.ReadAsync(data =>
        {
            var matches = new List<BookBorrowingHistoryRowViewModel>();
            if (term is not null)
            {
                var terms = VietnameseText.SplitTerms(term);
                var books = data.Sach
                    .Where(book =>
                        VietnameseText.ContainsAllTerms(terms, book.TenSach) ||
                        VietnameseText.ContainsAllTerms(terms, $"SH-{book.Id}", book.Id.ToString()))
                    .ToDictionary(book => book.Id);
                var readers = data.DocGia.ToDictionary(reader => reader.Id);
                var loans = data.PhieuMuon.ToDictionary(loan => loan.Id);
                var loanDetails = data.ChiTietPhieuMuon
                    .GroupBy(detail => detail.PhieuMuonId)
                    .ToDictionary(group => group.Key, group => group.ToList());
                var returnedQuantities = data.ChiTietPhieuTra
                    .GroupBy(detail => detail.ChiTietPhieuMuonId)
                    .ToDictionary(group => group.Key, group => group.Sum(detail => detail.SoLuongTra));

                foreach (var detail in data.ChiTietPhieuMuon.Where(detail => books.ContainsKey(detail.SachId)))
                {
                    if (!loans.TryGetValue(detail.PhieuMuonId, out var loan))
                    {
                        continue;
                    }

                    var allLoanDetails = loanDetails.GetValueOrDefault(loan.Id) ?? [];
                    var allReturned = allLoanDetails.Count > 0 && allLoanDetails.All(item =>
                        returnedQuantities.GetValueOrDefault(item.Id) >= item.SoLuong);
                    var someReturned = allLoanDetails.Any(item => returnedQuantities.GetValueOrDefault(item.Id) > 0);
                    var status = loan.TrangThai == "DaTra" || allReturned
                        ? ("DaTra", "Đã trả")
                        : loan.HanTra.Date < DateTime.UtcNow.Date
                            ? ("QuaHan", "Quá hạn")
                            : someReturned ? ("TraMotPhan", "Trả một phần") : ("DangMuon", "Đang mượn");
                    var book = books[detail.SachId];
                    matches.Add(new BookBorrowingHistoryRowViewModel
                    {
                        BookId = book.Id,
                        BookTitle = book.TenSach,
                        LoanId = loan.Id,
                        ReaderId = loan.DocGiaId,
                        ReaderName = readers.GetValueOrDefault(loan.DocGiaId)?.HoTen ?? $"Độc giả #{loan.DocGiaId}",
                        Quantity = detail.SoLuong,
                        BorrowedAt = loan.NgayMuon,
                        DueAt = loan.HanTra,
                        StatusCode = status.Item1,
                        Status = status.Item2
                    });
                }
            }

            return new BookBorrowingHistoryViewModel
            {
                Search = term,
                Results = PagedResult<BookBorrowingHistoryRowViewModel>.Create(
                    matches.OrderByDescending(row => row.BorrowedAt), page)
            };
        });

        return View(model);
    }

    public async Task<IActionResult> Create()
    {
        await PopulateCategoriesAsync();
        return View("Form", new Sach());
    }

    [HttpPost]
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

    [HttpGet]
    public async Task<IActionResult> Inventory(int id)
    {
        var model = await store.ReadAsync(data =>
        {
            var book = data.Sach.FirstOrDefault(item => item.Id == id);
            return book is null
                ? null
                : new InventoryAdjustmentPageViewModel
                {
                    Book = book,
                    History = data.BookInventoryAdjustments
                        .Where(item => item.BookId == id)
                        .OrderByDescending(item => item.AdjustedAt)
                        .Take(20)
                        .ToList()
                };
        });
        return model is null ? NotFound() : View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Inventory(int id, InventoryAdjustmentPageViewModel model)
    {
        if (model.Adjustment.Type is not ("HuHong" or "Mat"))
        {
            ModelState.AddModelError(nameof(model.Adjustment.Type), "Chọn loại sự cố hợp lệ.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateInventoryPageAsync(model, id);
            return View(model);
        }

        var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var result = await store.UpdateAsync(data =>
        {
            var book = data.Sach.FirstOrDefault(item => item.Id == id);
            var staff = data.NhanVien.FirstOrDefault(item => item.IdentityUserId == accountId);
            var account = data.Accounts.FirstOrDefault(item => item.Id == accountId);
            if (book is null || staff is null || account is null)
            {
                return (Success: false, Error: "Không tìm thấy sách hoặc nhân viên hợp lệ.");
            }

            if (model.Adjustment.Quantity > book.SoLuong)
            {
                return (Success: false, Error: $"Số bản cần loại khỏi kho vượt số lượng sẵn có ({book.SoLuong}).");
            }

            book.SoLuong -= model.Adjustment.Quantity;
            data.BookInventoryAdjustments.Add(new BookInventoryAdjustment
            {
                Id = data.BookInventoryAdjustments.Select(item => item.Id).DefaultIfEmpty(0).Max() + 1,
                BookId = book.Id,
                Quantity = model.Adjustment.Quantity,
                Reason = model.Adjustment.Type == "Mat" ? "Mất" : "Hư hỏng",
                Note = model.Adjustment.Note?.Trim(),
                StaffUsername = account.Username
            });
            return (Success: true, Error: string.Empty);
        });

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            await PopulateInventoryPageAsync(model, id);
            return View(model);
        }

        TempData["Success"] = "Đã xóa số bản khỏi tồn kho và ghi nhận lý do.";
        return RedirectToAction(nameof(Inventory), new { id });
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

    private async Task PopulateInventoryPageAsync(InventoryAdjustmentPageViewModel model, int id)
    {
        var page = await store.ReadAsync(data => new InventoryAdjustmentPageViewModel
        {
            Book = data.Sach.FirstOrDefault(item => item.Id == id) ?? new Sach { Id = id },
            Adjustment = model.Adjustment,
            History = data.BookInventoryAdjustments
                .Where(item => item.BookId == id)
                .OrderByDescending(item => item.AdjustedAt)
                .Take(20)
                .ToList()
        });
        model.Book = page.Book;
        model.History = page.History;
    }
}
