using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using quan_ly_muon_tra_sach_thu_vien.Data;
using quan_ly_muon_tra_sach_thu_vien.Models;
using quan_ly_muon_tra_sach_thu_vien.Models.ViewModels;

namespace quan_ly_muon_tra_sach_thu_vien.Controllers;

[Authorize(Roles = LibraryRoles.QuanLy)]
public sealed class BaoCaoController(LibraryFileStore store) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest("Ngày thống kê không hợp lệ.");
        }

        var today = DateTime.Today;
        var selectedEndDate = endDate?.Date ?? today;
        var selectedStartDate = startDate?.Date ?? new DateTime(today.Year, today.Month, 1).AddMonths(-5);
        if (selectedStartDate < new DateTime(2000, 1, 1) ||
            selectedEndDate < selectedStartDate ||
            selectedEndDate > today)
        {
            return BadRequest("Khoảng thời gian thống kê không hợp lệ.");
        }

        var startUtc = DateTime.SpecifyKind(selectedStartDate, DateTimeKind.Local).ToUniversalTime();
        var endUtc = DateTime.SpecifyKind(selectedEndDate.AddDays(1), DateTimeKind.Local).ToUniversalTime();
        var model = await store.ReadAsync(data => BuildStatistics(
            data, selectedStartDate, selectedEndDate, startUtc, endUtc));

        ViewData["WideContent"] = true;
        return View(model);
    }

    private static LibraryStatisticsViewModel BuildStatistics(
        LibraryData data,
        DateTime startDate,
        DateTime endDate,
        DateTime startUtc,
        DateTime endUtc)
    {
        var loansInRange = data.PhieuMuon
            .Where(loan => loan.NgayMuon >= startUtc && loan.NgayMuon < endUtc)
            .OrderByDescending(loan => loan.NgayMuon)
            .ToList();
        var loanIds = loansInRange.Select(loan => loan.Id).ToHashSet();
        var receipts = data.PhieuTra.ToDictionary(receipt => receipt.Id);
        var returnedByDetail = data.ChiTietPhieuTra
            .Where(detail => receipts.ContainsKey(detail.PhieuTraId))
            .GroupBy(detail => detail.ChiTietPhieuMuonId)
            .ToDictionary(group => group.Key, group => group.Sum(detail => detail.SoLuongTra));
        var loanDetails = data.ChiTietPhieuMuon
            .Where(detail => loanIds.Contains(detail.PhieuMuonId))
            .GroupBy(detail => detail.PhieuMuonId)
            .ToDictionary(group => group.Key, group => group.ToList());
        var statusCounts = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["DangMuon"] = 0,
            ["QuaHan"] = 0,
            ["TraMotPhan"] = 0,
            ["DaTra"] = 0
        };
        var today = DateTime.UtcNow.Date;
        foreach (var loan in loansInRange)
        {
            var details = loanDetails.GetValueOrDefault(loan.Id) ?? [];
            var status = GetLoanStatus(loan, details, returnedByDetail, today);
            statusCounts[status.Code]++;
        }

        var monthlyStatistics = BuildMonthlyStatistics(data, startDate, endDate, startUtc, endUtc);
        var paymentReturnIds = data.FinePayments.Select(payment => payment.ReturnId).ToHashSet();
        var totalFineRevenue = CalculateFineRevenue(data, paymentReturnIds, startUtc, endUtc);
        var books = data.Sach.ToDictionary(book => book.Id);
        var mostBorrowedBooks = data.ChiTietPhieuMuon
            .Where(detail => loanIds.Contains(detail.PhieuMuonId) && books.ContainsKey(detail.SachId))
            .GroupBy(detail => detail.SachId)
            .Select(group =>
            {
                var book = books[group.Key];
                return new PopularBookStatisticsViewModel
                {
                    Title = book.TenSach,
                    Author = book.TacGia,
                    CopiesBorrowed = group.Sum(detail => detail.SoLuong),
                    BorrowingSlips = group.Select(detail => detail.PhieuMuonId).Distinct().Count()
                };
            })
            .OrderByDescending(book => book.CopiesBorrowed)
            .ThenBy(book => book.Title)
            .Take(5)
            .ToList();
        var readers = data.DocGia.ToDictionary(reader => reader.Id);
        var returnFineTotals = data.ChiTietPhieuTra
            .GroupBy(detail => detail.PhieuTraId)
            .ToDictionary(group => group.Key, group => group.Sum(detail => Math.Max(detail.TienPhat, 0m)));
        var paymentsByReturn = data.FinePayments
            .GroupBy(payment => payment.ReturnId)
            .ToDictionary(group => group.Key, group => group.Sum(payment => Math.Max(payment.Amount, 0m)));
        var returnsByLoan = data.PhieuTra
            .GroupBy(receipt => receipt.PhieuMuonId)
            .ToDictionary(group => group.Key, group => group.ToList());
        var recentLoans = loansInRange
            .Take(10)
            .Select(loan =>
            {
                var details = loanDetails.GetValueOrDefault(loan.Id) ?? [];
                var status = GetLoanStatus(loan, details, returnedByDetail, today);
                var confirmedFineDebt = returnsByLoan.GetValueOrDefault(loan.Id, [])
                    .Sum(receipt => Math.Max(
                        returnFineTotals.GetValueOrDefault(receipt.Id) -
                        Math.Max(
                            paymentsByReturn.GetValueOrDefault(receipt.Id),
                            Math.Max(receipt.TienPhatDaThanhToan, 0m)),
                        0m));
                var lateDays = Math.Max((today - loan.HanTra.Date).Days, 0);
                var accruedFine = details.Sum(detail =>
                {
                    var outstandingQuantity = Math.Max(
                        detail.SoLuong - returnedByDetail.GetValueOrDefault(detail.Id), 0);
                    return lateDays * loan.MucPhatMoiNgay * outstandingQuantity;
                });
                return new RecentLibraryLoanViewModel
                {
                    Id = loan.Id,
                    ReaderName = readers.TryGetValue(loan.DocGiaId, out var reader)
                        ? reader.HoTen
                        : $"Độc giả #{loan.DocGiaId}",
                    BorrowedAt = loan.NgayMuon,
                    DueAt = loan.HanTra,
                    Status = status.Label,
                    StatusCode = status.Code,
                    FineOutstanding = confirmedFineDebt + accruedFine
                };
            })
            .ToList();

        return new LibraryStatisticsViewModel
        {
            StartDate = startDate,
            EndDate = endDate,
            TotalBorrowedSlips = loansInRange.Count,
            TotalReturnedSlips = data.PhieuTra.Count(receipt =>
                receipt.NgayTra >= startUtc && receipt.NgayTra < endUtc),
            TotalFineRevenue = totalFineRevenue,
            MonthlyStatistics = monthlyStatistics,
            StatusStatistics =
            [
                new LoanStatusStatisticsViewModel { Code = "DangMuon", Label = "Đang mượn", Count = statusCounts["DangMuon"] },
                new LoanStatusStatisticsViewModel { Code = "QuaHan", Label = "Quá hạn", Count = statusCounts["QuaHan"] },
                new LoanStatusStatisticsViewModel { Code = "TraMotPhan", Label = "Trả một phần", Count = statusCounts["TraMotPhan"] },
                new LoanStatusStatisticsViewModel { Code = "DaTra", Label = "Đã trả", Count = statusCounts["DaTra"] }
            ],
            MostBorrowedBooks = mostBorrowedBooks,
            RecentLoans = recentLoans
        };
    }

    private static List<MonthlyLibraryStatisticsViewModel> BuildMonthlyStatistics(
        LibraryData data,
        DateTime startDate,
        DateTime endDate,
        DateTime rangeStartUtc,
        DateTime rangeEndUtc)
    {
        var months = new List<MonthlyLibraryStatisticsViewModel>();
        var cursor = new DateTime(startDate.Year, startDate.Month, 1);
        var lastMonth = new DateTime(endDate.Year, endDate.Month, 1);
        var paymentReturnIds = data.FinePayments.Select(payment => payment.ReturnId).ToHashSet();
        while (cursor <= lastMonth)
        {
            var nextMonth = cursor.AddMonths(1);
            var monthStart = DateTime.SpecifyKind(
                cursor == new DateTime(startDate.Year, startDate.Month, 1) ? startDate : cursor,
                DateTimeKind.Local).ToUniversalTime();
            var monthEndDate = nextMonth > endDate ? endDate.AddDays(1) : nextMonth;
            var monthEnd = DateTime.SpecifyKind(monthEndDate, DateTimeKind.Local).ToUniversalTime();
            months.Add(new MonthlyLibraryStatisticsViewModel
            {
                Label = cursor.ToString("MM/yyyy"),
                BorrowedSlips = data.PhieuMuon.Count(loan =>
                    loan.NgayMuon >= monthStart && loan.NgayMuon < monthEnd &&
                    loan.NgayMuon >= rangeStartUtc && loan.NgayMuon < rangeEndUtc),
                ReturnedSlips = data.PhieuTra.Count(receipt =>
                    receipt.NgayTra >= monthStart && receipt.NgayTra < monthEnd &&
                    receipt.NgayTra >= rangeStartUtc && receipt.NgayTra < rangeEndUtc),
                FineRevenue = CalculateFineRevenue(data, paymentReturnIds,
                    Max(monthStart, rangeStartUtc), Min(monthEnd, rangeEndUtc))
            });
            cursor = nextMonth;
        }

        return months;
    }

    private static (string Code, string Label) GetLoanStatus(
        PhieuMuon loan,
        IReadOnlyCollection<ChiTietPhieuMuon> details,
        IReadOnlyDictionary<int, int> returnedByDetail,
        DateTime today)
    {
        if (loan.TrangThai == "DaTra" ||
            (details.Count > 0 && details.All(detail =>
                returnedByDetail.GetValueOrDefault(detail.Id) >= detail.SoLuong)))
        {
            return ("DaTra", "Đã trả");
        }

        if (loan.HanTra.Date < today)
        {
            return ("QuaHan", "Quá hạn");
        }

        if (details.Any(detail => returnedByDetail.GetValueOrDefault(detail.Id) > 0))
        {
            return ("TraMotPhan", "Trả một phần");
        }

        return ("DangMuon", "Đang mượn");
    }

    private static decimal CalculateFineRevenue(
        LibraryData data,
        HashSet<int> paymentReturnIds,
        DateTime start,
        DateTime end)
    {
        var recordedPayments = data.FinePayments
            .Where(payment => payment.PaidAt >= start && payment.PaidAt < end)
            .Sum(payment => payment.Amount);
        var legacyPayments = data.PhieuTra
            .Where(receipt => !paymentReturnIds.Contains(receipt.Id) &&
                receipt.NgayThanhToanTienPhat is DateTime paidAt && paidAt >= start && paidAt < end &&
                receipt.TienPhatDaThanhToan > 0)
            .Sum(receipt => receipt.TienPhatDaThanhToan);
        return recordedPayments + legacyPayments;
    }

    private static DateTime Max(DateTime first, DateTime second) => first >= second ? first : second;
    private static DateTime Min(DateTime first, DateTime second) => first <= second ? first : second;
}
