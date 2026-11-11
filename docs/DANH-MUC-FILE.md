# Danh mục file trong dự án

Tài liệu này mô tả vai trò của các file do dự án quản lý. Đây là bản đồ mã nguồn và dữ liệu, không phải bản sao nguyên văn nội dung từng file.

> **Bảo vệ dữ liệu:** Các file trong `database/` và bản sao cũ trong `App_Data/` có thể chứa thông tin cá nhân, tài khoản hoặc dữ liệu nghiệp vụ. Tài liệu chỉ mô tả công dụng, không ghi lại dữ liệu bên trong. `users.txt` lưu hash mật khẩu; không chia sẻ các file dữ liệu này công khai.
>
> Các file thư viện bên thứ ba trong `wwwroot/lib/`, file sinh tự động trong `bin/`, `obj/`, và metadata Git không được liệt kê từng file. Chúng không phải mã nguồn nghiệp vụ do dự án tự viết.

## 1. File ở thư mục gốc và cấu hình

| File | Công dụng |
|---|---|
| `README.md` | Giới thiệu dự án, cách chạy, dữ liệu, tài khoản mặc định và quyền của Quản lý/Nhân viên. |
| `Program.cs` | Điểm khởi chạy ứng dụng: đăng ký MVC, lưu file, dịch vụ tài khoản, mã hóa mật khẩu, cookie đăng nhập, phân quyền, định tuyến và khởi tạo dữ liệu. |
| `quan-ly-muon-tra-sach-thu-vien.csproj` | Cấu hình dự án ASP.NET Core MVC, phiên bản .NET, nullable reference types và namespace gốc. |
| `appsettings.json` | Cấu hình logging, thư mục dữ liệu (`LibraryData:DirectoryPath`) và host được phép. |
| `appsettings.Development.json` | Ghi đè cấu hình logging khi chạy môi trường Development. |
| `.gitignore` | Quy định những file/thư mục Git bỏ qua, ví dụ file tạm và file do build sinh ra. |
| `Properties/launchSettings.json` | Các profile chạy local, URL HTTP/HTTPS và biến môi trường khi phát triển. |

## 2. Cấu hình Visual Studio Code

| File | Công dụng |
|---|---|
| `.vscode/launch.json` | Cấu hình F5/Run and Debug, chạy ứng dụng .NET và tự mở địa chỉ local khi server sẵn sàng. |
| `.vscode/tasks.json` | Các task build và dừng tiến trình ứng dụng cũ trước khi build để tránh DLL bị khóa. |
| `.vscode/stop-running-library.ps1` | Tìm và dừng tiến trình chạy ứng dụng thư viện phục vụ task build của VS Code. |

## 3. Controllers — xử lý yêu cầu và nghiệp vụ màn hình

| File | Công dụng và chức năng |
|---|---|
| `Controllers/HomeController.cs` | Hiển thị tổng quan hiện tại cho tài khoản đã đăng nhập; chuyển người chưa đăng nhập tới trang đăng nhập; trang Privacy và trang lỗi. |
| `Controllers/AccountController.cs` | Đăng ký nhân viên công khai, đăng nhập/đăng xuất bằng cookie, hiển thị và cập nhật hồ sơ người dùng, trang từ chối quyền truy cập. |
| `Controllers/TaiKhoanController.cs` | Admin xem danh sách, sửa hồ sơ, reset mật khẩu, khóa/mở và xóa tài khoản nhân viên có điều kiện. |
| `Controllers/QuyDinhController.cs` | Admin xem/cập nhật quy định mượn, phạt, tuổi độc giả và thời hạn thẻ. |
| `Controllers/SachController.cs` | Danh sách/tìm kiếm/lọc theo thể loại và tồn kho, chi tiết, thêm/sửa sách và loại bản hỏng/mất khỏi tồn kho có ghi nhật ký cho nhân viên/Admin. |
| `Controllers/TheLoaiController.cs` | Danh sách, chi tiết, thêm, sửa và xóa thể loại; kiểm tra trùng tên và sách đang thuộc thể loại. |
| `Controllers/DocGiaController.cs` | Nhân viên/Admin tạo, sửa, gia hạn thẻ và xem lịch sử; xóa độc giả chỉ dành cho Admin. Kiểm tra email, tuổi và lịch sử mượn. |
| `Controllers/MuonTraController.cs` | Lập phiếu mượn/trả, kiểm tra hạn mức, ghi nhận tình trạng tốt/hỏng/mất, tính phạt, lập phiếu thu và xem/in biên nhận. |
| `Controllers/BaoCaoController.cs` | Admin xem xu hướng mượn, trạng thái phiếu, top sách, phiếu gần đây và doanh thu tiền phạt theo khoảng ngày. |

## 4. Data — lưu trữ, tài khoản và tiện ích nghiệp vụ

| File | Công dụng và chức năng |
|---|---|
| `Data/LibraryData.cs` | Mô hình dữ liệu tổng hợp trong bộ nhớ (`LibraryData`), bản ghi tài khoản (`LibraryAccount`) và hồ sơ tài khoản (`LibraryUserProfile`). |
| `Data/LibraryFileStore.cs` | Đọc/ghi 13 bộ dữ liệu JSON Lines; khóa đồng bộ thao tác, ghi file tạm rồi thay thế file chính, chuyển đổi dữ liệu từ file cũ. |
| `Data/LibraryAccountService.cs` | Tạo tài khoản mặc định và đăng ký nhân viên; xác thực, hash/khóa đăng nhập, cập nhật hồ sơ, reset mật khẩu, khóa/mở và xóa tài khoản có điều kiện. |
| `Data/LibraryRoles.cs` | Hằng số tên vai trò `Quản lý` và `Nhân viên` dùng chung trong phân quyền. |
| `Data/LibraryDataInitializer.cs` | Điều phối việc tạo cấu trúc file, tài khoản mặc định và dữ liệu mẫu lúc ứng dụng khởi động. |
| `Data/LibrarySampleDataSeeder.cs` | Bổ sung dữ liệu mẫu sách, thể loại, độc giả, nhân viên và phiếu mượn khi còn thiếu. |
| `Data/VietnameseText.cs` | Chuẩn hóa từ khóa tiếng Việt để tìm kiếm không phân biệt dấu/hoa thường và khớp nhiều từ. |

## 5. Models — mô hình dữ liệu nghiệp vụ

| File | Công dụng |
|---|---|
| `Models/Sach.cs` | Thông tin sách trong danh mục, gồm dữ liệu xuất bản, số lượng và liên kết thể loại. |
| `Models/TheLoai.cs` | Thông tin thể loại sách và liên kết tới sách thuộc thể loại. |
| `Models/DocGia.cs` | Hồ sơ độc giả, ngày sinh, ngày đăng ký, hạn thẻ và liên kết tài khoản nếu có. |
| `Models/NhanVien.cs` | Hồ sơ nhân viên nghiệp vụ và liên kết tới tài khoản đăng nhập. |
| `Models/PhieuMuon.cs` | Phiếu mượn: độc giả, nhân viên lập phiếu, ngày mượn/hẹn trả, mức phạt trễ đã chụp và các dòng sách mượn. |
| `Models/ChiTietPhieuMuon.cs` | Một dòng sách và số lượng tương ứng trong phiếu mượn. |
| `Models/PhieuTra.cs` | Phiếu trả: lần trả của một phiếu mượn, nhân viên ghi nhận, ngày trả và thông tin thanh toán phạt. |
| `Models/ChiTietPhieuTra.cs` | Chi tiết từng sách được trả: số lượng, tình trạng, ghi chú và tiền phạt đã chốt. |
| `Data/LibraryData.cs` | Còn định nghĩa quy định thư viện, phiếu thu tiền phạt và nhật ký điều chỉnh tồn kho. |
| `Models/ErrorViewModel.cs` | Dữ liệu mã yêu cầu dùng để hiển thị trang lỗi. |

## 6. Models/ViewModels — dữ liệu truyền giữa controller và giao diện

| File | Công dụng |
|---|---|
| `Models/ViewModels/LoginViewModel.cs` | Dữ liệu form đăng nhập, gồm tên tài khoản, mật khẩu, ghi nhớ đăng nhập và URL quay lại. |
| `Models/ViewModels/LibraryProfileViewModel.cs` | Dữ liệu xem/cập nhật hồ sơ tài khoản. |
| `Models/ViewModels/RegisterStaffAccountViewModel.cs` | Dữ liệu form đăng ký nhân viên; kiểm tra thông tin cá nhân, email, tên tài khoản, mật khẩu và xác nhận mật khẩu. |
| `Models/ViewModels/AccountSummaryViewModel.cs` | Dữ liệu rút gọn để hiển thị danh sách tài khoản/nhân viên. |
| `Models/ViewModels/LibraryOverviewViewModel.cs` | Các chỉ số hiện tại trên trang tổng quan, không lọc theo ngày. |
| `Models/ViewModels/LibraryStatisticsViewModel.cs` | Số liệu thống kê theo khoảng ngày, trạng thái phiếu, xu hướng tháng, sách được mượn nhiều và phiếu gần đây. |
| `Models/ViewModels/LibraryLoanViewModels.cs` | Các mô hình cho mượn/trả, lịch sử độc giả, phiếu thu, quy định hệ thống và điều chỉnh tồn. |
| `Models/ViewModels/PagedResult.cs` | Kết quả phân trang chung, dữ liệu trang hiện tại và thông tin điều hướng trang; cũng chứa mô hình chi tiết thể loại. |

## 7. Views — giao diện Razor

### Views dùng chung

| File | Công dụng |
|---|---|
| `Views/_ViewImports.cshtml` | Khai báo namespace và Tag Helpers dùng trong các view Razor. |
| `Views/_ViewStart.cshtml` | Chọn layout mặc định cho các view. |
| `Views/Shared/_Layout.cshtml` | Khung giao diện chung: menu, liên kết theo vai trò, thông tin đăng nhập, thông báo, CSS và JavaScript. |
| `Views/Shared/_Layout.cshtml.css` | CSS được scope riêng cho layout Razor. |
| `Views/Shared/_LoanNavigation.cshtml` | Menu con dùng chung cho các màn hình mượn/trả. |
| `Views/Shared/_Pagination.cshtml` | Điều hướng trang dùng chung cho danh sách có phân trang. |
| `Views/Shared/_ValidationScriptsPartial.cshtml` | Nạp thư viện kiểm tra form phía trình duyệt. |
| `Views/Shared/Error.cshtml` | Giao diện lỗi dùng chung. |

### Trang chủ, tài khoản và thống kê

| File | Công dụng |
|---|---|
| `Views/Home/Index.cshtml` | Giao diện trang tổng quan. |
| `Views/Home/Privacy.cshtml` | Trang thông tin quyền riêng tư mẫu của ASP.NET Core. |
| `Views/Account/Login.cshtml` | Form đăng nhập. |
| `Views/Account/Register.cshtml` | Form đăng ký nhân viên, tài khoản được kích hoạt ngay với vai trò Nhân viên. |
| `Views/Account/Profile.cshtml` | Xem và sửa hồ sơ tài khoản đang đăng nhập. |
| `Views/Account/AccessDenied.cshtml` | Thông báo người dùng không có quyền truy cập. |
| `Views/TaiKhoan/Index.cshtml` | Danh sách tài khoản nhân viên với thao tác sửa hồ sơ, reset mật khẩu, khóa/mở và xóa. |
| `Views/QuyDinh/Index.cshtml` | Form cấu hình thời hạn/số lượng mượn, tiền phạt, độ tuổi và thời hạn thẻ. |
| `Views/TaiKhoan/Edit.cshtml` | Form sửa hồ sơ nhân viên, không sửa tên đăng nhập/mật khẩu tại màn hình này. |
| `Views/TaiKhoan/Delete.cshtml` | Xác nhận xóa tài khoản nhân viên. |
| `Views/BaoCao/Index.cshtml` | Dashboard thống kê có bộ lọc ngày, biểu đồ mượn, trạng thái phiếu, top sách, phiếu gần đây và tiền phạt; chỉ Admin truy cập. |

### Danh mục sách, thể loại, độc giả

| File | Công dụng |
|---|---|
| `Views/Sach/Index.cshtml` | Danh sách sách, thể loại, tìm kiếm/lọc tồn kho và các thao tác theo vai trò; Admin mở quản lý thể loại từ trang này. |
| `Views/Sach/Form.cshtml` | Form thêm hoặc sửa sách. |
| `Views/Sach/Details.cshtml` | Chi tiết một sách. |
| `Views/Sach/Inventory.cshtml` | Ghi nhận sách có sẵn hư hỏng/mất, trừ tồn và xem lịch sử điều chỉnh. |
| `Views/TheLoai/Index.cshtml` | Danh sách và tìm kiếm thể loại. |
| `Views/TheLoai/Form.cshtml` | Form thêm hoặc sửa thể loại. |
| `Views/TheLoai/Details.cshtml` | Chi tiết thể loại và danh sách sách liên quan. |
| `Views/TheLoai/Delete.cshtml` | Xác nhận xóa thể loại. |
| `Views/DocGia/Index.cshtml` | Danh sách và tìm kiếm độc giả. |
| `Views/DocGia/Form.cshtml` | Form thêm hoặc sửa hồ sơ độc giả. |
| `Views/DocGia/Details.cshtml` | Chi tiết độc giả và thông tin liên quan. |
| `Views/DocGia/History.cshtml` | Lịch sử mượn trả và công nợ phạt theo độc giả. |
| `Views/DocGia/Delete.cshtml` | Xác nhận xóa độc giả. |

### Mượn và trả sách

| File | Công dụng |
|---|---|
| `Views/MuonTra/Index.cshtml` | Danh sách phiếu mượn/trả, lọc và phân trang. |
| `Views/MuonTra/Borrow.cshtml` | Form lập phiếu mượn một hoặc nhiều đầu sách. |
| `Views/MuonTra/Return.cshtml` | Form ghi nhận trả sách, tình trạng sách và khoản phạt. |
| `Views/MuonTra/Details.cshtml` | Chi tiết phiếu mượn, các lần trả, tiền phạt và thanh toán. |
| `Views/MuonTra/FineReceipt.cshtml` | Biên nhận thu tiền phạt có thể in. |

## 8. CSS, JavaScript và tài nguyên tĩnh

| File | Công dụng |
|---|---|
| `wwwroot/css/site.css` | Kiểu dáng giao diện toàn ứng dụng, bố cục, bảng, form và các thành phần responsive. |
| `wwwroot/js/site.js` | Tương tác giao diện: thông báo, cập nhật dữ liệu khi chọn độc giả/sách, thao tác dòng sách mượn và kiểm tra trực tiếp mật khẩu/xác nhận mật khẩu. |
| `wwwroot/favicon.ico` | Biểu tượng trang web trên tab trình duyệt. |
| `wwwroot/lib/` | Thư viện frontend bên thứ ba, ví dụ Bootstrap, jQuery và thư viện kiểm tra form; không phải mã nghiệp vụ của dự án. |

## 9. Tài liệu thiết kế trong `docs/`

| File | Công dụng |
|---|---|
| `docs/CAU-TRUC-DU-LIEU.md` | Cấu trúc các file lưu dữ liệu, khởi tạo, ràng buộc và cách vận hành chức năng sách/độc giả/mượn/trả. |
| `docs/DAC-TA-USE-CASE.md` | Tác nhân, danh sách use case, luồng nghiệp vụ và quy tắc chung. |
| `docs/ERD.md` | Mô hình dữ liệu hiện tại, thiết kế ERD mục tiêu và phân tích chuẩn hóa 3NF. |
| `docs/PRODUCT-BACKLOG.md` | Backlog sản phẩm, epic, sprint và các nội dung cần thống nhất. |
| `docs/RA-SOAT-CODE.md` | Kết quả rà soát code, các điểm đã sửa và các điểm còn mở. |
| `docs/DANH-MUC-FILE.md` | File này: bản đồ các file của dự án và chức năng của chúng. |

## 10. Các file dữ liệu đang lưu trong `database/`

Ứng dụng lưu từng bản ghi JSON trên một dòng (JSON Lines). Không đưa giá trị bản ghi cụ thể vào tài liệu này vì file có thể chứa thông tin cá nhân, hash mật khẩu hoặc dữ liệu nghiệp vụ.

| File | Dữ liệu được lưu |
|---|---|
| `database/users.txt` | Tài khoản đăng nhập, hash mật khẩu, vai trò và trạng thái khóa đăng nhập. |
| `database/profiles.txt` | Hồ sơ cá nhân gắn với tài khoản: họ tên, email, số điện thoại và địa chỉ. |
| `database/staff.txt` | Hồ sơ nghiệp vụ nhân viên và liên kết tới tài khoản. |
| `database/readers.txt` | Hồ sơ độc giả, ngày sinh/hạn thẻ và liên kết tài khoản nếu có. |
| `database/categories.txt` | Danh sách thể loại sách. |
| `database/books.txt` | Danh mục sách và số lượng hiện có. |
| `database/loans.txt` | Phiếu mượn sách. |
| `database/loan-details.txt` | Các dòng sách/số lượng trong từng phiếu mượn. |
| `database/returns.txt` | Phiếu ghi nhận trả sách và thông tin thanh toán phạt. |
| `database/return-details.txt` | Chi tiết sách trả, tình trạng sách và tiền phạt đã chốt. |
| `database/fine-payments.txt` | Nhật ký từng lần thu tiền, người thu và thời điểm thu. |
| `database/inventory-adjustments.txt` | Nhật ký số sách hư hỏng/mất bị loại khỏi tồn kho. |
| `database/settings.txt` | Quy định mượn, phạt, tuổi và thời hạn thẻ hiện hành. |
| `App_Data/library.json.migrated.bak` | Bản sao dự phòng dữ liệu định dạng cũ sau khi ứng dụng chuyển dữ liệu sang các file mới. Có thể chứa dữ liệu nhạy cảm; chỉ giữ và sao lưu ở nơi phù hợp. |

## 11. Luồng hoạt động tổng quát

1. `Program.cs` đăng ký dịch vụ, cấu hình cookie/phân quyền và gọi `LibraryDataInitializer`.
2. `LibraryDataInitializer` bảo đảm các file dữ liệu tồn tại, sau đó khởi tạo tài khoản và dữ liệu mẫu cần thiết.
3. Controllers nhận yêu cầu, kiểm tra quyền/đầu vào và gọi `LibraryFileStore` hoặc `LibraryAccountService`.
4. `LibraryFileStore` đọc dữ liệu thành `LibraryData`; sau khi cập nhật, dữ liệu được ghi lại vào các file trong `database/`.
5. Controller trả về ViewModel; view Razor kết hợp dữ liệu đó thành HTML cho trình duyệt.
6. CSS và JavaScript trong `wwwroot/` định dạng giao diện và xử lý tương tác phía trình duyệt. Kiểm tra phía trình duyệt hỗ trợ trải nghiệm người dùng; kiểm tra phía server vẫn là nơi quyết định dữ liệu hợp lệ.
