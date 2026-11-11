using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using quan_ly_muon_tra_sach_thu_vien.Models;
using quan_ly_muon_tra_sach_thu_vien.Data;
using quan_ly_muon_tra_sach_thu_vien.Models.ViewModels;

namespace quan_ly_muon_tra_sach_thu_vien.Controllers;

public class HomeController(LibraryFileStore store) : Controller
{
    [Authorize]
    public async Task<IActionResult> Index()
    {
        var today = DateTime.UtcNow.Date;
        var model = await store.ReadAsync(data =>
        {
            var currentLoans = data.PhieuMuon
                .Where(loan => loan.TrangThai is "DangMuon" or "QuaHan" or "TraMotPhan")
                .ToList();
            return new LibraryOverviewViewModel
            {
                BookTitles = data.Sach.Count,
                AvailableCopies = data.Sach.Sum(book => book.SoLuong),
                Categories = data.TheLoai.Count,
                Readers = data.DocGia.Count,
                ActiveLoans = currentLoans.Count,
                OverdueLoans = currentLoans.Count(loan =>
                    loan.TrangThai == "QuaHan" || loan.HanTra.Date < today)
            };
        });

        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
