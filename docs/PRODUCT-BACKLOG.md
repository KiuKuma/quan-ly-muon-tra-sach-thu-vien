# Product Backlog – Quản lý mượn trả sách thư viện

**Product Owner:** Đặng Huy Quang Dũng  
**Cơ sở:** mục 1.5 đến 1.8 của đề cương và mã nguồn nhánh `develop` (commit `dcc0e3d`).  
**Phương pháp:** Scrum, mỗi Sprint 2 tuần (Sprint 0 chuẩn bị, Sprint 1 đến 4 tính từ tuần 1).

## 1. Quy ước

- **Vai trò trong User Story:** *Thủ thư* (trong code là vai trò `Nhân viên`), *Quản trị viên* (trong code là `Quản lý`, có toàn bộ quyền của Thủ thư), *Độc giả*.
- **Ưu tiên (MoSCoW):** M = bắt buộc, S = nên có, C = có thể có.
- **SP (Story Point):** ước lượng đề xuất theo dãy 1, 2, 3, 5, 8; nhóm điều chỉnh khi Sprint Planning.
- **Trạng thái:** `Xong` = đã có trong nhánh `develop`; `Một phần` = đã có nhưng chưa đủ so với đề cương; `Chưa` = chưa làm.
- **Phụ trách:** theo phân công mục 1.7 (QD = Quang Dũng, PA = Phương Anh, HT = Huy Thành, QA = Quỳnh Anh).

## 2. Backlog

### Epic 1. Nền tảng và chuẩn bị (Sprint 0)

| ID | User Story | Ưu tiên | SP | Sprint | Phụ trách | Trạng thái |
|---|---|---|---|---|---|---|
| PB-01 | Là nhóm phát triển, chúng tôi muốn có ERD chuẩn 3NF cho toàn hệ thống để mọi người hiểu chung mô hình dữ liệu | M | 5 | 0 | QD | Xong (`docs/ERD.md`) |
| PB-02 | Là nhóm phát triển, chúng tôi muốn có kho mã nguồn Git theo Gitflow và project nền để cùng làm việc | M | 3 | 0 | PA | Xong |
| PB-03 | Là nhóm phát triển, chúng tôi muốn có bản mẫu giao diện (Figma) cho Đăng nhập, Dashboard, Danh mục | M | 5 | 0 | HT | Chưa xác nhận |
| PB-04 | Là nhóm phát triển, chúng tôi muốn có Kế hoạch kiểm thử và Definition of Done để thống nhất tiêu chí hoàn thành | M | 3 | 0 | QA | Chưa xác nhận |

### Epic 2. Xác thực và phân quyền

| ID | User Story | Ưu tiên | SP | Sprint | Phụ trách | Trạng thái |
|---|---|---|---|---|---|---|
| PB-05 | Là Thủ thư hoặc Quản trị viên, tôi muốn đăng nhập bằng tên tài khoản và mật khẩu để sử dụng hệ thống | M | 3 | 1 | PA | Xong |
| PB-06 | Là chủ hệ thống, tôi muốn mật khẩu được mã hóa và tài khoản bị khóa tạm sau nhiều lần đăng nhập sai để tăng bảo mật | M | 3 | 1 | PA | Xong (khóa 15 phút sau 5 lần sai) |
| PB-07 | Là người dùng, tôi muốn chỉ thấy và dùng được các chức năng đúng vai trò của mình | M | 5 | 1 | PA | Xong cho Thủ thư và Quản trị viên |
| PB-08 | Là người dùng đã đăng nhập, tôi muốn xem và cập nhật hồ sơ cá nhân | S | 2 | 1 | PA | Xong |
| PB-09 | Là Quản trị viên, tôi muốn sửa hồ sơ, reset mật khẩu, khóa/mở và xóa tài khoản nhân viên | S | 5 | 1 | PA | Xong (xóa bị chặn nếu nhân viên có lịch sử mượn/trả; không tạo tài khoản tại màn này) |
| PB-10 | Là Độc giả, tôi muốn có tài khoản để đăng nhập và tra cứu sách | S | 5 | 1 hoặc 3 | PA | Chưa (code chưa có vai trò Độc giả và chưa có đăng ký) |

### Epic 3. Quản lý danh mục

| ID | User Story | Ưu tiên | SP | Sprint | Phụ trách | Trạng thái |
|---|---|---|---|---|---|---|
| PB-11 | Là Thủ thư, tôi muốn thêm/sửa sách và loại các bản hỏng/mất khỏi tồn kho có ghi lý do | M | 5 | 1 | QD | Xong (giữ đầu sách và lịch sử mượn; ghi nhật ký số bản bị loại) |
| PB-12 | Là Quản trị viên, tôi muốn thêm, sửa, xóa thể loại để phân loại sách | M | 3 | 1 | QD | Xong |
| PB-13 | Là Thủ thư, tôi muốn tạo/sửa hồ sơ và gia hạn thẻ độc giả; Quản trị viên có thêm quyền xóa | M | 5 | 1 | QD | Xong |
| PB-14 | Là Quản trị viên, tôi không muốn xóa nhầm sách, thể loại hoặc độc giả đang được phiếu mượn sử dụng | M | 2 | 1 | QD | Xong |
| PB-15 | Là Thủ thư, tôi muốn điều chỉnh sách hư hỏng/mất với nhật ký số lượng, lý do và người thao tác | S | 3 | 2 | PA, QD | Xong (điều chỉnh tồn sẵn có; phiếu trả lưu tình trạng riêng) |

### Epic 4. Mượn và trả sách

| ID | User Story | Ưu tiên | SP | Sprint | Phụ trách | Trạng thái |
|---|---|---|---|---|---|---|
| PB-16 | Là Thủ thư, tôi muốn lập phiếu mượn cho độc giả để ghi nhận việc mượn sách | M | 8 | 2 | PA | Một phần (mỗi phiếu hiện chọn một đầu sách) |
| PB-17 | Là Thủ thư, tôi muốn hệ thống tự trừ số lượng sách khi mượn và cộng lại khi trả | M | 3 | 2 | PA | Xong |
| PB-18 | Là Thủ thư, tôi muốn xác nhận độc giả trả sách để đóng phiếu mượn | M | 5 | 2 | PA | Một phần (chưa ghi ngày trả thực tế) |
| PB-19 | Là Thủ thư, tôi muốn hệ thống tự tính tiền phạt theo quy định và lập phiếu thu khi nhận tiền | M | 8 | 2 | PA | Xong (phạt trễ, hư hỏng/mất, có nhật ký thu) |
| PB-20 | Là Thủ thư, tôi muốn hệ thống kiểm tra thẻ, nợ quá hạn/phạt và hạn mức trước khi cho mượn | M | 5 | 2 | QD | Xong |
| PB-21 | Là Thủ thư, tôi muốn xem lịch sử mượn trả và công nợ của từng độc giả | S | 5 | 2 | QD | Xong |
| PB-22 | Là Thủ thư, tôi muốn có thông báo rõ ràng khi hết sách hoặc độc giả nợ phạt | S | 2 | 2 | HT | Chưa |
| PB-23 | Là Thủ thư, tôi muốn xem danh sách phiếu mượn kèm trạng thái Đang mượn, Quá hạn, Đã trả | M | 3 | 2 | PA | Xong |

### Epic 5. Tra cứu và thống kê

| ID | User Story | Ưu tiên | SP | Sprint | Phụ trách | Trạng thái |
|---|---|---|---|---|---|---|
| PB-24 | Là Thủ thư, tôi muốn tìm sách theo tên hoặc tác giả, tìm độc giả theo tên hoặc email | M | 3 | 1 | QD | Xong |
| PB-25 | Là Độc giả, tôi muốn tra cứu sách theo nhiều tiêu chí (tên, tác giả, thể loại, năm) để tìm đúng cuốn cần | M | 5 | 3 | QD | Chưa |
| PB-26 | Là Thủ thư, tôi muốn tìm kiếm không phân biệt dấu tiếng Việt để gõ nhanh hơn | C | 2 | 3 | QD | Chưa |
| PB-27 | Là Quản trị viên, tôi muốn xem tổng quan kho sách, độc giả và phiếu đang mượn/quá hạn | M | 3 | 1 | PA | Xong |
| PB-28 | Là Quản trị viên, tôi muốn xem số phiếu mượn/trả và tiền phạt đã thu theo từng tháng trong năm | S | 5 | 3 | QD | Xong |
| PB-29 | Là Quản trị viên, tôi muốn xem danh sách phiếu đang quá hạn, độc giả, sách chưa trả và nhân viên lập phiếu | S | 3 | 3 | QD | Xong |
| PB-30 | Là Quản trị viên, tôi muốn đối soát chi tiết phiếu mượn/trả, tiền phạt và sách bị loại khỏi kho theo ngày | S | 5 | 3 | QD, HT | Xong |
| PB-31 | Là nhóm phát triển, chúng tôi muốn tối ưu truy vấn và rà soát bảo mật toàn hệ thống | S | 5 | 3 | PA | Chưa |
| PB-37 | Là Quản trị viên, tôi muốn cấu hình thời hạn/số lượng mượn, mức phạt, độ tuổi và thời hạn thẻ | M | 5 | 2 | PA | Xong |
| PB-38 | Là Quản trị viên, tôi muốn xem xu hướng mượn/trả, trạng thái phiếu, sách được mượn nhiều và doanh thu tiền phạt theo khoảng ngày | S | 5 | 3 | QD | Xong |

### Epic 6. Chất lượng và bàn giao

| ID | User Story | Ưu tiên | SP | Sprint | Phụ trách | Trạng thái |
|---|---|---|---|---|---|---|
| PB-32 | Là nhóm phát triển, chúng tôi muốn có Test Case cho từng phân hệ và Bug Log để theo dõi chất lượng | M | 8 | 1 đến 3 | QA | Chưa xác nhận |
| PB-33 | Là nhóm phát triển, chúng tôi muốn kiểm thử hồi quy sau mỗi đợt thêm chức năng | S | 3 | 3 | QA | Chưa |
| PB-34 | Là khách hàng, chúng tôi muốn kiểm thử chấp nhận (UAT) và Biên bản nghiệm thu trước khi bàn giao | M | 5 | 4 | QA | Chưa |
| PB-35 | Là người dùng mới, tôi muốn có tài liệu Hướng dẫn sử dụng hệ thống | M | 5 | 3 đến 4 | HT | Chưa |
| PB-36 | Là nhóm phát triển, chúng tôi muốn đóng gói bản phát hành chính thức (Release) | M | 3 | 4 | QD, PA | Chưa |

## 3. Tổng hợp theo Sprint

| Sprint | Mục tiêu | Story chính |
|---|---|---|
| 0 | Chuẩn bị nền tảng | PB-01 đến PB-04 |
| 1 | Xác thực và quản lý danh mục | PB-05 đến PB-14, PB-24, PB-27 |
| 2 | Mượn trả và xử lý vi phạm | PB-15 đến PB-23 |
| 3 | Tra cứu và báo cáo thống kê | PB-25, PB-26, PB-28 đến PB-31, PB-33 |
| 4 | UAT, sửa lỗi, đóng gói | PB-34 đến PB-36 |

## 4. Điểm cần nhóm chốt (ảnh hưởng thứ tự ưu tiên)

1. **Vai trò Độc giả (PB-10, PB-25):** đề cương có phân quyền Độc giả và trang tra cứu cho Độc giả, nhưng code chỉ có Thủ thư và Quản trị viên. Cần chốt có làm đăng nhập cho Độc giả hay không, và làm ở Sprint nào.
2. **Quy định nghiệp vụ (PB-19, PB-20, PB-37):** đã chọn phạt trễ cố định theo ngày, mặc định 2.000đ/cuốn/ngày; cho phép mượn tiếp nếu không nợ phạt/quá hạn và còn dưới hạn mức 5 bản cùng lúc.
4. **Tên vai trò:** thống nhất giữa tài liệu (Thủ thư, Quản trị viên) và code (Nhân viên, Quản lý).
5. **Quyền sách:** Nhân viên được thêm/sửa sách và loại bản hỏng/mất khỏi tồn có nhật ký; chỉ Quản trị viên được quản lý thể loại và quy định.
6. Các mục ghi "Chưa xác nhận" (PB-03, PB-04, PB-32) là sản phẩm của Huy Thành và Quỳnh Anh, chưa thấy trong repo nên cần hai bạn xác nhận.
