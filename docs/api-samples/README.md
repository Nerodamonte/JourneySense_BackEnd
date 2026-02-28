# Mẫu test API – JourneySense Backend

Thư mục này chứa body JSON mẫu và file `.http` để test nhanh các API.

## Cấu trúc

```
api-samples/
├── README.md                 # File này
├── api-tests.http            # Gọi API trực tiếp (REST Client / Thunder Client)
├── auth/                     # Đăng ký, đăng nhập, OTP
├── payment/                  # Thanh toán PayOS
├── journeys/                 # Thiết lập hành trình
├── experiences/              # Ghé thăm + đánh giá
└── micro-experiences/        # CRUD micro-experience
```

## Cách dùng

### 1. File `.http` (khuyến nghị)

- **VS Code**: cài extension [REST Client](https://marketplace.visualstudio.com/items?itemName=humao.rest-client), mở `api-tests.http`, click "Send Request" trên từng block.
- **Thunder Client**: Import hoặc copy từng request vào Thunder Client.

Mở `api-tests.http`, sửa biến `@baseUrl` (ví dụ `https://localhost:7001`), sau khi login lấy `token` và gán vào `@token` cho các API cần xác thực.

### 2. Copy body từ file JSON

Mỗi thư mục con có file JSON tương ứng endpoint. Dùng làm body khi gọi bằng Postman, curl, hoặc client bất kỳ.

**Ví dụ curl:**

```bash
# Login
curl -X POST https://localhost:7001/api/auth/login \
  -H "Content-Type: application/json" \
  -d @docs/api-samples/auth/login.json

# Tạo thanh toán
curl -X POST https://localhost:7001/api/Payment/create \
  -H "Content-Type: application/json" \
  -d @docs/api-samples/payment/create-payment.json

# Thiết lập hành trình
curl -X POST https://localhost:7001/api/journeys/setup \
  -H "Content-Type: application/json" \
  -d @docs/api-samples/journeys/setup.json
```

## Danh sách endpoint và file mẫu

| Nhóm | Method | Endpoint | File body mẫu |
|------|--------|----------|----------------|
| Auth | POST | `/api/auth/login` | `auth/login.json` |
| Auth | POST | `/api/auth/register/send-otp` | `auth/register-send-otp.json` |
| Auth | POST | `/api/auth/register/resend-otp` | `auth/register-send-otp.json` |
| Auth | POST | `/api/auth/register/verify-otp` | `auth/register-verify-otp.json` |
| Auth | POST | `/api/auth/register/set-password` | `auth/register-set-password.json` (header: `Authorization: Bearer <registerToken>`) |
| Payment | POST | `/api/Payment/create` | `payment/create-payment.json` |
| Payment | GET | `/api/Payment/link/{paymentLinkId}` | — |
| Payment | POST | `/api/Payment/link/{paymentLinkId}/cancel` | — (query: `?reason=...`) |
| Journeys | POST | `/api/journeys/setup` | `journeys/setup.json` |
| Experiences | POST | `/api/experiences/visit` | `experiences/visit-feedback.json` (cần Bearer token) |
| Micro-exp | GET | `/api/micro-experiences` | — (query: `keyword`, `categoryId`, `status`) |
| Micro-exp | GET | `/api/micro-experiences/{id}` | — |
| Micro-exp | POST | `/api/micro-experiences` | `micro-experiences/create.json` |
| Micro-exp | PUT | `/api/micro-experiences/{id}` | `micro-experiences/update.json` |
| Micro-exp | DELETE | `/api/micro-experiences/{id}` | — |

## Giá trị enum (số trong JSON)

### VehicleType (journeys/setup)

- `0` = Walking  
- `1` = Bicycle  
- `2` = Motorbike  
- `3` = Car  

### ExperienceStatus (micro-experiences update)

- `0` = ActiveUnverified  
- `1` = Verified  
- `2` = Featured  
- `3` = NeedsUpdate  
- `4` = Inactive  
- `5` = Rejected  

## Lưu ý khi test

1. **Auth**: Sau `verify-otp` nhận `registerToken` → dùng làm `Authorization: Bearer <registerToken>` khi gọi `set-password`. Sau khi đăng ký xong dùng `login` để lấy token đăng nhập.
2. **Experiences/visit**: Cần token đăng nhập; `experienceId` và `journeyId` phải tồn tại trong DB.
3. **Micro-experiences**: `categoryId` phải là ID danh mục có trong DB. Nếu chưa có category, tạo hoặc dùng ID từ dữ liệu seed.
4. **Payment**: `totalAmount` (VNĐ); `items[].price` * `quantity` nên khớp với `totalAmount` theo logic nghiệp vụ.
5. **Base URL**: Khi chạy local, kiểm tra port trong `Properties/launchSettings.json` (thường `https://localhost:7xxx`).
