# Rà soát code quản lý Sách, Độc giả, Thể loại (đối chiếu nhiệm vụ Quang Dũng)

**Phạm vi:** `SachController`, `DocGiaController`, `TheLoaiController`, `LibraryFileStore`, các view liên quan và phần giao diện bảng dữ liệu.  
**Cơ sở:** nhánh `develop`, commit `dcc0e3d`. Đã build và chạy thử trên .NET 10 (đăng nhập bằng tài khoản Quản lý và Nhân viên, thử thêm sách hợp lệ, năm tương lai, thể loại không tồn tại, truy cập trái quyền).

## 1. Kết luận

CRUD Sách, Độc giả, Thể loại chạy đúng thiết kế: có chống giả mạo yêu cầu (`ValidateAntiForgeryToken`), kiểm tra trùng lặp trong cùng lần khóa ghi tệp, chặn xóa dữ liệu đang được tham chiếu. Lần rà soát này phát hiện thêm vài chỗ lệch giữa code, `README.md` và tài liệu; các chỗ ở mục 3 đã được sửa trong nhánh `feature/sprint1-ra-soat-quang-dung`.

## 2. Đã đúng

| Hạng mục | Cách code xử lý |
|---|---|
| Ghi dữ liệu an toàn | Mọi thao tác đi qua `LibraryFileStore.UpdateAsync` với khóa `SemaphoreSlim`; ghi tệp `.tmp` rồi đổi tên. Kiểm tra trùng và ghi nằm trong cùng một lần khóa |
| Đọc dữ liệu | `ReadAsync` nạp lại từ đĩa mỗi lần, nên việc gán `Sach.TheLoai` trong `Index` không làm bẩn dữ liệu dùng chung |
| Chống overposting | Dùng `[Bind(...)]` liệt kê trường được nhận; `Id` và `NgayDangKy` do server cấp |
| Toàn vẹn tham chiếu | Xóa Sách có trong phiếu mượn, xóa Độc giả có phiếu, xóa Thể loại còn sách đều bị chặn kèm thông báo |
| Trùng lặp | Email (Độc giả, kể cả với hồ sơ tài khoản), tên thể loại (không phân biệt hoa thường) |
| Cấp mã | `Max(Id) + 1` trong lần khóa ghi |

## 3. Lỗi đã phát hiện và đã sửa

| # | Mức | Vị trí | Vấn đề | Cách sửa |
|---|---|---|---|---|
| 1 | Trung bình | `SachController` (Create, Edit, Delete) và view Sách | Commit `f6b0b51` cho Nhân viên thêm, sửa, xóa sách, trái với `README.md`, PB-11, BR-06 và với `TheLoaiController`, `DocGiaController` (chỉ Quản lý) | Đưa về chỉ Quản lý; ẩn nút Thêm, Sửa, Xóa với Nhân viên. Đã thử: Nhân viên bị chuyển tới trang từ chối truy cập |
| 2 | Trung bình | `SachController`, `Views/Sach/Form` | Biểu mẫu không còn ô Thể loại và `Bind` bỏ `TheLoaiId`, nên sách mới luôn không có thể loại, Thể loại thành vô nghĩa và Sprint 3 không lọc theo thể loại được | Thêm lại ô chọn Thể loại (không bắt buộc, "Chưa phân loại"); kiểm tra thể loại tồn tại cả ở bước kiểm tra và trong lần khóa ghi |
| 3 | Thấp | `SachController` | Năm xuất bản nhập được đến 2200 | Chặn năm lớn hơn năm hiện tại |
| 4 | Thấp | `TheLoaiController.Create` | Không cắt khoảng trắng `MoTa` (`Edit` có cắt) | Cắt khoảng trắng và đổi chuỗi rỗng thành `null` ở cả Create và Edit |
| 5 | Thấp | `DocGiaController.DeleteConfirmed`, `Views/DocGia/Delete` | Thông báo và trang xác nhận nói xóa cả tài khoản liên kết nhưng code không làm vậy | Sửa lời thông báo cho đúng hành vi |
| 6 | Thấp | `site.css` | Tiêu đề cột bảng (Mã sách, Năm xuất bản, Số lượng) xuống dòng | `white-space: nowrap` cho tiêu đề cột |
| 7 | Thấp | `site.css`, `Views/Home/Index` | Tiêu đề "Tổng quan thư viện" dùng khoảng cách chữ âm làm các chữ dính nhau; chấm tròn phát sáng trang trí | Bỏ khoảng cách chữ âm, bỏ chấm, giảm cỡ và độ đậm |

Mục 6 và 7 thuộc giao diện (phân công của Huy Thành); sửa nhỏ, nằm ở commit riêng để dễ xem lại.

## 4. Điểm còn mở

| # | Mức | Vị trí | Vấn đề | Đề xuất |
|---|---|---|---|---|
| 1 | Trung bình | `SachController.Edit` | `SoLuong` là số còn lại trong kho (bị trừ khi mượn, cộng khi trả) nhưng Quản lý sửa tay được. Sửa lúc sách đang cho mượn làm số liệu không còn phản ánh tổng số bản | Cần nhóm thống nhất (xem `ERD.md`, câu hỏi 1) |
| 2 | Thấp | Tìm kiếm (Sách, Độc giả, Thể loại) | `Contains(..., OrdinalIgnoreCase)` không bỏ dấu tiếng Việt: gõ "lap trinh" không ra "Lập trình" | Làm cùng tìm kiếm đa tiêu chí ở Sprint 3 |
| 3 | Thấp | `DocGia`, `NhanVien` | Họ tên, email, SĐT lặp với hồ sơ tài khoản (xem `ERD.md`, mục 3) | Chỉ xử lý nếu nhóm đồng ý chuyển sang ERD 3NF |
| 4 | Thấp | `HomeController.Index` | Có `[Authorize]` nên nhánh "Đăng nhập hệ thống" trong `Views/Home/Index` không bao giờ hiển thị | Bỏ nhánh thừa hoặc bỏ `[Authorize]`, tùy nhóm (thuộc Phương Anh) |

## 5. Phần liên quan nhưng thuộc nhiệm vụ khác

| Phần | Hiện trạng | Thuộc |
|---|---|---|
| Kiểm tra điều kiện mượn (chặn độc giả đang quá hạn hoặc nợ phạt) | **Chưa có.** `MuonTraController.Create` chỉ kiểm tra có độc giả, có sách, đủ số lượng và hạn trả không ở quá khứ | Sprint 2, Quang Dũng |
| Lịch sử mượn trả theo từng độc giả | **Chưa có** trang riêng. `MuonTra/Index` chỉ liệt kê toàn bộ phiếu | Sprint 2, Quang Dũng |
| Ghi ngày trả, tiền phạt trễ hạn | **Chưa có.** `Return` chỉ đặt `TrangThai = "DaTra"` và cộng lại số lượng | Sprint 2, Phương Anh |
| Báo cáo thống kê | Mới có các con số tổng (`BaoCaoController`). Chưa có lượt mượn theo sách hoặc theo thời gian, danh sách sách quá hạn, biểu đồ | Sprint 3, Quang Dũng |
| Tìm kiếm đa tiêu chí cho độc giả | Chưa có; hiện chỉ tìm theo tên và tác giả, và chưa có vai trò Độc giả | Sprint 3, Quang Dũng |
