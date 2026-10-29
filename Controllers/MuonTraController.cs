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
    public async Task<IActionResult> Index(int? readerId, string? status, int page = 1)
    {
        ViewBag.ReaderId = readerId;
        ViewBag.Status = status;
        var model = await store.ReadAsync(data =>
        {
            var summaries = BuildLoanSummaries(data);
            if (readerId.HasValue)
            {
                summaries = summaries.Where(loan => loan.ReaderId == readerId.Value).ToList();
            }

            if (!string.IsNullOrWhiteSpace(status) && status != "TatCa")
            {
                summaries = summaries.Where(loan => loan.StatusCode == status).ToList();
            }

            return new LibraryLoanPageViewModel
            {
                Loans = PagedResult<LibraryLoanSummaryViewModel>.Create(summaries, page),
                FilterReaderId = readerId,
                FilterStatus = status
            };
        });

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var model = await store.ReadAsync(data => BuildLoanDetails(data, id));
        return model is null ? NotFound() : View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Borrow()
    {
        var model = await store.ReadAsync(BuildBorrowPage);
        if (model.Books.Count == 0)
        {
            TempData["Error"] = "Hiện không còn sách nào có thể cho mượn.";
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Borrow(BorrowPageViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateBorrowPageAsync(model);
            return View(model);
        }

        if (model.Loan.Items.Count == 0)
        {
            ModelState.AddModelError(nameof(model.Loan.Items), "Vui lòng chọn ít nhất một sách.");
            await PopulateBorrowPageAsync(model);
            return View(model);
        }

        var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var result = await store.UpdateAsync(data =>
        {
            var reader = data.DocGia.FirstOrDefault(item => item.Id == model.Loan.ReaderId);
            var staff = data.NhanVien.FirstOrDefault(item => item.IdentityUserId == accountId);
            var staffAccount = data.Accounts.FirstOrDefault(item => item.Id == accountId);
            if (reader is null || staff is null || staffAccount is null)
            {
                return (Success: false, Error: "Không tìm thấy độc giả hoặc nhân viên hợp lệ.");
            }

            var outstandingBookCount = GetReaderOutstandingBookDebt(data, reader.Id);
            if (outstandingBookCount > 0)
            {
                return (Success: false,
                    Error: $"Độc giả còn nợ {outstandingBookCount} cuốn sách chưa trả. Vui lòng nhận lại sách trước khi lập phiếu mượn mới.");
            }

            var outstandingFine = GetReaderFineDebt(data, reader.Id);
            if (outstandingFine > 0)
            {
                return (Success: false,
                    Error: $"Độc giả còn nợ phạt {outstandingFine:N0} ₫. Vui lòng thanh toán trước khi mượn sách.");
            }

            var borrowedDate = model.Loan.BorrowedAt.Date;
            var dueDate = model.Loan.DueAt.Date;
            if (borrowedDate == DateTime.MinValue || dueDate == DateTime.MinValue ||
                borrowedDate > DateTime.Today || dueDate < borrowedDate || dueDate < DateTime.Today)
            {
                return (Success: false, Error: "Ngày mượn không được ở tương lai; hạn trả phải từ ngày mượn và không ở quá khứ.");
            }

            var quantities = model.Loan.Items
                .GroupBy(item => item.BookId)
                .ToDictionary(group => group.Key, group => group.Sum(item => item.Quantity));
            foreach (var (bookId, quantity) in quantities)
            {
                var book = data.Sach.FirstOrDefault(item => item.Id == bookId);
                if (book is null)
                {
                    return (Success: false, Error: $"Sách mã {bookId} không còn trong danh mục hoặc đã hết sách.");
                }

                if (quantity <= 0)
                {
                    return (Success: false, Error: "Số lượng mượn phải lớn hơn 0.");
                }

                if (book.SoLuong < quantity)
                {
                    return (Success: false,
                        Error: $"Sách “{book.TenSach}” không đủ tồn kho: hiện còn {book.SoLuong} cuốn.");
                }
            }

            var loanId = data.PhieuMuon.Select(item => item.Id).DefaultIfEmpty(0).Max() + 1;
            data.PhieuMuon.Add(new PhieuMuon
            {
                Id = loanId,
                DocGiaId = reader.Id,
                NhanVienId = staff.Id,
                TenTaiKhoanNhanVien = staffAccount.Username,
                NgayMuon = DateTime.SpecifyKind(borrowedDate, DateTimeKind.Utc),
                HanTra = DateTime.SpecifyKind(dueDate, DateTimeKind.Utc),
                TrangThai = "DangMuon"
            });

            var nextDetailId = data.ChiTietPhieuMuon.Select(item => item.Id).DefaultIfEmpty(0).Max() + 1;
            foreach (var (bookId, quantity) in quantities)
            {
                data.ChiTietPhieuMuon.Add(new ChiTietPhieuMuon
                {
                    Id = nextDetailId++,
                    PhieuMuonId = loanId,
                    SachId = bookId,
                    SoLuong = quantity
                });
                data.Sach.First(item => item.Id == bookId).SoLuong -= quantity;
            }

            return (Success: true, Error: string.Empty);
        });

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            await PopulateBorrowPageAsync(model);
            return View(model);
        }

        TempData["Success"] = "Đã tạo phiếu mượn.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Return(int? readerId, int? loanId, int page = 1)
    {
        var model = await store.ReadAsync(data =>
        {
            var loans = BuildReturnableLoans(data)
                .Where(loan => !readerId.HasValue || loan.ReaderId == readerId.Value)
                .ToList();
            var pagedLoans = PagedResult<ReturnableLoanViewModel>.Create(loans, page);
            return new ReturnPageViewModel
            {
                FilterReaderId = readerId,
                Loans = pagedLoans,
                SelectedLoan = loanId.HasValue ? loans.FirstOrDefault(loan => loan.Id == loanId.Value) : null,
                Return = loanId.HasValue
                    ? new CreateReturnViewModel
                    {
                        LoanId = loanId.Value,
                        Items = loans.FirstOrDefault(loan => loan.Id == loanId.Value)?.Books
                            .Select(book => new ReturnBookItemViewModel
                            {
                                LoanDetailId = book.LoanDetailId
                            }).ToList() ?? []
                    }
                    : new CreateReturnViewModel()
            };
        });

        if (loanId.HasValue && model.SelectedLoan is null)
        {
            return NotFound();
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Return(ReturnPageViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateReturnPageAsync(model);
            return View(model);
        }

        if (model.Return.Items.All(item => item.Quantity == 0))
        {
            ModelState.AddModelError(string.Empty, "Nhập số lượng trả cho ít nhất một đầu sách.");
            await PopulateReturnPageAsync(model);
            return View(model);
        }

        var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var result = await store.UpdateAsync(data =>
        {
            var loan = data.PhieuMuon.FirstOrDefault(item => item.Id == model.Return.LoanId);
            var staff = data.NhanVien.FirstOrDefault(item => item.IdentityUserId == accountId);
            var staffAccount = data.Accounts.FirstOrDefault(item => item.Id == accountId);
            if (loan is null || staff is null || staffAccount is null || loan.TrangThai == "DaTra")
            {
                return (Success: false, Error: "Không tìm thấy phiếu mượn còn sách cần trả.", FineAmount: 0m);
            }

            var details = data.ChiTietPhieuMuon.Where(item => item.PhieuMuonId == loan.Id).ToDictionary(item => item.Id);
            var priorReturns = data.ChiTietPhieuTra
                .Where(item => data.PhieuTra.Any(receipt => receipt.Id == item.PhieuTraId))
                .GroupBy(item => item.ChiTietPhieuMuonId)
                .ToDictionary(group => group.Key, group => group.Sum(item => item.SoLuongTra));
            var returnItems = model.Return.Items.Where(item => item.Quantity > 0).ToList();
            if (returnItems.Select(item => item.LoanDetailId).Distinct().Count() != returnItems.Count)
            {
                return (Success: false, Error: "Một đầu sách chỉ được chọn một lần trong phiếu trả.", FineAmount: 0m);
            }

            foreach (var item in returnItems)
            {
                if (!details.TryGetValue(item.LoanDetailId, out var detail))
                {
                    return (Success: false, Error: "Chi tiết sách không thuộc phiếu mượn đã chọn.", FineAmount: 0m);
                }

                var alreadyReturned = priorReturns.GetValueOrDefault(detail.Id);
                if (item.Quantity > detail.SoLuong - alreadyReturned ||
                    !data.Sach.Any(book => book.Id == detail.SachId) ||
                    item.Condition is not ("Tot" or "HuHong"))
                {
                    return (Success: false, Error: "Số lượng trả vượt số còn mượn hoặc tình trạng sách không hợp lệ.", FineAmount: 0m);
                }
            }

            var returnedAt = DateTime.UtcNow;
            var lateDays = Math.Max((returnedAt.Date - loan.HanTra.Date).Days, 0);
            var returnId = data.PhieuTra.Select(item => item.Id).DefaultIfEmpty(0).Max() + 1;
            data.PhieuTra.Add(new PhieuTra
            {
                Id = returnId,
                PhieuMuonId = loan.Id,
                NhanVienId = staff.Id,
                TenTaiKhoanNhanVien = staffAccount.Username,
                NgayTra = returnedAt
            });

            var nextReturnDetailId = data.ChiTietPhieuTra.Select(item => item.Id).DefaultIfEmpty(0).Max() + 1;
            var fineTotal = 0m;
            foreach (var item in returnItems)
            {
                var detail = details[item.LoanDetailId];
                var fine = CalculateProgressiveFine(lateDays, item.Quantity);
                fineTotal += fine;
                data.ChiTietPhieuTra.Add(new ChiTietPhieuTra
                {
                    Id = nextReturnDetailId++,
                    PhieuTraId = returnId,
                    ChiTietPhieuMuonId = detail.Id,
                    SoLuongTra = item.Quantity,
                    TienPhat = fine,
                    TinhTrang = item.Condition,
                    GhiChu = item.Note?.Trim()
                });

                if (item.Condition == "Tot")
                {
                    data.Sach.First(book => book.Id == detail.SachId).SoLuong += item.Quantity;
                }
            }

            var returnedAfterThisReceipt = priorReturns;
            foreach (var item in returnItems)
            {
                returnedAfterThisReceipt[item.LoanDetailId] =
                    returnedAfterThisReceipt.GetValueOrDefault(item.LoanDetailId) + item.Quantity;
            }

            if (details.Values.All(detail =>
                    returnedAfterThisReceipt.GetValueOrDefault(detail.Id) >= detail.SoLuong))
            {
                loan.TrangThai = "DaTra";
            }
            else
            {
                loan.TrangThai = loan.HanTra.Date < DateTime.UtcNow.Date ? "QuaHan" : "DangMuon";
            }

            return (Success: true, Error: string.Empty, FineAmount: fineTotal);
        });

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            await PopulateReturnPageAsync(model);
            return View(model);
        }

        TempData["Success"] = result.FineAmount > 0
            ? $"Đã lập phiếu trả, cập nhật tồn kho và ghi nhận tiền phạt {result.FineAmount:N0} ₫."
            : "Đã lập phiếu trả và cập nhật số lượng sách.";
        return RedirectToAction(nameof(Details), new { id = model.Return.LoanId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PayFine(int id, int returnId)
    {
        var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var result = await store.UpdateAsync(data =>
        {
            var loan = data.PhieuMuon.FirstOrDefault(item => item.Id == id);
            var receipt = data.PhieuTra.FirstOrDefault(item =>
                item.Id == returnId && item.PhieuMuonId == id);
            var staff = data.NhanVien.FirstOrDefault(item => item.IdentityUserId == accountId);
            var staffAccount = data.Accounts.FirstOrDefault(item => item.Id == accountId);
            if (loan is null || receipt is null || staff is null || staffAccount is null)
            {
                return (Success: false, Error: "Không tìm thấy phiếu trả hoặc nhân viên hợp lệ.", Amount: 0m);
            }

            var fineTotal = data.ChiTietPhieuTra
                .Where(item => item.PhieuTraId == receipt.Id)
                .Sum(item => Math.Max(item.TienPhat, 0m));
            var outstanding = Math.Max(fineTotal - receipt.TienPhatDaThanhToan, 0m);
            if (outstanding <= 0)
            {
                return (Success: false, Error: "Phiếu trả này không còn nợ tiền phạt.", Amount: 0m);
            }

            receipt.TienPhatDaThanhToan = fineTotal;
            receipt.NgayThanhToanTienPhat = DateTime.UtcNow;
            receipt.TenTaiKhoanThuTienPhat = staffAccount.Username;
            return (Success: true, Error: string.Empty, Amount: outstanding);
        });

        if (!result.Success)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = $"Đã xác nhận thu {result.Amount:N0} ₫ tiền phạt.";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task PopulateBorrowPageAsync(BorrowPageViewModel model)
    {
        var options = await store.ReadAsync(BuildBorrowPage);
        model.Readers = options.Readers;
        model.Books = options.Books;
    }

    private async Task PopulateReturnPageAsync(ReturnPageViewModel model)
    {
        var refreshed = await store.ReadAsync(data =>
        {
            var loans = BuildReturnableLoans(data)
                .Where(loan => !model.FilterReaderId.HasValue || loan.ReaderId == model.FilterReaderId.Value)
                .ToList();
            return new ReturnPageViewModel
            {
                FilterReaderId = model.FilterReaderId,
                Loans = PagedResult<ReturnableLoanViewModel>.Create(loans, 1),
                SelectedLoan = loans.FirstOrDefault(loan => loan.Id == model.Return.LoanId)
            };
        });
        model.Loans = refreshed.Loans;
        model.SelectedLoan = refreshed.SelectedLoan;
    }

    private static BorrowPageViewModel BuildBorrowPage(LibraryData data) => new()
    {
        Readers = data.DocGia.OrderBy(item => item.Id).Select(item => new ReaderOptionViewModel
        {
            Id = item.Id,
            Name = item.HoTen
        }).ToList(),
        Books = data.Sach.Where(item => item.SoLuong > 0).OrderBy(item => item.TenSach)
            .Select(item => new BookOptionViewModel
            {
                Id = item.Id,
                Name = item.TenSach,
                Author = item.TacGia,
                Publisher = item.NhaXuatBan,
                AvailableCopies = item.SoLuong
            }).ToList()
    };

    private static List<LibraryLoanSummaryViewModel> BuildLoanSummaries(LibraryData data)
    {
        var readers = data.DocGia.ToDictionary(item => item.Id);
        var books = data.Sach.ToDictionary(item => item.Id);
        var returns = data.PhieuTra.GroupBy(item => item.PhieuMuonId)
            .ToDictionary(group => group.Key, group => group.OrderBy(item => item.NgayTra).ToList());
        var returnedByLoan = BuildReturnedQuantitiesByLoan(data);

        return data.PhieuMuon.OrderByDescending(item => item.NgayMuon).Select(loan =>
        {
            var loanDetails = data.ChiTietPhieuMuon.Where(detail => detail.PhieuMuonId == loan.Id).ToList();
            var returnedQuantities = returnedByLoan.GetValueOrDefault(loan.Id) ?? [];
            var status = GetLoanStatus(loan, loanDetails, returnedQuantities, DateTime.UtcNow.Date);
            var loanBooks = loanDetails.Select(detail =>
                $"{(books.TryGetValue(detail.SachId, out var book) ? book.TenSach : $"Sách #{detail.SachId}")} × {detail.SoLuong}");
            var returnSlipSummary = returns.TryGetValue(loan.Id, out var receipts)
                ? string.Join(", ", receipts.Select(receipt => $"PT-{receipt.Id} ({receipt.NgayTra.ToLocalTime():dd/MM/yyyy})"))
                : "Chưa trả";
            var employee = data.NhanVien.FirstOrDefault(item => item.Id == loan.NhanVienId);
            var staffUsername = loan.TenTaiKhoanNhanVien ??
                data.Accounts.FirstOrDefault(account => account.Id == employee?.IdentityUserId)?.Username ?? "Không rõ";

            return new LibraryLoanSummaryViewModel
            {
                Id = loan.Id,
                ReaderId = loan.DocGiaId,
                ReaderName = readers.TryGetValue(loan.DocGiaId, out var reader)
                    ? reader.HoTen
                    : $"Độc giả #{loan.DocGiaId}",
                StaffUsername = staffUsername,
                Books = string.Join(", ", loanBooks),
                BorrowedAt = loan.NgayMuon,
                DueAt = loan.HanTra,
                Status = status.Label,
                StatusCode = status.Code,
                ReturnSlips = returnSlipSummary
            };
        }).ToList();
    }

    private static LibraryLoanDetailsViewModel? BuildLoanDetails(LibraryData data, int loanId)
    {
        var loan = data.PhieuMuon.FirstOrDefault(item => item.Id == loanId);
        if (loan is null)
        {
            return null;
        }

        var books = data.Sach.ToDictionary(item => item.Id);
        var readers = data.DocGia.ToDictionary(item => item.Id);
        var details = data.ChiTietPhieuMuon.Where(item => item.PhieuMuonId == loan.Id).ToList();
        var returnedByLoan = BuildReturnedQuantitiesByLoan(data).GetValueOrDefault(loan.Id) ?? [];
        var status = GetLoanStatus(loan, details, returnedByLoan, DateTime.UtcNow.Date);
        var employee = data.NhanVien.FirstOrDefault(item => item.Id == loan.NhanVienId);
        var staffUsername = loan.TenTaiKhoanNhanVien ??
            data.Accounts.FirstOrDefault(account => account.Id == employee?.IdentityUserId)?.Username ?? "Không rõ";
        var receipts = data.PhieuTra.Where(item => item.PhieuMuonId == loan.Id)
            .OrderBy(item => item.NgayTra).ToList();
        var receiptIds = receipts.Select(item => item.Id).ToHashSet();
        var returnDetails = data.ChiTietPhieuTra.Where(item => receiptIds.Contains(item.PhieuTraId)).ToList();

        return new LibraryLoanDetailsViewModel
        {
            Id = loan.Id,
            ReaderId = loan.DocGiaId,
            ReaderName = readers.GetValueOrDefault(loan.DocGiaId)?.HoTen ?? $"Độc giả #{loan.DocGiaId}",
            StaffUsername = staffUsername,
            BorrowedAt = loan.NgayMuon,
            DueAt = loan.HanTra,
            Status = status.Label,
            StatusCode = status.Code,
            OutstandingLoanFine = GetLoanOutstandingFine(data, loan, DateTime.UtcNow.Date),
            ReaderOutstandingFine = GetReaderFineDebt(data, loan.DocGiaId),
            Books = details.Select(detail =>
            {
                var returned = returnedByLoan.GetValueOrDefault(detail.Id);
                return new LoanBookDetailsViewModel
                {
                    Title = books.GetValueOrDefault(detail.SachId)?.TenSach ?? $"Sách #{detail.SachId}",
                    Quantity = detail.SoLuong,
                    ReturnedQuantity = returned,
                    OutstandingQuantity = Math.Max(detail.SoLuong - returned, 0)
                };
            }).ToList(),
            Returns = receipts.Select(receipt =>
            {
                var receiptDetails = returnDetails.Where(item => item.PhieuTraId == receipt.Id).ToList();
                var fine = receiptDetails.Sum(item => Math.Max(item.TienPhat, 0m));
                var paid = Math.Min(Math.Max(receipt.TienPhatDaThanhToan, 0m), fine);
                return new LoanReturnDetailsViewModel
                {
                    Id = receipt.Id,
                    ReturnedAt = receipt.NgayTra,
                    FineAmount = fine,
                    FinePaid = paid,
                    FineOutstanding = fine - paid,
                    FinePaidAt = receipt.NgayThanhToanTienPhat,
                    FinePaidBy = receipt.TenTaiKhoanThuTienPhat,
                    Books = receiptDetails.Select(returnDetail =>
                    {
                        var loanDetail = details.FirstOrDefault(item =>
                            item.Id == returnDetail.ChiTietPhieuMuonId);
                        return new LoanReturnBookDetailsViewModel
                        {
                            Title = loanDetail is not null &&
                                    books.TryGetValue(loanDetail.SachId, out var book)
                                ? book.TenSach
                                : "Sách không còn trong danh mục",
                            Quantity = returnDetail.SoLuongTra,
                            FineAmount = returnDetail.TienPhat,
                            Condition = returnDetail.TinhTrang,
                            Note = returnDetail.GhiChu
                        };
                    }).ToList()
                };
            }).ToList()
        };
    }

    private static decimal GetReaderFineDebt(LibraryData data, int readerId)
    {
        var today = DateTime.UtcNow.Date;
        var readerLoanIds = data.PhieuMuon
            .Where(item => item.DocGiaId == readerId)
            .Select(item => item.Id)
            .ToHashSet();
        var receiptDebt = data.PhieuTra
            .Where(receipt => readerLoanIds.Contains(receipt.PhieuMuonId))
            .Sum(receipt =>
            {
                var fineTotal = data.ChiTietPhieuTra
                    .Where(item => item.PhieuTraId == receipt.Id)
                    .Sum(item => Math.Max(item.TienPhat, 0m));
                return Math.Max(fineTotal - Math.Max(receipt.TienPhatDaThanhToan, 0m), 0m);
            });
        var accruedLoanFine = data.PhieuMuon
            .Where(loan => loan.DocGiaId == readerId)
            .Sum(loan => GetLoanOutstandingFine(data, loan, today));
        return receiptDebt + accruedLoanFine;
    }

    private static int GetReaderOutstandingBookDebt(LibraryData data, int readerId)
    {
        var returnedByLoan = BuildReturnedQuantitiesByLoan(data);
        return data.PhieuMuon
            .Where(loan => loan.DocGiaId == readerId)
            .Sum(loan =>
            {
                var returned = returnedByLoan.GetValueOrDefault(loan.Id) ?? [];
                return data.ChiTietPhieuMuon
                    .Where(detail => detail.PhieuMuonId == loan.Id)
                    .Sum(detail => Math.Max(detail.SoLuong - returned.GetValueOrDefault(detail.Id), 0));
            });
    }

    private static decimal GetLoanOutstandingFine(LibraryData data, PhieuMuon loan, DateTime today)
    {
        var lateDays = Math.Max((today - loan.HanTra.Date).Days, 0);
        if (lateDays == 0)
        {
            return 0m;
        }

        var returnedByDetail = BuildReturnedQuantitiesByLoan(data).GetValueOrDefault(loan.Id) ?? [];
        return data.ChiTietPhieuMuon
            .Where(detail => detail.PhieuMuonId == loan.Id)
            .Sum(detail =>
            {
                var outstandingQuantity = Math.Max(detail.SoLuong - returnedByDetail.GetValueOrDefault(detail.Id), 0);
                return CalculateProgressiveFine(lateDays, outstandingQuantity);
            });
    }

    private static decimal CalculateProgressiveFine(int lateDays, int quantity)
    {
        if (lateDays <= 0 || quantity <= 0)
        {
            return 0m;
        }

        var firstTierDays = Math.Min(lateDays, 7);
        var secondTierDays = Math.Min(Math.Max(lateDays - 7, 0), 23);
        var thirdTierDays = Math.Max(lateDays - 30, 0);
        return quantity * (firstTierDays * 1_000m + secondTierDays * 2_000m + thirdTierDays * 5_000m);
    }

    private static List<ReturnableLoanViewModel> BuildReturnableLoans(LibraryData data)
    {
        var readers = data.DocGia.ToDictionary(item => item.Id);
        var books = data.Sach.ToDictionary(item => item.Id);
        var returnedByLoan = BuildReturnedQuantitiesByLoan(data);
        return data.PhieuMuon
            .Where(loan => loan.TrangThai != "DaTra")
            .OrderByDescending(loan => loan.NgayMuon)
            .Select(loan =>
            {
                var reader = readers.GetValueOrDefault(loan.DocGiaId);
                var details = data.ChiTietPhieuMuon.Where(item => item.PhieuMuonId == loan.Id);
                var detailList = details.ToList();
                var returnedQuantities = returnedByLoan.GetValueOrDefault(loan.Id) ?? [];
                var bookItems = detailList.Select(detail =>
                {
                    var returned = returnedQuantities.GetValueOrDefault(detail.Id);
                    return new ReturnableBookViewModel
                    {
                        LoanDetailId = detail.Id,
                        BookId = detail.SachId,
                        Title = books.TryGetValue(detail.SachId, out var book)
                            ? book.TenSach
                            : $"Sách #{detail.SachId}",
                        BorrowedQuantity = detail.SoLuong,
                        AlreadyReturned = returned,
                        OutstandingQuantity = Math.Max(detail.SoLuong - returned, 0)
                    };
                }).Where(book => book.OutstandingQuantity > 0).ToList();

                var status = GetLoanStatus(loan, detailList, returnedQuantities, DateTime.UtcNow.Date);
                return new ReturnableLoanViewModel
                {
                    Id = loan.Id,
                    ReaderId = loan.DocGiaId,
                    ReaderName = reader?.HoTen ?? $"Độc giả #{loan.DocGiaId}",
                    BorrowedAt = loan.NgayMuon,
                    DueAt = loan.HanTra,
                    Status = status.Label,
                    StatusCode = status.Code,
                    Books = bookItems
                };
            })
            .Where(loan => loan.Books.Count > 0)
            .ToList();
    }

    private static Dictionary<int, Dictionary<int, int>> BuildReturnedQuantitiesByLoan(LibraryData data)
    {
        var detailLoans = data.ChiTietPhieuMuon.ToDictionary(item => item.Id, item => item.PhieuMuonId);
        var receiptIds = data.PhieuTra.Select(item => item.Id).ToHashSet();
        return data.ChiTietPhieuTra
            .Where(item => receiptIds.Contains(item.PhieuTraId) &&
                           detailLoans.ContainsKey(item.ChiTietPhieuMuonId))
            .GroupBy(item => detailLoans[item.ChiTietPhieuMuonId])
            .ToDictionary(
                loanGroup => loanGroup.Key,
                loanGroup => loanGroup.GroupBy(item => item.ChiTietPhieuMuonId)
                    .ToDictionary(detailGroup => detailGroup.Key, detailGroup => detailGroup.Sum(item => item.SoLuongTra)));
    }

    private static (string Code, string Label) GetLoanStatus(
        PhieuMuon loan,
        IReadOnlyCollection<ChiTietPhieuMuon> details,
        IReadOnlyDictionary<int, int> returnedQuantities,
        DateTime today)
    {
        if (loan.TrangThai == "DaTra" || (details.Count > 0 && details.All(detail =>
                returnedQuantities.GetValueOrDefault(detail.Id) >= detail.SoLuong)))
        {
            return ("DaTra", "Đã trả");
        }

        if (loan.HanTra.Date < today)
        {
            return ("QuaHan", "Quá hạn");
        }

        if (returnedQuantities.Values.Sum() > 0)
        {
            return ("TraMotPhan", "Trả một phần");
        }

        return ("DangMuon", "Đang mượn");
    }
}
