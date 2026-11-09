# Tổng hợp API trong `src`



## Tổng quan

Trong `src` có **28 API nghiệp vụ**, được tính theo số action HTTP trong các controller:

| Service | Số API | Đường dẫn truy cập |
|---|---:|---|
| Trip Service | 21 | Qua Gateway: `/ts/...` |
| Matching & Pricing Service | 3 | Qua Gateway: `/mp/...` |
| Payment Service | 4 | Qua Gateway: `/pm/...` |
| **Tổng** | **28** | |

Ngoài ra, API Gateway có `GET /` trả về `Hello World!` để kiểm tra Gateway đang chạy. Endpoint này không được tính là API nghiệp vụ.

> Lưu ý: API đọc vị trí tài xế của Matching & Pricing Service hỗ trợ cả tiền tố `/driver` và `/drivers`. Vì vậy có **29 URL nghiệp vụ thực tế**, nhưng chỉ có **28 action API**. Các endpoint OpenAPI chỉ xuất hiện trong môi trường Development và không được tính.

## Quy ước

- `Công khai`: không yêu cầu JWT theo khai báo hiện tại.
- `JWT`: gửi access token qua header `Authorization: Bearer <token>`.
- `{...}`: tham số động trên URL.

## 1. Trip Service — 21 API

Base URL qua Gateway: `http://localhost:8000/ts`

### Xác thực và tài khoản

| # | Method | Endpoint | Quyền | Ý nghĩa |
|---:|---|---|---|---|
| 1 | `POST` | `/auth/login` | Công khai | Đăng nhập và nhận JWT cùng thông tin tài khoản. |
| 2 | `POST` | `/auth/register` | Công khai | Đăng ký tài khoản người dùng mới. |
| 3 | `POST` | `/auth/register-driver` | JWT | Đăng ký hồ sơ tài xế cho tài khoản hiện tại. |
| 4 | `POST` | `/auth/logout` | JWT | Thực hiện yêu cầu đăng xuất; hiện tại kiểm tra tài khoản và trả về `204`. |
| 5 | `GET` | `/auth/about-me` | JWT | Lấy thông tin tài khoản và hồ sơ tài xế hiện tại. |

### Chuyến đi

| # | Method | Endpoint | Quyền | Ý nghĩa |
|---:|---|---|---|---|
| 6 | `GET` | `/trips/history` | JWT | Lấy lịch sử chuyến đi của hành khách. |
| 7 | `GET` | `/trips/active` | JWT | Lấy chuyến đang hoạt động của hành khách hoặc tài xế. |
| 8 | `GET` | `/trips/{tripUuid}` | JWT | Lấy chi tiết một chuyến đi mà người dùng có quyền xem. |
| 9 | `POST` | `/trips` | JWT | Tạo chuyến, tính quãng đường, thời gian, giá và tuyến đường. |
| 10 | `POST` | `/trips/{tripUuid}/confirm` | JWT | Hành khách xác nhận chuyến và bắt đầu tìm tài xế. |
| 11 | `POST` | `/trips/{tripUuid}/cancel` | JWT | Hành khách hủy chuyến đang chờ tài xế. |
| 12 | `POST` | `/trips/{tripUuid}/driver-location` | JWT | Tài xế cập nhật vị trí trong chuyến được giao. |

### Tài xế

| # | Method | Endpoint | Quyền | Ý nghĩa |
|---:|---|---|---|---|
| 13 | `PATCH` | `/driver/{driverUuid}/online` | JWT | Cập nhật trạng thái online/offline của chính tài xế. |
| 14 | `POST` | `/driver/trip/{tripUuid}/accept` | JWT | Tài xế chấp nhận đề nghị chuyến đi. |
| 15 | `POST` | `/driver/trip/{tripUuid}/reject` | JWT | Tài xế từ chối đề nghị và hệ thống tìm tài xế tiếp theo. |
| 16 | `POST` | `/driver/trip/{tripUuid}/pickup` | JWT | Xác nhận đã đón hành khách. |
| 17 | `POST` | `/driver/trip/{tripUuid}/dropoff` | JWT | Xác nhận trả khách và phát sự kiện tạo thanh toán. |
| 18 | `GET` | `/drivers/notifications/stream` | JWT | Mở luồng SSE nhận đề nghị chuyến đi theo thời gian thực. |
| 19 | `GET` | `/driver/history` | JWT | Lấy lịch sử các chuyến đã trả khách của tài xế hiện tại. |

### Thanh toán

| # | Method | Endpoint | Quyền | Ý nghĩa |
|---:|---|---|---|---|
| 20 | `GET` | `/payments` | JWT | Lấy danh sách hóa đơn của hành khách hiện tại từ Payment Service. |
| 21 | `GET` | `/payments/driver` | JWT | Lấy danh sách hóa đơn và thu nhập của tài xế hiện tại. |

## 2. Matching & Pricing Service — 3 API

Base URL qua Gateway: `http://localhost:8000/mp`

| # | Method | Endpoint | Quyền | Ý nghĩa |
|---:|---|---|---|---|
| 1 | `POST` | `/matching` | Công khai | Tìm tài xế phù hợp ở gần điểm đón. |
| 2 | `POST` | `/quote` | Công khai | Tính quãng đường, thời gian, giá và hình học tuyến đường. |
| 3 | `GET` | `/driver/{driverUuid}/location` | Công khai | Lấy vị trí và trạng thái của tài xế. Cũng nhận `/drivers/{driverUuid}/location`. |

## 3. Payment Service — 4 API

Base URL qua Gateway: `http://localhost:8000/pm`

| # | Method | Endpoint | Quyền | Ý nghĩa |
|---:|---|---|---|---|
| 1 | `GET` | `/payment/{uuid}` | Công khai | Lấy chi tiết thanh toán theo UUID. |
| 2 | `GET` | `/payment/rider/{riderUuid}` | Công khai | Lấy danh sách thanh toán của hành khách. |
| 3 | `GET` | `/payment/driver/{driverUuid}` | Công khai | Lấy danh sách thanh toán của tài xế. |
| 4 | `POST` | `/payment/ipn` | Công khai | Nhận webhook SePay, xác nhận thanh toán và thông báo kết quả cho Trip Service. |

## 4. Endpoint kỹ thuật của API Gateway



| Method | Endpoint | Ý nghĩa |
|---|---|---|
| `GET` | `http://localhost:8000/` | Kiểm tra Gateway đang hoạt động; trả về `Hello World!`. |
