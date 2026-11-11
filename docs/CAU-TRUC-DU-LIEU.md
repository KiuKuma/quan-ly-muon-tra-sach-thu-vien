# Cấu trúc dữ liệu tệp và quản lý Sách, Độc giả – Sprint 1

**Người thực hiện:** Đặng Huy Quang Dũng  
**Cơ sở:** nhánh `develop`, commit `dcc0e3d`, kèm các sửa của nhánh `feature/sprint1-ra-soat-quang-dung`. Mô hình dữ liệu xem [ERD.md](ERD.md).

Dự án lưu dữ liệu bằng tệp text JSON Lines (mỗi dòng là một bản ghi JSON) trong thư mục `database` (cấu hình `LibraryData:DirectoryPath` trong `appsettings.json`), không dùng SQL. Vì vậy "tạo cơ sở dữ liệu" gồm: danh sách tệp, cấu trúc từng bản ghi, ràng buộc, và bộ khởi tạo.

> **Lưu ý khi dùng Git:** từ commit `1530ba4`, thư mục dữ liệu mặc định là `database` và **các tệp `database/*.txt` được theo dõi trong Git** (`.gitignore` chỉ loại `database/*.tmp` và `database/*.bak`; trước đó dữ liệu nằm ở `App_Data` và bị loại). Hệ quả: (1) mọi người clone về đều có sẵn cùng bộ dữ liệu mẫu; (2) khi chạy ứng dụng, các tệp này bị ghi đè nên `git status` sẽ báo đã sửa. **Đừng `git add .` hoặc commit các tệp trong `database/`** nếu không có chủ ý. Muốn trả về dữ liệu gốc: tắt ứng dụng rồi chạy `git restore database`.
>
> **Lưu ý bảo mật:** `database/users.txt` chứa hash mật khẩu của tài khoản mặc định và `profiles.txt`, `readers.txt` chứa họ tên, email, số điện thoại. Chỉ nên để dữ liệu giả trong repo công khai.

## 1. Quy trình khởi tạo (`Program.cs` gọi `LibraryDataInitializer`)

| Bước | Thành phần | Việc làm |
|---|---|---|
| 1 | `LibraryFileStore.EnsureCreatedAsync()` | Tạo thư mục dữ liệu (`database`) và đủ 10 tệp. Nếu thấy `library.json` cũ thì tách sang các tệp mới |
| 2 | `LibraryAccountService.EnsureDefaultAccountsAsync()` | Bảo đảm có bốn tài khoản ban đầu (`admin`, `nhanvien1`, `nhanvien2`, `nhanvien3`) |
| 3 | `LibrarySampleDataSeeder.SeedAsync()` | Bổ sung thể loại, sách, độc giả, nhân viên và phiếu mượn mẫu còn thiếu, không ghi đè dữ liệu đã có |

Mọi lần ghi đều đi qua `LibraryFileStore.UpdateAsync`: có khóa `SemaphoreSlim`, ghi ra tệp `.tmp` rồi đổi tên. Chỉ chạy **một** tiến trình ứng dụng tại một thời điểm.

## 2. Cấu trúc từng tệp

| Tệp | Trường | Khóa và ràng buộc |
|---|---|---|
| `users.txt` | `Id` (GUID dạng chuỗi), `Username`, `PasswordHash`, `Roles[]`, `FailedLoginAttempts`, `LockoutEndUtc` | PK `Id`; `Username` duy nhất |
| `profiles.txt` | `AccountId`, `HoTen`, `Email`, `SoDienThoai`, `DiaChi` | PK và FK `AccountId` → `users`; `Email` duy nhất |
| `readers.txt` | `Id`, `HoTen`, `Email`, `SoDienThoai`, `DiaChi`, `NgayDangKy`, `IdentityUserId` | PK `Id`; `Email` duy nhất; FK `IdentityUserId` → `users` (có thể rỗng) |
| `staff.txt` | `Id`, `HoTen`, `Email`, `SoDienThoai`, `IdentityUserId` | PK `Id`; FK `IdentityUserId` → `users` |
| `categories.txt` | `Id`, `TenTheLoai`, `MoTa` | PK `Id`; `TenTheLoai` duy nhất |
| `books.txt` | `Id`, `TenSach`, `TacGia`, `NhaXuatBan`, `NamXuatBan`, `SoLuong`, `TheLoaiId` | PK `Id`; FK `TheLoaiId` → `categories` (có thể rỗng: sách chưa phân loại) |
| `loans.txt` | `Id`, `DocGiaId`, `NhanVienId`, `TenTaiKhoanNhanVien`, `NgayMuon`, `HanTra`, `TrangThai` | PK `Id`; FK → `readers`, `staff` |
| `loan-details.txt` | `Id`, `PhieuMuonId`, `SachId`, `SoLuong` | PK `Id`; FK → `loans`, `books` |
| `returns.txt` | `Id`, `PhieuMuonId`, `NhanVienId`, `TenTaiKhoanNhanVien`, `NgayTra`, `TienPhatDaThanhToan`, `NgayThanhToanTienPhat`, `TenTaiKhoanThuTienPhat` | PK `Id`; FK → `loans`, `staff`; ghi nhận thanh toán tiền phạt theo phiếu trả |
| `return-details.txt` | `Id`, `PhieuTraId`, `ChiTietPhieuMuonId`, `SoLuongTra`, `TienPhat`, `TinhTrang`, `GhiChu` | PK `Id`; FK → `returns`, `loan-details`; tiền phạt chốt theo từng dòng sách |

Mã sách (`Id`) kiểu số được cấp tự động bằng `Max(Id) + 1` trong cùng lần ghi; người dùng không nhập mã. Biểu mẫu thêm và sửa sách nhập tên sách, tác giả, thể loại (chọn từ danh sách, không bắt buộc), nhà xuất bản, năm xuất bản và số lượng.

## 3. Ràng buộc toàn vẹn và nơi đang kiểm tra

Tệp text không tự kiểm tra khóa ngoại hay tính duy nhất, nên các quy tắc dưới đây do **controller** bảo đảm khi thêm, sửa, xóa.

| Quy tắc | Nơi kiểm tra |
|---|---|
| `DocGia.Email` không trùng với độc giả khác và hồ sơ tài khoản | `DocGiaController` |
| `TheLoai.TenTheLoai` không trùng (không phân biệt hoa thường) | `TheLoaiController` |
| Mượn: độc giả và nhân viên hợp lệ, hạn trả không trước ngày mượn, đủ số lượng mọi sách | `MuonTraController.Borrow` |
| Mượn nhiều đầu sách trên một phiếu, trừ tồn kho theo từng sách | `MuonTraController.Borrow` |
| Trả: số lượng không vượt số còn mượn; mỗi lần trả tạo phiếu và chi tiết trả | `MuonTraController.Return` |
| Sách trả tốt được cộng vào tồn kho; sách hư hỏng được lưu tình trạng và không cộng vào số lượng sẵn có | `MuonTraController.Return` |
| Phạt trễ lũy tiến theo từng cuốn đã trả: 1–7 ngày 1.000đ/ngày; 8–30 ngày 2.000đ/ngày; từ ngày 31 là 5.000đ/ngày | `MuonTraController.Return` |
| Không tạo phiếu mượn khi độc giả còn tiền phạt chưa thanh toán; xác nhận thu tiền gắn với phiếu trả | `MuonTraController.Borrow`, `MuonTraController.PayFine` |
| Không tạo phiếu mượn khi độc giả còn bất kỳ sách nào chưa trả, kể cả phiếu còn trong hạn | `MuonTraController.Borrow` |
| `Sach.TheLoaiId` (nếu có) phải là thể loại có thật | `SachController` (Create, Edit) |
| `Sach.NamXuatBan` không lớn hơn năm hiện tại | `SachController` (Create, Edit) |
| Không xóa Sách đã có trong phiếu mượn | `SachController.DeleteConfirmed` |
| Không xóa Độc giả đã có phiếu mượn | `DocGiaController.DeleteConfirmed` |
| Không xóa Thể loại còn sách | `TheLoaiController.DeleteConfirmed` |
| Tra cứu phiếu theo mã độc giả và trạng thái; phân trang danh sách mượn/trả | `MuonTraController.Index` |

Các quy tắc chưa có nơi kiểm tra: `Sach.SoLuong` không âm sau khi Quản lý sửa tay; kiểm tra khóa ngoại khi dữ liệu bị sửa tay trong tệp.

## 4. Quy trình mượn, trả và tra cứu phiếu

- `/MuonTra/Borrow`: chọn mã độc giả, tên được tự hiển thị; nhập ngày mượn/hẹn trả; chọn một hoặc nhiều sách. Mã, tên, tác giả, nhà xuất bản và số lượng còn được hiển thị khi chọn sách. Mã phiếu tự cấp.
- `/MuonTra/Return`: lọc phiếu còn sách cần trả theo mã thẻ; chọn phiếu, nhập số lượng nhận cho từng đầu sách, tình trạng tốt/hư hỏng và ghi chú. Có thể lập nhiều phiếu trả cho một phiếu mượn (trả từng phần).
- `/MuonTra`: tra cứu theo mã thẻ và trạng thái, xem danh sách phân trang; chọn mã phiếu để mở chi tiết sách, số lượng trả, lịch sử phiếu trả, tiền phạt và thanh toán.
- Khi tạo phiếu, hệ thống chặn nếu còn bất kỳ sách mượn nào chưa trả (kể cả còn trong hạn) hoặc còn nợ phạt.
- Sách trả tốt được cộng vào số lượng sẵn có. Sách hư hỏng được lưu trong chi tiết phiếu trả nhưng không tăng số lượng có thể cho mượn.
- Tiền phạt lũy tiến của sách còn đang mượn được tính đến ngày hiện tại để hiển thị và cộng vào nợ độc giả; khi trả một phần, phạt được chốt tại `return-details.txt` cho số sách trả, còn phần chưa trả tiếp tục phát sinh. Không tính hai lần tiền phạt cho sách đã trả. Khoản đã chốt không đổi nếu biểu phí được điều chỉnh; thanh toán được ghi nhận trên phiếu trả, khoản chưa thu chặn lập phiếu mượn mới.
- Các thao tác mượn/trả/thu tiền chạy trong `LibraryFileStore.UpdateAsync`, dùng khóa `SemaphoreSlim` chung để kiểm tra và ghi dữ liệu tuần tự trong một tiến trình. Dự án yêu cầu chỉ chạy một tiến trình ứng dụng tại một thời điểm.

## 5. Quản lý Sách và Độc giả
Hai chức năng này đã có trong `develop` (`SachController`, `DocGiaController`, các view tương ứng). Thêm, sửa, xóa chỉ dành cho Quản lý; Nhân viên chỉ xem và tìm (khớp `README.md`). Bảng dưới là tài liệu hóa theo đúng hành vi của code.

| Đường dẫn | Chức năng | Vai trò được dùng |
|---|---|---|
| `/Sach`, `/DocGia` | Danh sách, tìm theo tên hoặc tác giả (Sách), tên hoặc email (Độc giả) | Nhân viên, Quản lý |
| `/Sach/Details/{id}`, `/DocGia/Details/{id}` | Xem chi tiết | Nhân viên, Quản lý |
| `/Sach/Create`, `/DocGia/Create` | Thêm | Quản lý |
| `/Sach/Edit/{id}`, `/DocGia/Edit/{id}` | Sửa | Quản lý |
| `/Sach/Delete/{id}`, `/DocGia/Delete/{id}` | Xóa (có trang xác nhận) | Quản lý |

Ràng buộc đang được controller kiểm tra:

- Thêm và sửa Sách: bắt buộc tên sách, tác giả; thể loại không bắt buộc nhưng nếu chọn phải tồn tại; năm xuất bản từ 1450 đến năm hiện tại; số lượng 0 đến 100000. Mã sách được tự cấp. Tên sách, tác giả, nhà xuất bản được cắt khoảng trắng đầu và cuối.
- Thêm và sửa Độc giả: bắt buộc họ tên, email hợp lệ và không trùng (kể cả với hồ sơ tài khoản). Khi sửa độc giả có tài khoản liên kết thì hồ sơ được cập nhật theo.
- Xóa Sách đã có trong phiếu mượn: bị chặn. Xóa Độc giả đã có phiếu mượn: bị chặn.
- Mọi thao tác POST đều có `[ValidateAntiForgeryToken]`.

## 6. Hạn chế của lưu trữ bằng tệp

- Chỉ chạy **một** tiến trình ứng dụng tại một thời điểm (khóa `SemaphoreSlim` chỉ có tác dụng trong một tiến trình).
- Mỗi lần ghi đều đọc và ghi lại cả 10 tệp, phù hợp với quy mô thư viện nhỏ của đề tài nhưng sẽ chậm khi dữ liệu lớn.
- Việc đổi tên 10 tệp tạm thành tệp chính không phải một thao tác nguyên tử duy nhất. Nếu máy tắt đúng lúc đó, dữ liệu có thể lệch giữa các tệp.

## 7. Việc để sprint sau

- Tìm kiếm đa tiêu chí (thể loại, năm xuất bản): Sprint 3.
- Tách `TacGia`, gộp `NguoiDung`, thêm `PhieuTra`: theo [ERD.md](ERD.md), chỉ làm khi cả nhóm thống nhất.
