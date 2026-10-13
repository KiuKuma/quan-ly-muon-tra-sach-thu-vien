using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using quan_ly_muon_tra_sach_thu_vien.Data;
using quan_ly_muon_tra_sach_thu_vien.Models;
using quan_ly_muon_tra_sach_thu_vien.Models.ViewModels;

namespace quan_ly_muon_tra_sach_thu_vien.Controllers;

[Authorize(Roles = LibraryRoles.NhanVien + "," + LibraryRoles.QuanLy)]
public sealed class MuonTraController(LibraryFileStore store) : Controller
{
    public async Task<IActionResult> Index()
    {
        var model = await store.ReadAsync(BuildPageModel);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LibraryLoanPageViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulatePageModelAsync(model);
            return View("Index", model);
        }

        var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var result = await store.UpdateAsync(data =>
        {
            var reader = data.DocGia.FirstOrDefault(item => item.Id == model.NewLoan.ReaderId);
            var book = data.Sach.FirstOrDefault(item => item.Id == model.NewLoan.BookId);
            var staff = data.NhanVien.FirstOrDefault(item => item.IdentityUserId == accountId);
            var staffAccount = data.Accounts.FirstOrDefault(item => item.Id == accountId);
            if (reader is null || book is null || staff is null || staffAccount is null ||
                model.NewLoan.DueAt.Date < DateTime.UtcNow.Date ||
                book.SoLuong < model.NewLoan.Quantity)
            {
                return false;
            }

            var loanId = data.PhieuMuon.Select(item => item.Id).DefaultIfEmpty(0).Max() + 1;
            data.PhieuMuon.Add(new PhieuMuon
            {
                Id = loanId,
                DocGiaId = reader.Id,
                NhanVienId = staff.Id,
                TenTaiKhoanNhanVien = staffAccount.Username,
                NgayMuon = DateTime.UtcNow,
                HanTra = model.NewLoan.DueAt.Date,
                TrangThai = "DangMuon"
            });
            data.ChiTietPhieuMuon.Add(new ChiTietPhieuMuon
            {
                Id = data.ChiTietPhieuMuon.Select(item => item.Id).DefaultIfEmpty(0).Max() + 1,
                PhieuMuonId = loanId,
                SachId = book.Id,
                SoLuong = model.NewLoan.Quantity
            });
            book.SoLuong -= model.NewLoan.Quantity;
            return true;
        });

        if (!result)
        {
            ModelState.AddModelError(string.Empty,
                "Không thể tạo phiếu. Hãy kiểm tra độc giả, sách còn sẵn và hạn trả.");
            await PopulatePageModelAsync(model);
            return View("Index", model);
        }

        TempData["Success"] = "Đã tạo phiếu mượn.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Return(int id)
    {
        var returned = await store.UpdateAsync(data =>
        {
            var loan = data.PhieuMuon.FirstOrDefault(item => item.Id == id);
            if (loan is null || loan.TrangThai == "DaTra")
            {
                return false;
            }

            var details = data.ChiTietPhieuMuon.Where(item => item.PhieuMuonId == id).ToList();
            if (details.Count == 0 || details.Any(detail =>
                    data.Sach.All(book => book.Id != detail.SachId)))
            {
                throw new InvalidDataException($"Phiếu mượn {id} có chi tiết sách không hợp lệ.");
            }

            foreach (var detail in details)
            {
                data.Sach.First(book => book.Id == detail.SachId).SoLuong += detail.SoLuong;
            }

            loan.TrangThai = "DaTra";
            return true;
        });

        TempData[returned ? "Success" : "Error"] = returned
            ? "Đã ghi nhận trả sách."
            : "Không tìm thấy phiếu mượn đang hoạt động.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulatePageModelAsync(LibraryLoanPageViewModel model)
    {
        var data = await store.ReadAsync(BuildPageModel);
        model.Loans = data.Loans;
        model.Readers = data.Readers;
        model.Books = data.Books;
    }

    private static LibraryLoanPageViewModel BuildPageModel(LibraryData data)
    {
        var readers = data.DocGia.ToDictionary(item => item.Id);
        var staff = data.NhanVien.ToDictionary(item => item.Id);
        var books = data.Sach.ToDictionary(item => item.Id);
        var today = DateTime.UtcNow.Date;
        var loanSummaries = data.PhieuMuon
            .OrderByDescending(item => item.NgayMuon)
            .Select(loan =>
            {
                var loanBooks = data.ChiTietPhieuMuon
                    .Where(detail => detail.PhieuMuonId == loan.Id)
                    .Select(detail => books.TryGetValue(detail.SachId, out var book)
                        ? $"{book.TenSach} × {detail.SoLuong}"
                        : $"Sách #{detail.SachId} × {detail.SoLuong}");
                var status = loan.TrangThai == "DaTra"
                    ? "Đã trả"
                    : loan.HanTra.Date < today ? "Quá hạn" : "Đang mượn";
                return new LibraryLoanSummaryViewModel
                {
                    Id = loan.Id,
                    ReaderName = readers.TryGetValue(loan.DocGiaId, out var reader)
                        ? reader.HoTen
                        : $"Độc giả #{loan.DocGiaId}",
                    StaffUsername = loan.TenTaiKhoanNhanVien ??
                        (staff.TryGetValue(loan.NhanVienId, out var employee)
                            ? data.Accounts.FirstOrDefault(account => account.Id == employee.IdentityUserId)?.Username
                            : null) ?? "Không rõ",
                    Books = string.Join(", ", loanBooks),
                    BorrowedAt = loan.NgayMuon,
                    DueAt = loan.HanTra,
                    Status = status
                };
            }).ToList();

        return new LibraryLoanPageViewModel
        {
            Loans = loanSummaries,
            Readers = data.DocGia.OrderBy(item => item.HoTen)
                .Select(item => new ReaderOptionViewModel { Id = item.Id, Name = item.HoTen })
                .ToList(),
            Books = data.Sach.Where(item => item.SoLuong > 0).OrderBy(item => item.TenSach)
                .Select(item => new BookOptionViewModel
                {
                    Id = item.Id,
                    Name = item.TenSach,
                    AvailableCopies = item.SoLuong
                }).ToList()
        };
    }
}
