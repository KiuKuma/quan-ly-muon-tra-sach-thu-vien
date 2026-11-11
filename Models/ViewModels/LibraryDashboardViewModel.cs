namespace quan_ly_muon_tra_sach_thu_vien.Models.ViewModels;

public sealed class LibraryDashboardViewModel
{
    public int BookTitles { get; set; }
    public int AvailableCopies { get; set; }
    public int Categories { get; set; }
    public int Readers { get; set; }
    public int Staff { get; set; }
    public int TotalLoans { get; set; }
    public int ActiveLoans { get; set; }
    public int OverdueLoans { get; set; }
    public int ReturnedLoans { get; set; }
    public bool IsManager { get; set; }
    public DateTime FilterStartDate { get; set; }
    public DateTime FilterEndDate { get; set; }
    public string? DateRangeError { get; set; }
    public string ChartPeriodLabel { get; set; } = "Theo tháng";
    public List<MonthlyLoanViewModel> MonthlyLoans { get; set; } = [];
    public List<PopularBookViewModel> PopularBooks { get; set; } = [];
    public List<RecentLoanViewModel> RecentLoans { get; set; } = [];
}

public sealed class MonthlyLoanViewModel
{
    public string Label { get; set; } = string.Empty;
    public int LoanCount { get; set; }
    public int HeightPercent { get; set; }
}

public sealed class PopularBookViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public int LoanCount { get; set; }
    public int SharePercent { get; set; }
}

public sealed class RecentLoanViewModel
{
    public int Id { get; set; }
    public string ReaderName { get; set; } = string.Empty;
    public DateTime BorrowedAt { get; set; }
    public DateTime DueAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string StatusCode { get; set; } = string.Empty;
}

public sealed class LibraryCatalogViewModel
{
    public PagedResult<LibraryCatalogBookViewModel> Books { get; set; } =
        PagedResult<LibraryCatalogBookViewModel>.Create([], 1);
    public List<LibraryCatalogCategoryViewModel> Categories { get; set; } = [];
    public string? Search { get; set; }
    public int? CategoryId { get; set; }
    public bool AvailableOnly { get; set; }
}

public sealed class LibraryCatalogBookViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string? Publisher { get; set; }
    public int? PublishedYear { get; set; }
    public int AvailableCopies { get; set; }
    public string Category { get; set; } = string.Empty;
}

public sealed class LibraryCatalogCategoryViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int BookCount { get; set; }
}