# Quản lý thư viện

Ứng dụng ASP.NET Core MVC trên .NET 10. Dữ liệu được lưu dưới dạng JSON Lines trong các tệp `.txt` ở thư mục `database`; không cần SQL Server.

## Chạy ứng dụng

Cần .NET 10 SDK. Từ thư mục dự án:

```powershell
dotnet run
```

Trong VS Code, mở thư mục dự án, chọn **Run and Debug** rồi nhấn nút chạy (hoặc nhấn `F5`). VS Code sẽ tự build ứng dụng và mở trang tại `http://localhost:5262`. Cần cài extension **C# Dev Kit** nếu VS Code chưa hỗ trợ chạy và debug C#.

Tác vụ build của VS Code tự dừng các tiến trình cũ của ứng dụng này trước khi ghi DLL mới, tránh lỗi build do phiên chạy trước vẫn khóa tệp. Dùng **Run Library** hoặc `F5` để chạy lại; không cần mở hay sửa các tệp trong thư mục .NET SDK.

Khi mở địa chỉ gốc của ứng dụng, người dùng được chuyển tới trang đăng nhập. Sau khi đăng nhập, màn hình tổng quan mở ra; tìm kiếm và quản lý danh mục sách nằm chung trong mục **Sách**, không có trang tra cứu sách công khai riêng.

Thư mục dữ liệu có thể đổi bằng `LibraryData__DirectoryPath`; mặc định là `database` tương đối với thư mục ứng dụng. Sao lưu cả thư mục này định kỳ. Chỉ chạy một tiến trình ứng dụng tại một thời điểm.

`users.txt` chỉ lưu tên tài khoản, hash mật khẩu, vai trò và trạng thái hoạt động. Hồ sơ cá nhân (họ tên, email, số điện thoại, địa chỉ) được lưu riêng trong `profiles.txt`; email chỉ là thông tin hồ sơ, không dùng để đăng nhập. Các tệp dữ liệu còn có độc giả, sách, phiếu mượn/trả, phiếu thu tiền phạt, lịch sử điều chỉnh tồn kho và `settings.txt` (quy định hệ thống). Nếu tìm thấy `library.json` cũ, ứng dụng tự chia dữ liệu vào các tệp mới và giữ bản gốc thành `library.json.migrated.bak`.

## Tài khoản mặc định

Ở lần chạy đầu tiên (chưa có tài khoản nào), ứng dụng tạo bốn tài khoản ban đầu. Từ các lần sau, ứng dụng không tạo lại tài khoản đã có; chỉ khi không còn tài khoản Quản lý nào thì `admin` được dựng lại để không bị khóa khỏi hệ thống.

| Vai trò | Tên tài khoản |
|---|---|
| Quản lý | `admin` |
| Nhân viên | `nhanvien1` |
| Nhân viên | `nhanvien2` |
| Nhân viên | `nhanvien3` |

Mật khẩu khởi tạo theo thông tin đã yêu cầu và được kiểm tra bằng ASP.NET Core `PasswordHasher`, không lưu dưới dạng văn bản rõ. Tên tài khoản và mật khẩu không thể chỉnh sửa từ trang hồ sơ. Email và thông tin cá nhân chỉ có vai trò chi tiết hồ sơ. Sau 5 lần đăng nhập sai liên tiếp, tài khoản bị khóa tạm 15 phút.

## Vai trò và chức năng

- **Nhân viên**: nghiệp vụ quầy gồm quản lý hồ sơ độc giả (thêm, sửa, gia hạn thẻ, xóa độc giả chưa có lịch sử mượn), thêm/sửa sách, loại bản hỏng/mất khỏi tồn kho có nhật ký, lập phiếu mượn/trả, kiểm tra lịch sử và thu tiền phạt có phiếu thu. Khi lập phiếu mượn, tìm sách bằng cách gõ tiền tố tên hoặc mã `SH-`; có thể tra cứu người mượn theo tên/mã sách. Nhân viên không sửa thể loại hoặc quy định, không quản lý tài khoản.
- **Admin (vai trò `Quản lý`)**: có toàn bộ quyền của Nhân viên; thêm/sửa/xóa thể loại, xóa độc giả chưa có lịch sử mượn, xem danh sách và sửa hồ sơ nhân viên, reset mật khẩu, khóa/mở và xóa tài khoản nhân viên, sửa quy định và xem/in thống kê vĩ mô theo khoảng ngày. Thống kê gồm xu hướng mượn theo tháng, trạng thái phiếu, top 5 sách được mượn nhiều, phiếu gần đây và tiền phạt đã thu. Không thể xóa nhân viên đã có lịch sử lập phiếu mượn/trả.
- **Thống kê**: chỉ Admin được truy cập; bộ lọc ngày áp dụng cho phiếu mượn, phiếu trả, trạng thái hiện tại của các phiếu trong kỳ, sách được mượn và tiền phạt thực thu. **Tổng quan** vẫn hiển thị số liệu hiện tại, không lọc ngày.
- **Quy định ban đầu**: mượn tối đa 14 ngày và 5 bản cùng lúc; phạt trễ 2.000 ₫/bản/ngày, sách hư hỏng 50.000 ₫/bản, sách mất 100.000 ₫/bản; tuổi làm thẻ 6–100; thẻ có hạn 12 tháng. Admin có thể đổi các giá trị này tại **Quy định**. Mức phạt trễ được chụp theo từng phiếu mượn; tiền phạt và phiếu thu đã ghi nhận không bị tính lại khi đổi quy định.
- Tìm kiếm sách, lọc theo thể loại và tình trạng tồn kho được gộp tại **Sách**; thể loại hiển thị ngay trong danh sách và không cần mục menu riêng. Admin mở quản lý thể loại từ trang **Sách**. Nút **Xóa** loại số bản hỏng/mất khỏi tồn kho theo lý do và số lượng; thông tin đầu sách cùng lịch sử mượn vẫn được giữ.
- Danh sách độc giả tìm theo tên, email, điện thoại hoặc địa chỉ, không phân biệt hoa thường và dấu tiếng Việt, có thể gõ nhiều từ.
- Giao diện thích ứng cho điện thoại, máy tính bảng và máy tính: biểu mẫu chuyển sang một cột, danh sách có nhãn theo từng dòng, bảng nghiệp vụ cuộn ngang trong vùng riêng và menu thu gọn trên màn hình nhỏ.
- Cả hai vai trò có thể cập nhật hồ sơ cá nhân. Tên tài khoản hiển thị trên thanh điều hướng mở hồ sơ và nút thoát.
- Nhân viên có thể tự đăng ký công khai tại **Đăng ký**; tài khoản được kích hoạt ngay và chỉ được gán vai trò `Nhân viên` (không có lựa chọn vai trò `Quản lý`). Đăng nhập bằng tên tài khoản, không dùng email. Mật khẩu mới được lưu dưới dạng hash.

Khi khởi động, ứng dụng bổ sung dữ liệu minh họa sách, thể loại và độc giả còn thiếu. Seeder không ghi đè những bản ghi nghiệp vụ đã có; các địa chỉ giữ chỗ `Dữ liệu minh họa` của độc giả mẫu được thay bằng địa chỉ công cộng có thật tại Hà Nội, còn địa chỉ đã được cập nhật riêng thì được giữ nguyên.
