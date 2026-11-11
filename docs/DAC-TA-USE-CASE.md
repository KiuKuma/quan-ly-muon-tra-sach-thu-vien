# Đặc tả Use Case – Quản lý mượn trả sách thư viện

**Người thực hiện:** Đặng Huy Quang Dũng (Product Owner)  
**Cơ sở:** đề cương (mục 1.1 đến 1.8) và mã nguồn nhánh `develop`, commit `dcc0e3d` (kèm các sửa của nhánh `feature/sprint1-ra-soat-quang-dung`). Liên kết với [PRODUCT-BACKLOG.md](PRODUCT-BACKLOG.md) và [ERD.md](ERD.md).  
**Ghi chú:** use case đánh dấu *Đề xuất* là chức năng đề cương yêu cầu nhưng code chưa có; luồng của chúng là đề xuất để nhóm xác nhận.

## 1. Tác nhân

| Tác nhân | Vai trò trong code | Mô tả |
|---|---|---|
| Thủ thư | `Nhân viên` | Xem danh mục, lập phiếu mượn, ghi nhận trả sách, xem báo cáo |
| Quản trị viên | `Quản lý` | Có toàn bộ quyền của Thủ thư, ngoài ra thêm/sửa/xóa sách, thể loại, độc giả và xem danh sách tài khoản nhân viên |
| Độc giả | chưa có | Tra cứu sách, xem lịch sử mượn của mình (đề xuất) |

## 2. Sơ đồ use case

```mermaid
flowchart LR
    TT["Thủ thư"]
    QT["Quản trị viên"]
    DG["Độc giả (đề xuất)"]
    QT -. "kế thừa quyền" .-> TT

    subgraph HT["Hệ thống quản lý mượn trả sách"]
        UC01(["UC01 Đăng nhập"])
        UC02(["UC02 Cập nhật hồ sơ cá nhân"])
        UC03(["UC03 Xem và sửa tài khoản nhân viên"])
        UC04(["UC04 Quản lý sách"])
        UC05(["UC05 Quản lý thể loại"])
        UC06(["UC06 Quản lý độc giả"])
        UC07(["UC07 Lập phiếu mượn"])
        UC08(["UC08 Ghi nhận trả sách"])
        UC09(["UC09 Tính tiền phạt trễ hạn"])
        UC10(["UC10 Kiểm tra điều kiện mượn"])
        UC11(["UC11 Xem chi tiết phiếu mượn"])
        UC12(["UC12 Xem lịch sử mượn trả"])
        UC13(["UC13 Tìm kiếm sách"])
        UC14(["UC14 Xem tổng quan"])
        UC15(["UC15 Xem báo cáo thống kê"])
    end

    TT --> UC01
    TT --> UC02
    TT --> UC07
    TT --> UC08
    TT --> UC11
    TT --> UC12
    TT --> UC13
    TT --> UC14
    TT --> UC15
    TT --> UC04
    TT --> UC05
    TT --> UC06
    QT --> UC03
    DG --> UC01
    DG --> UC12
    DG --> UC13
    UC07 -. "include" .-> UC10
    UC08 -. "include" .-> UC09
```

Thủ thư chỉ **xem** ở UC04, UC05, UC06; Quản trị viên mới được thêm, sửa, xóa.

## 3. Danh sách use case

| ID | Tên | Tác nhân | Sprint | Trạng thái |
|---|---|---|---|---|
| UC01 | Đăng nhập | Thủ thư, Quản trị viên (Độc giả: đề xuất) | 1 | Xong |
| UC02 | Cập nhật hồ sơ cá nhân | Thủ thư, Quản trị viên | 1 | Xong |
| UC03 | Xem và sửa tài khoản nhân viên | Quản trị viên | 1 | Xong |
| UC04 | Quản lý sách | Quản trị viên (Thủ thư xem) | 1 | Xong |
| UC05 | Quản lý thể loại | Quản trị viên (Thủ thư xem) | 1 | Xong |
| UC06 | Quản lý độc giả | Quản trị viên (Thủ thư xem) | 1 | Xong |
| UC07 | Lập phiếu mượn | Thủ thư | 2 | Xong |
| UC08 | Ghi nhận trả sách | Thủ thư | 2 | Xong |
| UC09 | Tính tiền phạt trễ hạn | Hệ thống | 2 | Xong |
| UC10 | Kiểm tra điều kiện mượn | Hệ thống | 2 | Xong |
| UC11 | Xem chi tiết phiếu mượn | Thủ thư | 2 | Xong |
| UC12 | Xem lịch sử mượn trả | Thủ thư, Độc giả | 2, 3 | Đề xuất |
| UC13 | Tìm kiếm sách | Thủ thư, Độc giả | 1, 3 | Một phần (chỉ theo tên, tác giả) |
| UC14 | Xem tổng quan | Thủ thư, Quản trị viên | 1 | Xong |
| UC15 | Xem báo cáo thống kê | Thủ thư, Quản trị viên | 3 | Một phần |

## 4. Đặc tả chi tiết

### UC01. Đăng nhập

| Mục | Nội dung |
|---|---|
| Tác nhân | Thủ thư, Quản trị viên |
| Mô tả | Người dùng đăng nhập bằng tên tài khoản và mật khẩu để dùng hệ thống |
| Tiền điều kiện | Có tài khoản trong hệ thống. Tài khoản hiện do hệ thống tạo sẵn, không đăng ký công khai |
| Hậu điều kiện | Người dùng được xác thực và chuyển tới trang tổng quan theo đúng vai trò |
| Luồng chính | 1. Người dùng mở trang đăng nhập. 2. Nhập tên tài khoản, mật khẩu (có thể chọn ghi nhớ đăng nhập). 3. Hệ thống kiểm tra mật khẩu với bản băm đã lưu. 4. Hệ thống tạo phiên đăng nhập và chuyển tới trang tổng quan |
| Luồng thay thế | 3a. Sai tên hoặc mật khẩu: báo lỗi, tăng số lần sai. 3b. Sai 5 lần liên tiếp: khóa tài khoản tạm 15 phút. 3c. Tài khoản đang bị khóa: báo tài khoản tạm khóa, thử lại sau 15 phút |
| Quy tắc | Đăng nhập bằng tên tài khoản, không dùng email. Truy cập chức năng không đủ quyền thì chuyển tới trang từ chối truy cập |

### UC04. Quản lý sách

| Mục | Nội dung |
|---|---|
| Tác nhân | Quản trị viên (thêm, sửa, xóa); Thủ thư (xem, tìm) |
| Mô tả | Quản lý danh mục đầu sách và số lượng trong kho |
| Tiền điều kiện | Đã đăng nhập. Thể loại không bắt buộc: sách chưa chọn thể loại hiển thị là "Chưa phân loại" |
| Hậu điều kiện | Dữ liệu sách trong `books.txt` được cập nhật |
| Luồng chính (thêm) | 1. Quản trị viên chọn Thêm sách. 2. Nhập tên sách, tác giả, nhà xuất bản, năm xuất bản, số lượng và chọn thể loại (nếu có). 3. Hệ thống tự cấp mã sách, kiểm tra hợp lệ và lưu sách. 4. Hiển thị thông báo thành công |
| Luồng chính (sửa, xóa, xem) | Sửa: chọn sách, chỉnh các trường rồi lưu. Xóa: chọn sách, xác nhận ở trang xác nhận. Xem: mở chi tiết sách kèm tên thể loại |
| Luồng thay thế | 3a. Thiếu tên hoặc tác giả: báo lỗi tại trường. 3b. Năm xuất bản lớn hơn năm hiện tại hoặc thể loại không tồn tại: báo lỗi tại trường. 4a. Xóa sách đã có trong phiếu mượn: không xóa, báo lỗi |
| Quy tắc | Tên sách tối đa 200 ký tự, tác giả 120. Năm xuất bản từ 1450 đến năm hiện tại. Số lượng 0 đến 100000 |

### UC05. Quản lý thể loại

| Mục | Nội dung |
|---|---|
| Tác nhân | Quản trị viên (thêm, sửa, xóa); Thủ thư (xem, tìm) |
| Mô tả | Quản lý các thể loại để phân loại sách |
| Luồng chính | Thêm, sửa, xóa, xem thể loại. Chi tiết thể loại hiển thị danh sách sách thuộc thể loại |
| Luồng thay thế | Tên trùng (không phân biệt hoa thường): báo "Tên thể loại đã tồn tại". Xóa thể loại còn sách: không xóa, báo lỗi |
| Quy tắc | Tên thể loại tối đa 100 ký tự, mô tả tối đa 500 ký tự |

### UC06. Quản lý độc giả

| Mục | Nội dung |
|---|---|
| Tác nhân | Quản trị viên (thêm, sửa, xóa); Thủ thư (xem, tìm) |
| Mô tả | Quản lý thông tin độc giả để lập phiếu mượn |
| Luồng chính (thêm) | 1. Quản trị viên chọn Thêm độc giả. 2. Nhập họ tên, email, số điện thoại, địa chỉ. 3. Hệ thống kiểm tra hợp lệ. 4. Hệ thống lưu và gán ngày đăng ký là thời điểm hiện tại |
| Luồng thay thế | Email trùng độc giả khác hoặc hồ sơ tài khoản: báo lỗi. Xóa độc giả đã có phiếu mượn: không xóa, báo lỗi |
| Quy tắc | Họ tên tối đa 120 ký tự, email hợp lệ và tối đa 256 ký tự, số điện thoại tối đa 20. Tìm theo họ tên hoặc email |

### UC07. Lập phiếu mượn

| Mục | Nội dung |
|---|---|
| Tác nhân | Thủ thư |
| Mô tả | Ghi nhận việc độc giả mượn sách và cập nhật tồn kho |
| Tiền điều kiện | Đã đăng nhập; có độc giả; có sách còn ít nhất một bản |
| Hậu điều kiện | Có phiếu mới trạng thái "Đang mượn"; số lượng sách còn giảm đúng số đã mượn |
| Luồng chính | 1. Thủ thư nhập mã độc giả và xác nhận tên tự hiển thị. 2. Nhập ngày mượn, ngày hẹn trả và chọn một hoặc nhiều sách. 3. Hệ thống hiển thị mã, tác giả, nhà xuất bản, số lượng còn; thủ thư nhập số lượng mượn. 4. Hệ thống kiểm tra tồn kho, tạo mã phiếu tự động, lưu chi tiết và trừ tồn kho. |
| Luồng thay thế | Mã độc giả hoặc sách không hợp lệ, ngày không đúng thứ tự, hoặc tồn kho không đủ: không tạo phiếu và báo lỗi. |
| Quy tắc | Mỗi đầu sách mượn từ 1 đến 100 cuốn; nhiều đầu sách có thể cùng thuộc một phiếu |

### UC08. Ghi nhận trả sách

| Mục | Nội dung |
|---|---|
| Tác nhân | Thủ thư |
| Mô tả | Kiểm tra sách nhận lại và lập phiếu trả cho một phần hoặc toàn bộ phiếu mượn |
| Tiền điều kiện | Phiếu tồn tại và còn sách chưa trả |
| Hậu điều kiện | Có phiếu trả và chi tiết trả ghi số lượng, tình trạng, ghi chú; trạng thái phiếu mượn được cập nhật |
| Luồng chính | 1. Thủ thư tìm phiếu theo mã thẻ độc giả. 2. Chọn phiếu, kiểm tra từng đầu sách. 3. Nhập số lượng nhận, tình trạng tốt/hư hỏng và ghi chú. 4. Hệ thống tạo phiếu trả, cộng sách tốt vào tồn kho và cập nhật trạng thái phiếu mượn. |
| Luồng thay thế | Số lượng trả vượt số còn mượn hoặc tình trạng không hợp lệ: không lập phiếu và báo lỗi. |
| Quy tắc | Có thể lập nhiều phiếu trả trên một phiếu mượn; sách hư hỏng được ghi nhận nhưng không cộng vào số lượng sẵn có. Tra cứu quản lý hỗ trợ lọc mã thẻ và trạng thái; màn hình chi tiết hiển thị từng lần trả và tiền phạt. Tiền phạt được chốt theo số lượng sách thực trả tại mỗi lần trả. |

### UC09. Tính tiền phạt trễ hạn

| Mục | Nội dung |
|---|---|
| Tác nhân | Hệ thống (kích hoạt khi Thủ thư thực hiện UC08) |
| Mô tả | Khi trả sách sau hạn, hệ thống tính tiền phạt lũy tiến theo số ngày trễ |
| Luồng chính | 1. Ghi nhận ngày trả thực tế. 2. Nếu ngày trả sau hạn trả, tính số ngày trễ. 3. Áp mức phạt lũy tiến để ra tiền phạt. 4. Lưu tiền phạt vào chi tiết phiếu trả (`ChiTietPhieuTra.TienPhat` trong ERD 3NF). 5. Hiển thị tiền phạt cho Thủ thư |
| Quy tắc | Tính theo từng cuốn còn đang mượn, cộng dồn theo bậc biên: ngày 1–7 là 1.000đ/cuốn/ngày; ngày 8–30 là 2.000đ/cuốn/ngày; từ ngày 31 là 5.000đ/cuốn/ngày. Trong lúc phiếu còn sách chưa trả, hiển thị và tính vào nợ phạt số tiền tạm tính đến ngày hiện tại. Khi trả từng phần, chốt tiền phạt cho số sách trả vào chi tiết phiếu trả; phần còn mượn tiếp tục phát sinh phạt, không tính trùng số sách đã trả. Ngày trễ = ngày trả thực tế trừ hạn trả, không tính phạt nếu trả đúng hạn. |

### UC10. Kiểm tra và thanh toán nợ phạt

| Mục | Nội dung |
|---|---|
| Tác nhân | Hệ thống (kích hoạt trong UC07) |
| Mô tả | Ngăn tạo phiếu mượn mới khi độc giả còn tiền phạt chưa thanh toán |
| Luồng chính | 1. Khi gửi yêu cầu mượn, hệ thống tìm mọi sách chưa trả và tiền phạt (bao gồm khoản phạt tạm tính của sách quá hạn chưa trả) trong cùng vùng khóa cập nhật dữ liệu. 2. Nếu còn sách chưa trả hoặc tiền phạt, không tạo phiếu, không trừ tồn kho và hiển thị lý do cụ thể. 3. Nhân viên nhận sách và xác nhận thu tiền trong chi tiết phiếu trả. 4. Khi không còn sách chưa trả hoặc nợ phạt, độc giả có thể mượn sách |
| Luồng thay thế | Phiếu trả đã thanh toán, không có nợ phạt hoặc không còn sách chưa trả: cho phép tiếp tục; không ghi nhận thu trùng |
| Quy tắc | Mọi kiểm tra nợ phạt và cập nhật tồn kho/phiếu mượn chạy bên trong cùng `LibraryFileStore.UpdateAsync` để đồng bộ trong một tiến trình ứng dụng. |

### UC11. Xem chi tiết phiếu mượn

| Mục | Nội dung |
|---|---|
| Tác nhân | Nhân viên, Quản lý |
| Mô tả | Xem sách, số lượng còn phải trả, thông tin độc giả và lịch sử trả/phạt/thanh toán của một phiếu |
| Luồng chính | 1. Chọn mã phiếu trong danh sách. 2. Hệ thống hiển thị thông tin phiếu mượn và từng đầu sách. 3. Hiển thị từng phiếu trả, tiền phạt đã chốt, số đã thu và số còn nợ. 4. Với khoản còn nợ, nhân viên xác nhận thu tiền từ màn hình chi tiết |
| Luồng thay thế | Mã phiếu không tồn tại: trả về trang không tìm thấy |

### UC12. Xem lịch sử mượn trả *(Đề xuất, Sprint 2 và 3)*

| Mục | Nội dung |
|---|---|
| Tác nhân | Thủ thư (xem của bất kỳ độc giả); Độc giả (chỉ xem của mình, khi có tài khoản Độc giả) |
| Mô tả | Xem các phiếu mượn của một độc giả kèm sách, ngày mượn, hạn trả, trạng thái |
| Luồng chính | 1. Chọn độc giả (Thủ thư) hoặc mở trang cá nhân (Độc giả). 2. Hệ thống liệt kê phiếu theo thời gian mới nhất trước. 3. Hiển thị trạng thái Đang mượn, Quá hạn, Đã trả |
| Luồng thay thế | Độc giả chưa có phiếu nào: hiển thị thông báo trống |

### UC13. Tìm kiếm sách

| Mục | Nội dung |
|---|---|
| Tác nhân | Thủ thư (hiện có); Độc giả (đề xuất, Sprint 3) |
| Mô tả | Tìm sách trong danh mục |
| Luồng chính | 1. Nhập từ khóa. 2. Hệ thống lọc sách theo tên hoặc tác giả, không phân biệt hoa thường. 3. Hiển thị danh sách sắp xếp theo tên sách |
| Luồng thay thế | Không có kết quả: hiển thị "Không tìm thấy sách phù hợp" |
| Mở rộng (Sprint 3) | Lọc thêm theo thể loại, năm xuất bản; tìm không phân biệt dấu tiếng Việt |

### UC15. Xem báo cáo thống kê

| Mục | Nội dung |
|---|---|
| Tác nhân | Thủ thư, Quản trị viên |
| Mô tả | Xem số liệu tổng hợp tình hình thư viện |
| Hiện có | Số đầu sách, số bản còn trong kho, số thể loại, số độc giả, tổng số phiếu, số phiếu đang mượn, quá hạn, đã trả |
| Mở rộng (Sprint 3) | Thống kê lượt mượn theo sách và thời gian, danh sách sách quá hạn, biểu đồ |

## 5. Quy tắc nghiệp vụ chung

| Mã | Quy tắc |
|---|---|
| BR-01 | Số lượng sách trong kho không âm; mượn làm giảm, trả làm tăng đúng số lượng trên phiếu |
| BR-02 | Không xóa sách, độc giả đã có phiếu mượn; không xóa thể loại còn sách |
| BR-03 | Email độc giả, tên thể loại không được trùng |
| BR-04 | Hạn trả không được trước ngày lập phiếu |
| BR-05 | Phiếu quá hạn là phiếu chưa trả mà hạn trả đã qua |
| BR-06 | Chỉ Quản trị viên được thêm, sửa, xóa danh mục; Thủ thư chỉ xem |
