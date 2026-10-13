using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using quan_ly_muon_tra_sach_thu_vien.Data;
using quan_ly_muon_tra_sach_thu_vien.Models;
using quan_ly_muon_tra_sach_thu_vien.Models.ViewModels;

namespace quan_ly_muon_tra_sach_thu_vien.Controllers;

public class HomeController(LibraryFileStore store) : Controller
{
    [Authorize]
    public async Task<IActionResult> Index()
    {
        var dashboard = await store.ReadAsync(data => new LibraryDashboardViewModel
        {
            BookTitles = data.Sach.Count,
            AvailableCopies = data.Sach.Sum(book => book.SoLuong),
            Categories = data.TheLoai.Count,
            Readers = data.DocGia.Count,
            TotalLoans = data.PhieuMuon.Count,
            ActiveLoans = data.PhieuMuon.Count(loan => loan.TrangThai is "DangMuon" or "QuaHan"),
            OverdueLoans = data.PhieuMuon.Count(loan =>
                loan.TrangThai == "QuaHan" ||
                (loan.TrangThai == "DangMuon" && loan.HanTra.Date < DateTime.UtcNow.Date)),
            ReturnedLoans = data.PhieuMuon.Count(loan => loan.TrangThai == "DaTra")
        });
        return View(dashboard);
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
