using System.ComponentModel.DataAnnotations;

namespace quan_ly_muon_tra_sach_thu_vien.Models.ViewModels;

public sealed class LibraryLoanPageViewModel
{
    public PagedResult<LibraryLoanSummaryViewModel> Loans { get; set; } =
        PagedResult<LibraryLoanSummaryViewModel>.Create([], 1);
    public List<ReaderOptionViewModel> Readers { get; set; } = [];
    public List<BookOptionViewModel> Books { get; set; } = [];
    public CreateLibraryLoanViewModel NewLoan { get; set; } = new();
}

public sealed class LibraryLoanSummaryViewModel
{
    public int Id { get; set; }
    public string ReaderName { get; set; } = string.Empty;
    public string StaffUsername { get; set; } = string.Empty;
    public string Books { get; set; } = string.Empty;
    public DateTime BorrowedAt { get; set; }
    public DateTime DueAt { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed class ReaderOptionViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class BookOptionViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int AvailableCopies { get; set; }
}

public sealed class CreateLibraryLoanViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn độc giả.")]
    [Display(Name = "Độc giả")]
    public int ReaderId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn sách.")]
    [Display(Name = "Sách")]
    public int BookId { get; set; }

    [Range(1, 100, ErrorMessage = "Số lượng mượn phải từ 1 đến 100.")]
    [Display(Name = "Số lượng")]
    public int Quantity { get; set; } = 1;

    [Required, DataType(DataType.Date)]
    [Display(Name = "Hạn trả")]
    public DateTime DueAt { get; set; } = DateTime.UtcNow.Date.AddDays(14);
}
