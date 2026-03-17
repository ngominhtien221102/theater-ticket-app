# Theater Ticketing App (VB.NET WinForms + PostgreSQL)

Ứng dụng desktop quản lý bán vé nhà hát, viết bằng VB.NET WinForms, dữ liệu lưu trên PostgreSQL.

## 1. Checklist bàn giao

- Mã nguồn đầy đủ: thư mục `TheaterTicketingApp/`
- File SQL tạo bảng: `init.sql`
- File hướng dẫn vận hành: `README.md` (file này)
- Ảnh chụp màn hình ứng dụng:
  - Có thể đính kèm tại `docs/screenshots/` (nếu có)

## 2. Công nghệ và kiến trúc

- UI: WinForms (code-first, không dùng Designer kéo thả)
- Runtime mục tiêu: `net8.0-windows`
- Data access: Entity Framework Core + Npgsql
- Database: PostgreSQL
- Kiến trúc chính:
  - `Forms/`: giao diện và xử lý sự kiện
  - `Repositories/`: nghiệp vụ + truy cập dữ liệu
  - `Data/`: `DbContext`, kết nối DB, vá schema cũ
  - `Services/`: job cập nhật trạng thái suất diễn

## 3. Yêu cầu môi trường

- Windows 10/11
- Visual Studio 2022/2026:
  - Workload `.NET desktop development`
  - Có .NET 8 target/runtime để chạy project `net8.0-windows`
- Docker Desktop (khuyến nghị, để dựng PostgreSQL nhanh)

## 4. Khởi tạo PostgreSQL

### 4.1 Cách khuyến nghị: Docker Compose

Chạy tại thư mục gốc dự án (thư mục chứa `docker-compose.yml`):

```powershell
docker compose up -d
docker compose ps
```

Thông số mặc định từ `docker-compose.yml`:

- Host: `localhost`
- Port: `5432`
- Database: `theater_ticketing`
- Username: `postgres`
- Password: `postgres`

Dừng DB:

```powershell
docker compose down
```

Lưu ý:

- `init.sql` chỉ tự chạy khi volume DB mới được tạo.
- Nếu đã có volume cũ, script khởi tạo có thể không chạy lại.

### 4.2 Cách thủ công (nếu không dùng Docker)

1. Tạo database `theater_ticketing` trên PostgreSQL.
2. Chạy script `init.sql`.
3. Đảm bảo user ứng dụng có quyền CRUD các bảng.

Ví dụ (nếu có `psql`):

```powershell
psql -h localhost -U postgres -d theater_ticketing -f init.sql
```

## 5. Cấu hình kết nối PostgreSQL

Ứng dụng ưu tiên đọc biến môi trường:

- `THEATER_DB_CONNECTION`

Ví dụ:

```powershell
$env:THEATER_DB_CONNECTION="Host=localhost;Port=5432;Database=theater_ticketing;Username=postgres;Password=postgres"
```

Nếu không set biến môi trường, app fallback về `DefaultConnectionString` trong `TheaterTicketingApp/Data/Database.vb`.

Khuyến nghị bàn giao:

- Luôn set `THEATER_DB_CONNECTION` để không phụ thuộc fallback theo máy phát triển.

## 6. Cách chạy chương trình

1. Mở `TheaterTicketingApp.slnx` (VS 2026) hoặc `TheaterTicketingApp.sln`.
2. Restore NuGet packages.
3. Đảm bảo PostgreSQL đã chạy.
4. Set `THEATER_DB_CONNECTION`.
5. Chạy `F5`.

Kiểm tra nhanh bằng CLI:

```powershell
dotnet build TheaterTicketingApp.sln
```

## 7. Rà soát logic nghiệp vụ (trạng thái hiện tại)

### 7.1 Màn hình chính

- Có 4 chức năng:
  - Quản lý suất diễn
  - Đặt vé
  - Gán ghế
  - Báo cáo suất diễn đã kết thúc

### 7.2 Quản lý suất diễn (`frmPerformanceMaster`)

- CRUD suất diễn.
- Tìm kiếm theo tên vở diễn và khoảng thời gian.
- Khi thêm mới:
  - Giờ bắt đầu phải >= hiện tại + 2 giờ.
- Khi cập nhật:
  - Nếu suất diễn đã có booking thì không cho sửa:
    - `start_time`
    - `duration_minutes`
    - `ticket_price`
- Trạng thái suất diễn:
  - `NOT_STARTED`
  - `IN_PROGRESS`
  - `ENDED`

### 7.3 Đặt vé (`frmBooking`)

- Chọn suất diễn theo danh sách (có ô tìm tên vở diễn).
- Tính tiền theo `ticket_price` của suất diễn.
- Không cho đặt nếu suất diễn đã kết thúc.
- Trước khi tạo booking, kiểm tra số ghế còn lại.
- Nếu hết ghế hoặc không đủ ghế theo `ticket_qty` thì chặn tạo booking.

### 7.4 Gán ghế (`frmSeatAssignment`)

- Sơ đồ ghế cố định 10x10: `A1..J10` (100 ghế).
- Màu:
  - Trắng: ghế trống
  - Xanh: ghế đang chọn cho booking hiện tại
  - Đỏ: ghế đã thuộc booking khác
- Số ghế chọn phải đúng bằng `ticket_qty` của booking.
- Lưu gán ghế trong transaction + ràng buộc unique chống trùng ghế.
- Nếu suất diễn đã kết thúc:
  - Không cho chỉnh sửa gán ghế.
- Hủy booking:
  - Chỉ hủy được trước giờ diễn ít nhất 24 giờ.

### 7.5 Báo cáo (`frmEndedPerformanceReport`)

- Chỉ hiển thị các suất diễn đã kết thúc.
- Mỗi suất diễn hiển thị:
  - Số ghế đã booking / tổng ghế
  - Doanh thu
- Có dòng tổng hợp:
  - Tổng suất đã kết thúc
  - Tổng ghế đã booking
  - Tổng doanh thu

### 7.6 Job cập nhật trạng thái suất diễn

- `PerformanceStatusJob` chạy mỗi 5 phút.
- Tự động đồng bộ trạng thái theo thời gian thực.
- Một số màn hình cũng gọi đồng bộ trạng thái khi load dữ liệu.

## 8. Cấu trúc database (`init.sql`)

### 8.1 Bảng `performances`

- Thông tin suất diễn + trạng thái + giá vé.
- Check constraint:
  - `duration_minutes > 0`
  - `ticket_price > 0`
  - `status IN ('NOT_STARTED','IN_PROGRESS','ENDED')`

### 8.2 Bảng `bookings`

- Thông tin đặt vé theo suất diễn.
- Ràng buộc hiện tại:
  - `seat_type = 'REGULAR'`

### 8.3 Bảng `seat_assignments`

- Ghế theo booking.
- Unique theo suất diễn:
  - `(performance_id, seat_row, seat_number)`
- Trigger đảm bảo `seat_assignments.performance_id` khớp booking.

### 8.4 View báo cáo bonus

- `v_performance_seat_type_summary`

## 9. Giả định và giới hạn hiện tại

- Mô hình ghế cố định 100 ghế (10 hàng x 10 cột).
- Chỉ có 1 loại ghế logic: `REGULAR`.
- Chưa có đăng nhập/phân quyền.
- Chưa có cơ chế giữ ghế tạm thời theo timeout.
- Chưa kiểm tra xung đột lịch giữa các suất diễn.
- Validation một số rule nằm ở tầng UI (form), không phải tất cả đều ở repository.
- Không có bộ test tự động; hiện kiểm tra chủ yếu bằng build + chạy tay.
- `EnsureSchema()` chỉ vá schema cũ (thêm cột/ràng buộc), không thay thế hoàn toàn script tạo DB từ đầu.

## 10. Ảnh chụp màn hình ứng dụng

Hiện tại repository chưa kèm ảnh.

Khi bàn giao, có thể thêm ảnh vào `docs/screenshots/` theo gợi ý:

- `main-form.png`
- `performance-master.png`
- `booking.png`
- `seat-assignment.png`
- `ended-performance-report.png`

## 11. Sự cố thường gặp

- Không kết nối được DB:
  - Kiểm tra `docker compose ps` phải thấy container `healthy`.
  - Kiểm tra lại `THEATER_DB_CONNECTION`.
  - Kiểm tra cổng `5432` có bị chiếm không.
- Build lỗi file `.exe` bị lock:
  - Đóng app đang chạy, rồi build lại.
- Mở solution bị incompatible:
  - Mở `TheaterTicketingApp.slnx` hoặc mở trực tiếp `TheaterTicketingApp/TheaterTicketingApp.vbproj`.
