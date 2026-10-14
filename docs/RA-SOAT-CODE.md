# Rà soát code quản lý Sách, Độc giả, Thể loại (đối chiếu nhiệm vụ Quang Dũng)

**Phạm vi:** `SachController`, `DocGiaController`, `TheLoaiController`, `LibraryFileStore`, và các phần liên quan trong `MuonTraController`, `BaoCaoController`.  
**Cơ sở:** nhánh `develop`, commit `1530ba4`. Rà soát bằng cách đọc mã, chưa chạy thử ứng dụng.

## 1. Kết luận

CRUD Sách, Độc giả, Thể loại **hoạt động đúng theo thiết kế**: phân quyền đúng (Nhân viên chỉ xem, Quản lý mới sửa), có chống giả mạo yêu cầu (`ValidateAntiForgeryToken`), có kiểm tra trùng lặp trong cùng lần khóa ghi tệp, chặn xóa dữ liệu đang được tham chiếu. Chỉ có một số lỗi nhỏ và điểm nên cải thiện, nêu ở mục 3.

## 2. Đã đúng

| Hạng mục | Cách code xử lý |
|---|---|
| Ghi dữ liệu an toàn | Mọi thao tác đi qua `LibraryFileStore.UpdateAsync` với khóa `SemaphoreSlim`; ghi tệp `.tmp` rồi đổi tên. Kiểm tra trùng và ghi nằm trong cùng một lần khóa nên không lọt race condition |
| Đọc dữ liệu | `ReadAsync` nạp lại từ đĩa mỗi lần, nên việc gán `Sach.TheLoai` trong `Index` không làm bẩn dữ liệu dùng chung |
| Chống overposting | Dùng `[Bind(...)]` liệt kê trường được nhận; `Id` và `NgayDangKy` do server cấp |
| Phân quyền | Lớp: Nhân viên + Quản lý. Quản lý sách: Nhân viên + Quản lý |
| Toàn vẹn tham chiếu | Xóa Sách có trong phiếu mượn, xóa Độc giả có phiếu, xóa Thể loại còn sách đều bị chặn kèm thông báo |
| Trùng lặp | Email (Độc giả, kể cả với hồ sơ tài khoản), tên thể loại (không phân biệt hoa thường) |
| Cấp mã | `Max(Id) + 1` trong lần khóa ghi |

## 3. Lỗi và điểm cần cải thiện

| # | Mức | Vị trí | Vấn đề | Đề xuất |
|---|---|---|---|---|
| 1 | Thấp | `SachController.Create` | Không cắt khoảng trắng đầu/cuối của `TenSach`, `TacGia`, `NhaXuatBan` (hàm `Edit` có cắt). Dữ liệu thêm mới có thể lệch thứ tự sắp xếp và khó tìm | Thêm `Trim()` giống hàm `Edit` |
| 2 | Thấp | `TheLoaiController.Create` | Không cắt `MoTa` (hàm `Edit` có cắt) | Thêm `Trim()` giống hàm `Edit` |
| 3 | Thấp | `DocGiaController.DeleteConfirmed` | Thông báo nói xóa cả tài khoản liên kết nhưng code không làm vậy | Sửa thông báo thành "Đã xóa độc giả." |
| 4 | Trung bình | `SachController.Edit` | `SoLuong` là số còn lại trong kho (bị trừ khi mượn, cộng khi trả) nhưng Quản lý sửa tay được. Sửa lúc sách đang cho mượn sẽ làm số liệu không còn phản ánh tổng số bản | Cần nhóm thống nhất (xem `ERD.md`, câu hỏi 1) |
| 5 | Thấp | Tìm kiếm (Sách, Độc giả, Thể loại) | `Contains(..., OrdinalIgnoreCase)` không bỏ dấu tiếng Việt: gõ "lap trinh" không ra "Lập trình" | Làm cùng tìm kiếm đa tiêu chí ở Sprint 3 |
| 7 | Thấp | `Sach.NamXuatBan` | Cho phép đến năm 2200, tức nhập được năm tương lai xa | Đề xuất giới hạn theo năm hiện tại + 1 |
| 8 | Thấp | `DocGia`, `NhanVien` | Họ tên, email, SĐT lặp với hồ sơ tài khoản (xem `ERD.md`, mục 3) | Chỉ xử lý nếu nhóm đồng ý chuyển sang ERD 3NF |

## 4. Phần liên quan nhưng thuộc nhiệm vụ khác

| Phần | Hiện trạng | Thuộc |
|---|---|---|
| Kiểm tra điều kiện mượn (chặn độc giả đang quá hạn hoặc nợ phạt) | **Chưa có.** `MuonTraController.Create` chỉ kiểm tra có độc giả, có sách, đủ số lượng và hạn trả không ở quá khứ | Sprint 2, Quang Dũng |
| Lịch sử mượn trả theo từng độc giả | **Chưa có** trang riêng. `MuonTra/Index` chỉ liệt kê toàn bộ phiếu | Sprint 2, Quang Dũng |
| Ghi ngày trả, tiền phạt trễ hạn | **Chưa có.** `Return` chỉ đặt `TrangThai = "DaTra"` và cộng lại số lượng | Sprint 2, Phương Anh |
| Báo cáo thống kê | Mới có các con số tổng (`BaoCaoController`). Chưa có lượt mượn theo sách hoặc theo thời gian, danh sách sách quá hạn, biểu đồ | Sprint 3, Quang Dũng |
| Tìm kiếm đa tiêu chí cho độc giả | Chưa có; hiện chỉ tìm theo tên và tác giả, và chưa có vai trò Độc giả | Sprint 3, Quang Dũng |
