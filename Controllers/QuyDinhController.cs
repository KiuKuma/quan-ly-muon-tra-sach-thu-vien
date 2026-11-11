using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using quan_ly_muon_tra_sach_thu_vien.Data;
using quan_ly_muon_tra_sach_thu_vien.Models.ViewModels;

namespace quan_ly_muon_tra_sach_thu_vien.Controllers;

[Authorize(Roles = LibraryRoles.QuanLy)]
public sealed class QuyDinhController(LibraryFileStore store) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var settings = await store.ReadAsync(data => data.Settings);
        ViewBag.UpdatedAt = settings.UpdatedAt;
        ViewBag.UpdatedBy = settings.UpdatedBy;
        return View(ToViewModel(settings));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(LibrarySettingsViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var currentSettings = await store.ReadAsync(data => data.Settings);
            ViewBag.UpdatedAt = currentSettings.UpdatedAt;
            ViewBag.UpdatedBy = currentSettings.UpdatedBy;
            return View(model);
        }

        await store.UpdateAsync(data =>
        {
            data.Settings = new LibrarySettings
            {
                LoanTermDays = model.LoanTermDays,
                MaxBooksPerReader = model.MaxBooksPerReader,
                DailyLateFine = model.DailyLateFine,
                DamagedBookFine = model.DamagedBookFine,
                LostBookFine = model.LostBookFine,
                ReaderMinimumAge = model.ReaderMinimumAge,
                ReaderMaximumAge = model.ReaderMaximumAge,
                ReaderCardValidityMonths = model.ReaderCardValidityMonths,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = User.Identity?.Name
            };
            return true;
        });

        TempData["Success"] = "Đã cập nhật quy định thư viện. Quy định mới áp dụng cho giao dịch tiếp theo.";
        return RedirectToAction(nameof(Index));
    }

    private static LibrarySettingsViewModel ToViewModel(LibrarySettings settings) => new()
    {
        LoanTermDays = settings.LoanTermDays,
        MaxBooksPerReader = settings.MaxBooksPerReader,
        DailyLateFine = settings.DailyLateFine,
        DamagedBookFine = settings.DamagedBookFine,
        LostBookFine = settings.LostBookFine,
        ReaderMinimumAge = settings.ReaderMinimumAge,
        ReaderMaximumAge = settings.ReaderMaximumAge,
        ReaderCardValidityMonths = settings.ReaderCardValidityMonths
    };
}
