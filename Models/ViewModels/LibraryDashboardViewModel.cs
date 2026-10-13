namespace quan_ly_muon_tra_sach_thu_vien.Models.ViewModels;

public sealed class LibraryDashboardViewModel
{
    public int BookTitles { get; set; }
    public int AvailableCopies { get; set; }
    public int Categories { get; set; }
    public int Readers { get; set; }
    public int TotalLoans { get; set; }
    public int ActiveLoans { get; set; }
    public int OverdueLoans { get; set; }
    public int ReturnedLoans { get; set; }
}