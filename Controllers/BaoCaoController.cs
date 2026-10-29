using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using quan_ly_muon_tra_sach_thu_vien.Data;
using quan_ly_muon_tra_sach_thu_vien.Models.ViewModels;

namespace quan_ly_muon_tra_sach_thu_vien.Controllers;

[Authorize(Roles = LibraryRoles.NhanVien + "," + LibraryRoles.QuanLy)]
public sealed class BaoCaoController(LibraryFileStore store) : Controller
{
    public async Task<IActionResult> Index(DateTime? fromDate, DateTime? toDate)
    {
        var today = DateTime.UtcNow.Date;
        var defaultStartDate = new DateTime(today.Year, today.Month, 1).AddMonths(-5);
        var endDate = toDate?.Date ?? today;
        var startDate = fromDate?.Date ?? (toDate.HasValue
            ? endDate < DateTime.MinValue.AddMonths(5)
                ? DateTime.MinValue
                : new DateTime(endDate.Year, endDate.Month, 1).AddMonths(-5)
            : defaultStartDate);
        var isManager = User.IsInRole(LibraryRoles.QuanLy);

        if (!ModelState.IsValid || startDate > endDate)
        {
            return View(new LibraryDashboardViewModel
            {
                IsManager = isManager,
                FilterStartDate = startDate,
                FilterEndDate = endDate,
                DateRangeError = !ModelState.IsValid
                    ? "Ngày lọc không hợp lệ. Vui lòng chọn ngày theo định dạng trên lịch."
                    : "Ngày bắt đầu không được sau ngày kết thúc."
            });
        }

        var firstMonth = new DateTime(startDate.Year, startDate.Month, 1);
        var monthCount = (endDate.Year - firstMonth.Year) * 12 + endDate.Month - firstMonth.Month + 1;
        var monthsPerBucket = Math.Max(1, (int)Math.Ceiling(monthCount / 24d));
        var bucketCount = (monthCount + monthsPerBucket - 1) / monthsPerBucket;

        var report = await store.ReadAsync(data =>
        {
            var filteredLoans = data.PhieuMuon
                .Where(loan => loan.NgayMuon.Date >= startDate && loan.NgayMuon.Date <= endDate)
                .ToList();
            var activeLoans = filteredLoans.Count(loan => loan.TrangThai is "DangMuon" or "QuaHan");
            var overdueLoans = filteredLoans.Count(loan =>
                loan.TrangThai == "QuaHan" ||
                (loan.TrangThai == "DangMuon" && loan.HanTra.Date < today));

            var monthlyCounts = filteredLoans
                .GroupBy(loan => ((loan.NgayMuon.Year - firstMonth.Year) * 12 +
                                  loan.NgayMuon.Month - firstMonth.Month) / monthsPerBucket)
                .ToDictionary(group => group.Key, group => group.Count());
            var monthlyLoans = Enumerable.Range(0, bucketCount)
                .Select(bucket =>
                {
                    var firstMonthOffset = bucket * monthsPerBucket;
                    var lastMonthOffset = Math.Min(firstMonthOffset + monthsPerBucket - 1, monthCount - 1);
                    var bucketStart = firstMonth.AddMonths(firstMonthOffset);
                    var bucketEnd = firstMonth.AddMonths(lastMonthOffset);
                    return new
                    {
                        Label = monthsPerBucket == 1
                            ? bucketStart.ToString("MM/yyyy")
                            : $"{bucketStart:MM/yyyy}–{bucketEnd:MM/yyyy}",
                        Count = monthlyCounts.GetValueOrDefault(bucket)
                    };
                })
                .ToList();
            var maximumMonthlyLoans = monthlyLoans.Max(item => item.Count);

            var filteredLoanIds = filteredLoans.Select(loan => loan.Id).ToHashSet();
            var popularBooks = data.ChiTietPhieuMuon
                .Where(detail => filteredLoanIds.Contains(detail.PhieuMuonId))
                .GroupBy(detail => detail.SachId)
                .Select(group => new
                {
                    Book = data.Sach.FirstOrDefault(book => book.Id == group.Key),
                    LoanCount = group.Sum(detail => detail.SoLuong)
                })
                .Where(item => item.Book is not null)
                .OrderByDescending(item => item.LoanCount)
                .ThenBy(item => item.Book!.TenSach)
                .Take(5)
                .ToList();
            var maximumBookLoans = popularBooks.Select(item => item.LoanCount).DefaultIfEmpty(0).Max();

            return new LibraryDashboardViewModel
            {
                IsManager = isManager,
                FilterStartDate = startDate,
                FilterEndDate = endDate,
                ChartPeriodLabel = monthsPerBucket == 1 ? "Theo tháng" : "Gộp theo khoảng tháng",
                BookTitles = data.Sach.Count,
                AvailableCopies = data.Sach.Sum(book => book.SoLuong),
                Categories = data.TheLoai.Count,
                Readers = data.DocGia.Count,
                Staff = data.NhanVien.Count,
                TotalLoans = filteredLoans.Count,
                ActiveLoans = activeLoans,
                OverdueLoans = overdueLoans,
                ReturnedLoans = filteredLoans.Count(loan => loan.TrangThai == "DaTra"),
                MonthlyLoans = monthlyLoans.Select(item => new MonthlyLoanViewModel
                {
                    Label = item.Label,
                    LoanCount = item.Count,
                    HeightPercent = maximumMonthlyLoans == 0 || item.Count == 0
                        ? 0
                        : Math.Max(10, item.Count * 100 / maximumMonthlyLoans)
                }).ToList(),
                PopularBooks = popularBooks.Select(item => new PopularBookViewModel
                {
                    Title = item.Book!.TenSach,
                    Author = item.Book.TacGia,
                    LoanCount = item.LoanCount,
                    SharePercent = maximumBookLoans == 0 ? 0 : item.LoanCount * 100 / maximumBookLoans
                }).ToList(),
                RecentLoans = filteredLoans
                    .OrderByDescending(loan => loan.NgayMuon)
                    .ThenByDescending(loan => loan.Id)
                    .Take(6)
                    .Select(loan =>
                    {
                        var statusCode = loan.TrangThai == "DaTra"
                            ? "DaTra"
                            : loan.TrangThai == "QuaHan" || loan.HanTra.Date < today
                                ? "QuaHan"
                                : "DangMuon";
                        return new RecentLoanViewModel
                        {
                            Id = loan.Id,
                            ReaderName = data.DocGia.FirstOrDefault(reader => reader.Id == loan.DocGiaId)?.HoTen
                                ?? $"Độc giả #{loan.DocGiaId}",
                            BorrowedAt = loan.NgayMuon,
                            DueAt = loan.HanTra,
                            StatusCode = statusCode,
                            Status = statusCode switch
                            {
                                "DaTra" => "Đã trả",
                                "QuaHan" => "Quá hạn",
                                _ => "Đang mượn"
                            }
                        };
                    })
                    .ToList()
            };
        });

        return View(report);
    }
}
