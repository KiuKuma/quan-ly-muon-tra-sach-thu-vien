# Quản lý thư viện

Ứng dụng ASP.NET Core MVC trên .NET 10. Dữ liệu được lưu dưới dạng JSON Lines trong các tệp `.txt` ở `App_Data`; không cần SQL Server.

## Chạy ứng dụng

Cần .NET 10 SDK. Từ thư mục dự án:

```powershell
dotnet run
```

Trong VS Code, mở thư mục dự án, chọn **Run and Debug** rồi nhấn nút chạy (hoặc nhấn `F5`). VS Code sẽ tự build ứng dụng và mở trang tại `http://localhost:5262`. Cần cài extension **C# Dev Kit** nếu VS Code chưa hỗ trợ chạy và debug C#.

Thư mục dữ liệu có thể đổi bằng `LibraryData__DirectoryPath`; mặc định là `App_Data` tương đối với thư mục ứng dụng. Sao lưu cả thư mục này định kỳ. Chỉ chạy một tiến trình ứng dụng tại một thời điểm.

`users.txt` chỉ lưu tên tài khoản, hash mật khẩu và vai trò. Hồ sơ cá nhân (họ tên, email, số điện thoại, địa chỉ) được lưu riêng trong `profiles.txt`; email chỉ là thông tin hồ sơ, không dùng để đăng nhập. Các tệp còn lại lưu độc giả, thể loại, sách, nhân viên, phiếu mượn và chi tiết phiếu. Nếu tìm thấy `library.json` cũ, ứng dụng tự chia dữ liệu vào các tệp mới và giữ bản gốc thành `library.json.migrated.bak`.

## Tài khoản mặc định

Khi khởi động, ứng dụng bảo đảm có đúng bốn tài khoản ban đầu:

| Vai trò | Tên tài khoản |
|---|---|
| Quản lý | `admin` |
| Nhân viên | `nhanvien1` |
| Nhân viên | `nhanvien2` |
| Nhân viên | `nhanvien3` |

Mật khẩu khởi tạo theo thông tin đã yêu cầu và được kiểm tra bằng ASP.NET Core `PasswordHasher`, không lưu dưới dạng văn bản rõ. Tên tài khoản và mật khẩu không thể chỉnh sửa từ trang hồ sơ. Email và thông tin cá nhân chỉ có vai trò chi tiết hồ sơ. Sau 5 lần đăng nhập sai liên tiếp, tài khoản bị khóa tạm 15 phút.

## Vai trò và chức năng

- **Nhân viên**: xem sách, thể loại và độc giả; lập phiếu mượn, ghi nhận trả sách và xem báo cáo thống kê.
- **Quản lý**: có toàn bộ quyền của nhân viên, được thêm/sửa/xóa sách, thể loại và độc giả, đồng thời xem danh sách nhân viên.
- Cả hai vai trò có thể cập nhật hồ sơ cá nhân. Tên tài khoản hiển thị trên thanh điều hướng mở hồ sơ và nút thoát.
- Không có đăng ký công khai hoặc đăng nhập bằng email.

Khi khởi động, ứng dụng bổ sung dữ liệu minh họa sách, thể loại và độc giả còn thiếu. Seeder không ghi đè những bản ghi nghiệp vụ đã có.
