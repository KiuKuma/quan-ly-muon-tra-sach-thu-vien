# ERD chuẩn 3NF – Sprint 0

**Người thực hiện:** Đặng Huy Quang Dũng  
**Cơ sở:** nhánh `develop`, commit `dcc0e3d` (mã nguồn nền của Đỗ Phương Anh)  
**Lưu trữ:** tệp JSON Lines trong thư mục dữ liệu (không dùng SQL); `LibraryFileStore` tự tạo thêm tệp mới khi nâng cấp dữ liệu.

## 1. Khảo sát dữ liệu

Đối chiếu `Models/`, `Data/LibraryData.cs`, `Data/LibraryFileStore.cs` và các controller để xác định các tập dữ liệu hiện có.

| Tệp trong `database` | Lớp C# | Ý nghĩa |
|---|---|---|
| `users.txt` | `LibraryAccount` | Tài khoản đăng nhập, hash mật khẩu, vai trò |
| `profiles.txt` | `LibraryUserProfile` | Hồ sơ cá nhân của tài khoản |
| `readers.txt` | `DocGia` | Độc giả |
| `staff.txt` | `NhanVien` | Nhân viên thư viện |
| `categories.txt` | `TheLoai` | Thể loại sách |
| `books.txt` | `Sach` | Đầu sách và số lượng còn |
| `loans.txt` | `PhieuMuon` | Phiếu mượn |
| `loan-details.txt` | `ChiTietPhieuMuon` | Sách và số lượng trên từng phiếu |
| `returns.txt` | `PhieuTra` | Phiếu trả sách |
| `return-details.txt` | `ChiTietPhieuTra` | Số lượng, tình trạng và ghi chú từng đầu sách trả |
| `fine-payments.txt` | `FinePayment` | Phiếu thu tiền phạt, người thu và thời điểm thu |
| `inventory-adjustments.txt` | `BookInventoryAdjustment` | Nhật ký sách hư hỏng/mất bị loại khỏi tồn kho |
| `settings.txt` | `LibrarySettings` | Một bản ghi tham số nghiệp vụ do Quản lý cấu hình |

## 2. ERD hiện trạng (theo đúng tên trong code)

```mermaid
erDiagram
    TaiKhoan ||--o| HoSo : "AccountId"
    TaiKhoan |o--o| DocGia : "IdentityUserId"
    TaiKhoan |o--o| NhanVien : "IdentityUserId"
    TheLoai |o--o{ Sach : "TheLoaiId (có thể rỗng)"
    DocGia ||--o{ PhieuMuon : "DocGiaId"
    NhanVien ||--o{ PhieuMuon : "NhanVienId"
    PhieuMuon ||--|{ ChiTietPhieuMuon : "PhieuMuonId"
    Sach ||--o{ ChiTietPhieuMuon : "SachId"
    PhieuMuon ||--o{ PhieuTra : "PhieuMuonId"
    PhieuTra ||--|{ ChiTietPhieuTra : "PhieuTraId"
    ChiTietPhieuMuon ||--o{ ChiTietPhieuTra : "ChiTietPhieuMuonId"
    PhieuTra ||--o{ FinePayment : "ReturnId"
    Sach ||--o{ BookInventoryAdjustment : "BookId"

    TaiKhoan {
        string Id PK
        string Username
        string PasswordHash
        string_list Roles
        bool IsActive
        int FailedLoginAttempts
        datetime LockoutEndUtc
    }
    HoSo {
        string AccountId PK
        string HoTen
        string Email
        string SoDienThoai
        string DiaChi
    }
    DocGia {
        int Id PK
        string HoTen
        string Email
        string SoDienThoai
        string DiaChi
        datetime NgayDangKy
        datetime NgaySinh
        datetime NgayHetHan
        string IdentityUserId FK
    }
    NhanVien {
        int Id PK
        string HoTen
        string Email
        string SoDienThoai
        string IdentityUserId FK
    }
    TheLoai {
        int Id PK
        string TenTheLoai
        string MoTa
    }
    Sach {
        int Id PK
        string TenSach
        string TacGia
        string NhaXuatBan
        int NamXuatBan
        int SoLuong
        int TheLoaiId FK
    }
    PhieuMuon {
        int Id PK
        int DocGiaId FK
        int NhanVienId FK
        string TenTaiKhoanNhanVien
        datetime NgayMuon
        datetime HanTra
        decimal MucPhatMoiNgay
        string TrangThai
    }
    ChiTietPhieuMuon {
        int Id PK
        int PhieuMuonId FK
        int SachId FK
        int SoLuong
    }
    PhieuTra {
        int Id PK
        int PhieuMuonId FK
        int NhanVienId FK
        string TenTaiKhoanNhanVien
        datetime NgayTra
        decimal TienPhatDaThanhToan
        datetime NgayThanhToanTienPhat
        string TenTaiKhoanThuTienPhat
    }
    ChiTietPhieuTra {
        int Id PK
        int PhieuTraId FK
        int ChiTietPhieuMuonId FK
        int SoLuongTra
        decimal TienPhat
        string TinhTrang
        string GhiChu
    }
    FinePayment {
        int Id PK
        int ReturnId FK
        int LoanId FK
        int ReaderId FK
        decimal Amount
        datetime PaidAt
        string StaffUsername
    }
    BookInventoryAdjustment {
        int Id PK
        int BookId FK
        int Quantity
        string Reason
        string Note
        string StaffUsername
        datetime AdjustedAt
    }
    LibrarySettings {
        int LoanTermDays
        int MaxBooksPerReader
        decimal DailyLateFine
        decimal DamagedBookFine
        decimal LostBookFine
        int ReaderMinimumAge
        int ReaderMaximumAge
        int ReaderCardValidityMonths
    }
```

## 3. Điểm chưa đạt 3NF trong thiết kế hiện trạng

| # | Vị trí | Vấn đề | Dạng chuẩn bị vi phạm |
|---|---|---|---|
| 1 | `Sach.TacGia` | Một ô chứa nhiều tác giả, ví dụ `"David Thomas, Andrew Hunt"` | 1NF (thuộc tính đa trị) |
| 2 | `HoSo`, `DocGia`, `NhanVien` | `HoTen`, `Email`, `SoDienThoai` lặp ở cả ba nơi khi một người có tài khoản. `DocGiaController.Edit` phải tự đồng bộ sang hồ sơ | 3NF (dữ liệu dư, phụ thuộc bắc cầu qua `IdentityUserId`) |
| 3 | `PhieuMuon.TenTaiKhoanNhanVien` | Suy ra được từ `NhanVienId` rồi `IdentityUserId` rồi `Username` | 3NF (phụ thuộc bắc cầu) |
| 4 | `PhieuMuon.TrangThai` | `QuaHan` suy ra từ `HanTra` và ngày hiện tại. `MuonTraController` đã tự tính khi hiển thị, nên giá trị lưu dễ lệch | Thuộc tính suy diễn |
| 5 | `PhieuTra` còn tổng thanh toán trong khi `FinePayment` lưu từng lần thu | Hai trường tổng thanh toán được giữ để tương thích dữ liệu cũ; dữ liệu mới dùng các bản ghi `FinePayment` làm sổ thu chi tiết | Dư thừa tương thích |
| 6 | `Roles` dùng `Quản lý` và `Nhân viên` | Tài liệu dùng tên nghiệp vụ `Quản trị viên` và `Thủ thư`; hai cặp tên được ánh xạ tương ứng. Chưa có tài khoản độc giả | Khác tên, không khác quyền |

## 4. ERD chuẩn 3NF (mục tiêu)

```mermaid
erDiagram
    VaiTro ||--o{ TaiKhoan : "gán cho"
    TaiKhoan |o--o| NguoiDung : "có hồ sơ"
    NguoiDung ||--o| DocGia : "là"
    NguoiDung ||--o| NhanVien : "là"
    TheLoai ||--o{ Sach : "phân loại"
    Sach ||--o{ SachTacGia : "có"
    TacGia ||--o{ SachTacGia : "viết"
    DocGia ||--o{ PhieuMuon : "mượn"
    NhanVien ||--o{ PhieuMuon : "lập"
    PhieuMuon ||--|{ ChiTietPhieuMuon : "gồm"
    Sach ||--o{ ChiTietPhieuMuon : "được mượn"
    PhieuMuon ||--o{ PhieuTra : "được trả bằng"
    NhanVien ||--o{ PhieuTra : "nhận"
    PhieuTra ||--|{ ChiTietPhieuTra : "gồm"
    ChiTietPhieuMuon ||--o{ ChiTietPhieuTra : "được trả"

    VaiTro {
        int Id PK
        string TenVaiTro UK
    }
    TaiKhoan {
        string Id PK
        string Username UK
        string PasswordHash
        int VaiTroId FK
        int FailedLoginAttempts
        datetime LockoutEndUtc
    }
    NguoiDung {
        int Id PK
        string HoTen
        string Email UK
        string SoDienThoai
        string DiaChi
        string TaiKhoanId FK "UK, có thể null"
    }
    DocGia {
        int Id PK
        int NguoiDungId FK "UK"
        datetime NgayDangKy
    }
    NhanVien {
        int Id PK
        int NguoiDungId FK "UK"
    }
    TheLoai {
        int Id PK
        string TenTheLoai UK
        string MoTa
    }
    TacGia {
        int Id PK
        string TenTacGia
    }
    Sach {
        int Id PK
        string TenSach
        string NhaXuatBan
        int NamXuatBan
        int SoLuong
        int TheLoaiId FK
    }
    SachTacGia {
        int SachId PK, FK
        int TacGiaId PK, FK
    }
    PhieuMuon {
        int Id PK
        int DocGiaId FK
        int NhanVienId FK
        datetime NgayMuon
        datetime HanTra
    }
    ChiTietPhieuMuon {
        int Id PK
        int PhieuMuonId FK
        int SachId FK
        int SoLuong
    }
    PhieuTra {
        int Id PK
        int PhieuMuonId FK
        int NhanVienId FK
        datetime NgayTra
        decimal TienPhatDaThanhToan
        datetime NgayThanhToanTienPhat
        string TenTaiKhoanThuTienPhat
    }
    ChiTietPhieuTra {
        int Id PK
        int PhieuTraId FK
        int ChiTietPhieuMuonId FK
        int SoLuongTra
        decimal TienPhat
        string TinhTrang
        string GhiChu
    }
```

### Cách xử lý từng điểm ở mục 3

| Điểm | Cách xử lý trong ERD 3NF |
|---|---|
| 1 | Tách `TacGia` và bảng nối `SachTacGia` (nhiều–nhiều) |
| 2 | Thêm `NguoiDung` giữ họ tên, email, SĐT, địa chỉ. `DocGia` và `NhanVien` chỉ giữ thông tin riêng và trỏ về `NguoiDung`. Một người vừa là độc giả vừa là nhân viên chỉ có một hồ sơ |
| 3 | Bỏ `TenTaiKhoanNhanVien` |
| 4 | Bỏ `TrangThai`. Trạng thái tính từ `HanTra`, ngày hiện tại và tổng `SoLuongTra` so với tổng `SoLuong` |
| 5 | Thêm `PhieuTra` và `ChiTietPhieuTra`, cho phép trả từng phần. `TienPhat` được lưu vì là số tiền đã chốt tại thời điểm trả; số đã thu, thời điểm và tài khoản thu được lưu ở `PhieuTra` |
| 6 | Tách `VaiTro` thành bảng riêng để đổi hoặc mở rộng vai trò mà không sửa cấu trúc |

### Kiểm tra 3NF

- **1NF:** mọi thuộc tính đơn trị sau khi tách `TacGia`.
- **2NF:** mọi bảng có khóa chính một cột. `SachTacGia` có khóa kép nhưng không có thuộc tính ngoài khóa.
- **3NF:** không còn thuộc tính phụ thuộc bắc cầu vào khóa chính.

### Ràng buộc nghiệp vụ

- Duy nhất (không phân biệt hoa thường): `TaiKhoan.Username`, `NguoiDung.Email`, `TheLoai.TenTheLoai`.
- `Sach.SoLuong >= 0`; `Sach.NamXuatBan` trong khoảng 1450 đến năm hiện tại (`[Range]` cho phép đến 2200, `SachController` chặn năm lớn hơn năm hiện tại).
- `PhieuMuon.HanTra >= NgayMuon`; `ChiTietPhieuMuon.SoLuong > 0`.
- `ChiTietPhieuTra.SoLuongTra` không vượt số lượng mượn chưa trả của dòng tương ứng.
- Không xóa `Sach`, `DocGia`, `NhanVien` đang được phiếu mượn tham chiếu; không xóa `TheLoai` còn sách. Các điều kiện này code hiện tại đã chặn.

## 5. Ánh xạ ERD 3NF sang tệp hiện tại

ERD ở mục 4 là mô hình mục tiêu. Code hiện tại chưa cần đổi ngay. Bảng sau cho biết từng thực thể mới nằm ở đâu.

| ERD 3NF | Tệp hiện tại | Ghi chú |
|---|---|---|
| `TaiKhoan`, `VaiTro` | `users.txt` | `Roles` hiện là danh sách một phần tử |
| `NguoiDung` | `profiles.txt`, `readers.txt`, `staff.txt` | Gộp phần thông tin cá nhân dùng chung |
| `DocGia`, `NhanVien` | `readers.txt`, `staff.txt` | Giữ `Id`, bỏ trường cá nhân lặp |
| `TheLoai` | `categories.txt` | Giữ nguyên |
| `Sach`, `TacGia`, `SachTacGia` | `books.txt` | Tách chuỗi `TacGia` |
| `PhieuMuon`, `ChiTietPhieuMuon` | `loans.txt`, `loan-details.txt` | Bỏ `TrangThai`, `TenTaiKhoanNhanVien` |
| `PhieuTra`, `ChiTietPhieuTra`, `FinePayment` | `returns.txt`, `return-details.txt`, `fine-payments.txt` | Lưu trả từng phần, tình trạng, tiền phạt đã chốt và từng lần thu |
| `LibrarySettings`, `BookInventoryAdjustment` | `settings.txt`, `inventory-adjustments.txt` | Quy định vận hành và nhật ký điều chỉnh tồn |

## 6. Câu hỏi cần chốt với nhóm

1. **`Sach.SoLuong`:** hiện là số còn lại và bị trừ khi lập phiếu. ERD này giữ nguyên ý nghĩa đó để khớp `MuonTraController`. Nếu muốn tách thành tổng số và số còn lại thì cần thống nhất với Phương Anh (Sprint 2).
2. **Mức phạt:** đã thống nhất thành mức cố định có thể cấu hình theo ngày; mặc định 2.000đ/cuốn/ngày. Mức trên mỗi phiếu được lưu tại thời điểm mượn.
3. **Tên vai trò:** tài liệu nghiệp vụ dùng Thủ thư/Quản trị viên; code dùng Nhân viên/Quản lý. Chưa có đăng nhập Độc giả.
