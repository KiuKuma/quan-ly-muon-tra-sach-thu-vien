namespace quan_ly_muon_tra_sach_thu_vien.Models.ViewModels;

public sealed class LibraryStatisticsViewModel
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalBorrowedSlips { get; set; }
    public int TotalReturnedSlips { get; set; }
    public decimal TotalFineRevenue { get; set; }
    public List<MonthlyLibraryStatisticsViewModel> MonthlyStatistics { get; set; } = [];
    public List<LoanStatusStatisticsViewModel> StatusStatistics { get; set; } = [];
    public List<PopularBookStatisticsViewModel> MostBorrowedBooks { get; set; } = [];
    public List<RecentLibraryLoanViewModel> RecentLoans { get; set; } = [];
}

public sealed class MonthlyLibraryStatisticsViewModel
{
    public string Label { get; set; } = string.Empty;
    public int BorrowedSlips { get; set; }
    public int ReturnedSlips { get; set; }
    public decimal FineRevenue { get; set; }
}

public sealed class LoanStatusStatisticsViewModel
{
    public string Code { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
}

public sealed class PopularBookStatisticsViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public int CopiesBorrowed { get; set; }
    public int BorrowingSlips { get; set; }
}

public sealed class RecentLibraryLoanViewModel
{
    public int Id { get; set; }
    public string ReaderName { get; set; } = string.Empty;
    public DateTime BorrowedAt { get; set; }
    public DateTime DueAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string StatusCode { get; set; } = string.Empty;
    public decimal FineOutstanding { get; set; }
}
