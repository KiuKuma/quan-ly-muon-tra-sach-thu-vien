using quan_ly_muon_tra_sach_thu_vien.Models;

namespace quan_ly_muon_tra_sach_thu_vien.Data;

public sealed class LibrarySampleDataSeeder(LibraryFileStore store)
{
    private static readonly (string Name, string Description)[] SampleCategories =
    [
        ("Lập trình", "Sách ngôn ngữ lập trình, thuật toán và thực hành phát triển phần mềm."),
        ("Công nghệ", "Thiết kế sản phẩm số, kiến trúc và hệ thống công nghệ."),
        ("Kỹ năng", "Sách kỹ năng học tập, tư duy và phát triển bản thân."),
        ("Văn học", "Tác phẩm văn học kinh điển và hiện đại."),
        ("Thiếu nhi", "Sách dành cho thiếu nhi và bạn đọc trẻ.")
    ];

    private static readonly SampleBook[] SampleBooks =
    [
        new("9780132350884", "Clean Code: A Handbook of Agile Software Craftsmanship", "Robert C. Martin", "Prentice Hall", 2008, "Lập trình", 5),
        new("9780135957059", "The Pragmatic Programmer: 20th Anniversary Edition", "David Thomas, Andrew Hunt", "Addison-Wesley Professional", 2019, "Lập trình", 4),
        new("9781491910771", "Head First Java: A Brain-Friendly Guide", "Kathy Sierra, Bert Bates, Trisha Gee", "O'Reilly Media", 2022, "Lập trình", 3),
        new("9780321965516", "Don't Make Me Think, Revisited", "Steve Krug", "New Riders", 2014, "Công nghệ", 4),
        new("9780321125217", "Domain-Driven Design: Tackling Complexity in the Heart of Software", "Eric Evans", "Addison-Wesley Professional", 2003, "Công nghệ", 2),
        new("9780735211292", "Atomic Habits", "James Clear", "Avery", 2018, "Kỹ năng", 6),
        new("9780061120084", "To Kill a Mockingbird", "Harper Lee", "Harper Perennial Modern Classics", 2006, "Văn học", 3),
        new("9780747532699", "Harry Potter and the Philosopher's Stone", "J. K. Rowling", "Bloomsbury", 1997, "Thiếu nhi", 4)
    ];

    private static readonly SampleReader[] SampleReaders =
    [
        new("Độc giả mẫu - Minh An", "minh.an@example.test", "0900000101", "Dữ liệu minh họa"),
        new("Độc giả mẫu - Gia Huy", "gia.huy@example.test", "0900000102", "Dữ liệu minh họa"),
        new("Độc giả mẫu - Thu Hà", "thu.ha@example.test", "0900000103", "Dữ liệu minh họa")
    ];

    public Task SeedAsync()
    {
        return store.UpdateAsync(data =>
        {
            foreach (var sample in SampleCategories)
            {
                if (data.TheLoai.Any(category => category.TenTheLoai.Equals(
                        sample.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                data.TheLoai.Add(new TheLoai
                {
                    Id = NextId(data.TheLoai.Select(category => category.Id)),
                    TenTheLoai = sample.Name,
                    MoTa = sample.Description
                });
            }

            var categoryIds = data.TheLoai.ToDictionary(category => category.TenTheLoai,
                category => category.Id, StringComparer.OrdinalIgnoreCase);
            foreach (var sample in SampleBooks)
            {
                if (data.Sach.Any(book => book.ISBN == sample.Isbn))
                {
                    continue;
                }

                data.Sach.Add(new Sach
                {
                    Id = NextId(data.Sach.Select(book => book.Id)),
                    TenSach = sample.Title,
                    TacGia = sample.Author,
                    ISBN = sample.Isbn,
                    NhaXuatBan = sample.Publisher,
                    NamXuatBan = sample.Year,
                    SoLuong = sample.Copies,
                    TheLoaiId = categoryIds[sample.Category]
                });
            }

            foreach (var sample in SampleReaders)
            {
                if (data.DocGia.Any(reader => reader.Email.Equals(sample.Email, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                data.DocGia.Add(new DocGia
                {
                    Id = NextId(data.DocGia.Select(reader => reader.Id)),
                    HoTen = sample.Name,
                    Email = sample.Email,
                    SoDienThoai = sample.Phone,
                    DiaChi = sample.Address,
                    NgayDangKy = DateTime.UtcNow.AddDays(-30)
                });
            }

            foreach (var account in data.Accounts.Where(account => account.Roles.Contains(LibraryRoles.ThuThu)))
            {
                if (data.NhanVien.Any(staff => staff.IdentityUserId == account.Id))
                {
                    continue;
                }

                data.NhanVien.Add(new NhanVien
                {
                    Id = NextId(data.NhanVien.Select(staff => staff.Id)),
                    HoTen = account.HoTen,
                    Email = account.Email,
                    SoDienThoai = account.SoDienThoai,
                    IdentityUserId = account.Id
                });
            }

            SeedLoans(data);
            return true;
        });
    }

    private static void SeedLoans(LibraryData data)
    {
        var sampleReaders = data.DocGia
            .Where(reader => reader.Email.EndsWith("@example.test", StringComparison.OrdinalIgnoreCase))
            .OrderBy(reader => reader.Id)
            .ToList();
        var librarian = data.NhanVien.FirstOrDefault(staff =>
            data.Accounts.Any(account => account.Id == staff.IdentityUserId && account.Roles.Contains(LibraryRoles.ThuThu)));
        if (sampleReaders.Count < 3 || librarian is null || data.Sach.Count < 3)
        {
            return;
        }

        var sampleReaderIds = sampleReaders.Select(reader => reader.Id).ToHashSet();
        if (data.PhieuMuon.Any(loan => sampleReaderIds.Contains(loan.DocGiaId)))
        {
            return;
        }

        var books = data.Sach.OrderBy(book => book.Id).Take(3).ToList();
        var firstLoanId = NextId(data.PhieuMuon.Select(loan => loan.Id));
        var loans = new[]
        {
            new PhieuMuon
            {
            Id = firstLoanId,
                DocGiaId = sampleReaders[0].Id,
                NhanVienId = librarian.Id,
                NgayMuon = DateTime.UtcNow.Date.AddDays(-3),
                HanTra = DateTime.UtcNow.Date.AddDays(11),
                TrangThai = "DangMuon"
            },
            new PhieuMuon
            {
                Id = firstLoanId + 1,
                DocGiaId = sampleReaders[1].Id,
                NhanVienId = librarian.Id,
                NgayMuon = DateTime.UtcNow.Date.AddDays(-24),
                HanTra = DateTime.UtcNow.Date.AddDays(-10),
                TrangThai = "QuaHan"
            },
            new PhieuMuon
            {
                Id = firstLoanId + 2,
                DocGiaId = sampleReaders[2].Id,
                NhanVienId = librarian.Id,
                NgayMuon = DateTime.UtcNow.Date.AddDays(-18),
                HanTra = DateTime.UtcNow.Date.AddDays(-4),
                TrangThai = "DaTra"
            }
        };

        foreach (var loan in loans)
        {
            data.PhieuMuon.Add(loan);
        }

        var firstDetailId = NextId(data.ChiTietPhieuMuon.Select(detail => detail.Id));
        data.ChiTietPhieuMuon.AddRange(loans.Select((loan, index) => new ChiTietPhieuMuon
        {
            Id = firstDetailId + index,
            PhieuMuonId = loan.Id,
            SachId = books[index].Id,
            SoLuong = 1
        }));
    }

    private static int NextId(IEnumerable<int> ids) => ids.DefaultIfEmpty(0).Max() + 1;

    private sealed record SampleBook(
        string Isbn,
        string Title,
        string Author,
        string Publisher,
        int Year,
        string Category,
        int Copies);

    private sealed record SampleReader(string Name, string Email, string Phone, string Address);
}