using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using quan_ly_muon_tra_sach_thu_vien.Data;
using quan_ly_muon_tra_sach_thu_vien.Models;

namespace quan_ly_muon_tra_sach_thu_vien.Models.ViewModels;

public sealed class LibraryLoanPageViewModel
{
    public PagedResult<LibraryLoanSummaryViewModel> Loans { get; set; } =
        PagedResult<LibraryLoanSummaryViewModel>.Create([], 1);
    public int? FilterReaderId { get; set; }
    public string? FilterStatus { get; set; }
}

public sealed class BorrowPageViewModel
{
    public List<ReaderOptionViewModel> Readers { get; set; } = [];
    public List<BookOptionViewModel> Books { get; set; } = [];
    public int LoanTermDays { get; set; }
    public int MaxBooksPerReader { get; set; }
    public CreateLibraryLoanViewModel Loan { get; set; } = new();
}

public sealed class LibraryLoanSummaryViewModel
{
    public int Id { get; set; }
    public int ReaderId { get; set; }
    public string ReaderName { get; set; } = string.Empty;
    public string StaffUsername { get; set; } = string.Empty;
    public string Books { get; set; } = string.Empty;
    public DateTime BorrowedAt { get; set; }
    public DateTime DueAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string StatusCode { get; set; } = string.Empty;
    public string ReturnSlips { get; set; } = string.Empty;
}

public sealed class LibraryLoanDetailsViewModel
{
    public int Id { get; set; }
    public int ReaderId { get; set; }
    public string ReaderName { get; set; } = string.Empty;
    public string StaffUsername { get; set; } = string.Empty;
    public DateTime BorrowedAt { get; set; }
    public DateTime DueAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string StatusCode { get; set; } = string.Empty;
    public decimal OutstandingLoanFine { get; set; }
    public decimal ReaderOutstandingFine { get; set; }
    public decimal ReaderConfirmedFineDebt { get; set; }
    public List<ReaderPendingFineViewModel> ReaderPendingLoanFines { get; set; } = [];
    public List<LoanBookDetailsViewModel> Books { get; set; } = [];
    public List<LoanReturnDetailsViewModel> Returns { get; set; } = [];
}

public sealed class ReaderPendingFineViewModel
{
    public int LoanId { get; set; }
    public decimal Amount { get; set; }
}

public sealed class LoanBookDetailsViewModel
{
    public string Title { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int ReturnedQuantity { get; set; }
    public int OutstandingQuantity { get; set; }
}

public sealed class LoanReturnDetailsViewModel
{
    public int Id { get; set; }
    public DateTime ReturnedAt { get; set; }
    public decimal FineAmount { get; set; }
    public decimal FinePaid { get; set; }
    public decimal FineOutstanding { get; set; }
    public DateTime? FinePaidAt { get; set; }
    public string? FinePaidBy { get; set; }
    public List<LoanReturnBookDetailsViewModel> Books { get; set; } = [];
    public List<LoanFinePaymentViewModel> Payments { get; set; } = [];
}

public sealed class LoanFinePaymentViewModel
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaidAt { get; set; }
    public string StaffUsername { get; set; } = string.Empty;
}

public sealed class FinePaymentReceiptViewModel
{
    public int Id { get; set; }
    public int ReturnId { get; set; }
    public int LoanId { get; set; }
    public int ReaderId { get; set; }
    public string ReaderName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime PaidAt { get; set; }
    public string StaffUsername { get; set; } = string.Empty;
}

public sealed class LoanReturnBookDetailsViewModel
{
    public string Title { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal FineAmount { get; set; }
    public string Condition { get; set; } = string.Empty;
    public string? Note { get; set; }
}

public sealed class ReturnPageViewModel
{
    public int? FilterReaderId { get; set; }
    public PagedResult<ReturnableLoanViewModel> Loans { get; set; } =
        PagedResult<ReturnableLoanViewModel>.Create([], 1);
    public ReturnableLoanViewModel? SelectedLoan { get; set; }
    public CreateReturnViewModel Return { get; set; } = new();
}

public sealed class ReturnableLoanViewModel
{
    public int Id { get; set; }
    public int ReaderId { get; set; }
    public string ReaderName { get; set; } = string.Empty;
    public DateTime BorrowedAt { get; set; }
    public DateTime DueAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string StatusCode { get; set; } = string.Empty;
    public List<ReturnableBookViewModel> Books { get; set; } = [];
}

public sealed class ReturnableBookViewModel
{
    public int LoanDetailId { get; set; }
    public int BookId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int BorrowedQuantity { get; set; }
    public int AlreadyReturned { get; set; }
    public int OutstandingQuantity { get; set; }
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
    public string Author { get; set; } = string.Empty;
    public string? Publisher { get; set; }
    public int AvailableCopies { get; set; }
}

public sealed class BookBorrowingHistoryViewModel
{
    public string? Search { get; set; }
    public PagedResult<BookBorrowingHistoryRowViewModel> Results { get; set; } =
        PagedResult<BookBorrowingHistoryRowViewModel>.Create([], 1);
}

public sealed class BookBorrowingHistoryRowViewModel
{
    public int BookId { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public int LoanId { get; set; }
    public int ReaderId { get; set; }
    public string ReaderName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public DateTime BorrowedAt { get; set; }
    public DateTime DueAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string StatusCode { get; set; } = string.Empty;
}

public sealed class CreateLibraryLoanViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn độc giả.")]
    [Display(Name = "Độc giả")]
    public int ReaderId { get; set; }

    [Required, DataType(DataType.Date)]
    [Display(Name = "Ngày mượn")]
    public DateTime BorrowedAt { get; set; } = DateTime.Today;

    [Required, DataType(DataType.Date)]
    [Display(Name = "Hạn trả")]
    public DateTime DueAt { get; set; } = DateTime.Today.AddDays(14);

    [MinLength(1, ErrorMessage = "Vui lòng chọn ít nhất một sách.")]
    public List<BorrowBookItemViewModel> Items { get; set; } = [new()];
}

public sealed class BorrowBookItemViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn sách.")]
    public int BookId { get; set; }

    [Range(1, 100, ErrorMessage = "Số lượng mượn phải từ 1 đến 100.")]
    public int Quantity { get; set; } = 1;
}

public sealed class CreateReturnViewModel
{
    [Range(1, int.MaxValue)]
    public int LoanId { get; set; }

    public List<ReturnBookItemViewModel> Items { get; set; } = [];
}

public sealed class ReturnBookItemViewModel
{
    public int LoanDetailId { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Số lượng trả không hợp lệ.")]
    public int Quantity { get; set; }

    [Required]
    public string Condition { get; set; } = "Tot";

    [StringLength(300)]
    public string? Note { get; set; }
}

public sealed class ReaderHistoryViewModel
{
    public DocGia Reader { get; set; } = new();
    public List<ReaderHistoryLoanViewModel> Loans { get; set; } = [];
    public decimal OutstandingFine { get; set; }
}

public sealed class ReaderHistoryLoanViewModel
{
    public int Id { get; set; }
    public DateTime BorrowedAt { get; set; }
    public DateTime DueAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<string> Books { get; set; } = [];
    public decimal FinePaid { get; set; }
    public decimal FineOutstanding { get; set; }
}

public sealed class LibrarySettingsViewModel : IValidatableObject
{
    [Range(1, 90)]
    [Display(Name = "Số ngày mượn tối đa")]
    public int LoanTermDays { get; set; }

    [Range(1, 50)]
    [Display(Name = "Số bản sách tối đa mỗi độc giả")]
    public int MaxBooksPerReader { get; set; }

    [Range(typeof(decimal), "0", "100000000")]
    [Display(Name = "Phạt trễ hạn mỗi ngày (₫/cuốn)")]
    public decimal DailyLateFine { get; set; }

    [Range(typeof(decimal), "0", "100000000")]
    [Display(Name = "Phạt sách hư hỏng (₫/cuốn)")]
    public decimal DamagedBookFine { get; set; }

    [Range(typeof(decimal), "0", "100000000")]
    [Display(Name = "Phạt sách mất (₫/cuốn)")]
    public decimal LostBookFine { get; set; }

    [Range(0, 120)]
    [Display(Name = "Tuổi tối thiểu làm thẻ")]
    public int ReaderMinimumAge { get; set; }

    [Range(1, 120)]
    [Display(Name = "Tuổi tối đa làm thẻ")]
    public int ReaderMaximumAge { get; set; }

    [Range(1, 120)]
    [Display(Name = "Thời hạn thẻ (tháng)")]
    public int ReaderCardValidityMonths { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ReaderMinimumAge > ReaderMaximumAge)
        {
            yield return new ValidationResult(
                "Tuổi tối thiểu không được lớn hơn tuổi tối đa.",
                [nameof(ReaderMinimumAge), nameof(ReaderMaximumAge)]);
        }
    }
}

public sealed class InventoryAdjustmentViewModel
{
    public int BookId { get; set; }

    [Range(1, 100000)]
    [Display(Name = "Số bản cần loại khỏi kho")]
    public int Quantity { get; set; }

    [Required]
    [Display(Name = "Loại sự cố")]
    public string Type { get; set; } = string.Empty;

    [StringLength(300)]
    [Display(Name = "Ghi chú")]
    public string? Note { get; set; }
}

public sealed class InventoryAdjustmentPageViewModel
{
    [ValidateNever]
    public Sach Book { get; set; } = new();
    public InventoryAdjustmentViewModel Adjustment { get; set; } = new();
    [ValidateNever]
    public List<BookInventoryAdjustment> History { get; set; } = [];
}
