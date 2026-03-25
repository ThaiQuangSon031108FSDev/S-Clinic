# Hướng Dẫn Cài Đặt Database & Chạy Data.sql

Khi mang source code sang máy mới hoặc cài đặt lại database từ đầu, bạn làm theo các bước dưới đây để đảm bảo **dữ liệu mẫu tiếng Việt không bị lỗi font** và schema (bảng) được cập nhật đồng bộ nhất.

---

## Bước 1: Khởi tạo Database & Schema
Bạn **không cần** chạy script tạo bảng thủ công. Entity Framework Core (EF Core) sẽ lo việc tạo bảng và khoá ngoại.

1. Mở Terminal (PowerShell hoặc CMD) trong thư mục `SClinic`.
2. Chạy lệnh sau để apply tất cả các migration vào database (kể cả tạo database nếu chưa có):
   ```bash
   dotnet ef database update
   ```

*Lưu ý: Đảm bảo chuỗi kết nối (Connection String) trong `appsettings.json` trỏ đúng vào SQL Server (LocalDB hoặc Server tùy cấu hình máy).*

---

## Bước 2: Chạy dữ liệu mẫu (`Data.sql`)
File `Sql/Data.sql` chứa lượng lớn dữ liệu mẫu để hệ thống hoạt động ngay (bệnh nhân, bác sĩ, lịch khám, dịch vụ, thuốc...).

⚠️ **QUAN TRỌNG: Không nên mở & chạy file này trực tiếp bằng SQL Server Management Studio (SSMS) bằng cách click đúp.** SSMS đôi khi nhận diện sai bảng mã file (đọc file UTF-8 thành Windows-1252), dẫn tới việc chữ tiếng Việt bị biến thành ký tự lạ (ví dụ: `á` thành `Ã¡`).

### Cách chạy đúng (không bị lỗi font):
Mở Terminal trong thư mục `SClinic` và dùng công cụ `sqlcmd` (được cài sẵn cùng SQL Server) bằng lệnh:

```bash
sqlcmd -S . -d SClinicDb -i Sql\Data.sql -f 65001
```

**Giải thích thông số:**
- `-S .` : Tên SQL Server (Dấu `.` đại diện cho localhost/máy trạm mặc định). Nếu máy khác xài `.\SQLEXPRESS`, hãy đổi lại.
- `-d SClinicDb` : Tên cơ sở dữ liệu (phải khớp với tên tạo ở Bước 1).
- `-i Sql\Data.sql` : Đường dẫn tới file script.
- `-f 65001` : Ép hệ thống dùng chuẩn **UTF-8** để đọc file, giúp giữ nguyên font chữ tiếng Việt.

---

## Bước 3: Sửa lỗi font chữ dự phòng (Nếu Bước 2 vẫn lỗi)
Nếu vì lý do gì đó máy của bạn vẫn bị lỗi font sau khi đổ dữ liệu, hệ thống S-Clinic có xây dựng riêng một API nội bộ để tự động dò tìm và "sửa lỗi font" (ghi đè string chuẩn C# vào SQL).

1. Bạn vẫn khởi chạy ứng dụng bình thường (`dotnet run`).
2. Mở trình duyệt và truy cập vào đường dẫn:
   ```
   http://localhost:5071/dev/fix-encoding
   ```
3. Bạn sẽ thấy dòng chữ "Hoàn tất! Reload lại các trang để kiểm tra font chữ". Tất cả dữ liệu tiếng Việt (Bác sĩ, Bệnh nhân, Tên gói, Tên Dịch vụ, Tên thuốc) đã được sửa lại hoàn hảo.

---

## Thông tin đăng nhập Test
Sau khi hoàn tất, bạn có thể đăng nhập bằng các tài khoản sau (Mật khẩu chung: **`Sclinic@123`**):

- **Admin (Quản lý):** `admin@sclinic.vn`
- **Bác sĩ:** `bs.lethib@sclinic.vn`
- **Lễ tân:** `letan1@sclinic.vn`
- **Thu ngân:** `thungan1@sclinic.vn`
- **Bệnh nhân:** `thaiquangson@gmail.com`
