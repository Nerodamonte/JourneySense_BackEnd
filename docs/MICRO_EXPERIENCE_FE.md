# Micro Experience — hướng dẫn cho Frontend

Base path: **`/api/micro-experiences`** (nối sau URL API, vd. `http://localhost:5062`).

**Auth:** `GET` danh sách và chi tiết **không bắt buộc** JWT. Tạo / sửa / xoá experience và **quản lý ảnh** cần JWT role **`staff`**:  
`Authorization: Bearer <accessToken>`.

---

## 1. Danh sách — `GET /api/micro-experiences`

**Query (tùy chọn):** `keyword`, `categoryId`, `status`, `mood`, `timeOfDay`

**Response:** `MicroExperienceListItemResponse[]`

| Field | Mô tả |
|--------|--------|
| `id` | `guid` |
| `name`, `city`, `status` | string |
| `preferredTimes` | `string[]` |
| `latitude`, `longitude` | number \| null (WGS84) |
| **`coverPhotoUrl`** | string \| null — ảnh bìa hoặc ảnh đầu tiên; dùng làm thumbnail list |

**Hiển thị ảnh:** nếu `coverPhotoUrl` bắt đầu bằng `/`, nối với base URL API:  
`<API_BASE>` + `coverPhotoUrl` (vd. `http://localhost:5062/uploads/experiences/...`).

---

## 2. Chi tiết — `GET /api/micro-experiences/{id}`

**Response:** `MicroExperienceDetailResponse`

Các field chính: `id`, `categoryId`, `name`, `categoryName`, `richDescription`, `avgRating`, `qualityScore`, `status`, địa chỉ, `accessibleBy`, `preferredTimes`, `weatherSuitability`, `seasonality`, `amenityTags`, `tags`, `openingHours`, `priceRange`, `crowdLevel`, `latitude`, `longitude`, …

**`photos`:** `ExperiencePhotoResponse[]` \| null

Mỗi phần tử:

| Field | Mô tả |
|--------|--------|
| `id` | `guid` |
| `photoUrl` | string — URL đầy đủ hoặc đường dẫn tương đối `/uploads/...` |
| `thumbnailUrl` | string \| null |
| `caption` | string \| null |
| `isCover` | boolean |
| `uploadedAt` | ISO date \| null |

Sắp xếp: cover trước, sau đó theo `uploadedAt`.

---

## 3. Tạo experience — `POST /api/micro-experiences`

- **Header:** `Authorization: Bearer …`, `Content-Type: application/json`
- **Role:** `staff`

Body tối thiểu gồm `name`, `categoryId`, `accessibleBy`, và các field địa chỉ như backend đang validate (xem Swagger / `CreateMicroExperienceRequest`).

**Vị trí (WGS84):**

- Gửi **`latitude`** và **`longitude`** cùng lúc (số thực, `latitude` −90…90, `longitude` −180…180) → backend **gắn đúng điểm đó**, không cần geocode từ địa chỉ.
- **Bỏ trống cả hai** → backend geocode từ `address` / `city` / `country` (Goong) như cũ.
- **Chỉ gửi một trong hai** → `400` validation.

**Ảnh kèm lúc tạo (tuỳ chọn):** thêm mảng **`photos`** — mỗi phần tử:

```json
{
  "photoUrl": "https://cdn.example.com/a.jpg",
  "thumbnailUrl": null,
  "caption": "Mặt tiền",
  "isCover": true
}
```

- `photoUrl` bắt buộc (chuỗi ≤ 500 ký tự).
- `isCover: true`: gỡ cờ cover của các ảnh khác **cùng** experience (sau khi đã có id).

**Response:** `201` + `MicroExperienceDetailResponse` (trong đó có `photos` đã lưu).

**Lỗi thường gặp:** `400` — category không tồn tại, slug trùng, validation tọa độ, hoặc không geocode được khi không gửi `latitude`/`longitude`.

---

## 4. Cập nhật experience — `PUT /api/micro-experiences/{id}`

- **Role:** `staff`, JSON body như `UpdateMicroExperienceRequest`.

**Vị trí:** cùng quy tắc **cặp `latitude` + `longitude`** hoặc geocode từ địa chỉ như mục 3.

**Ảnh:** nếu gửi **`photos`**, backend **chỉ append** thêm bản ghi mới (không xoá ảnh cũ). Muốn xoá một ảnh → dùng API xoá ảnh riêng (mục 6).

---

## 5. Upload file ảnh — `POST /api/micro-experiences/{id}/photos`

Dùng khi staff chọn file từ máy / mobile, không chỉ URL.

- **Header:** `Authorization: Bearer …`
- **Content-Type:** `multipart/form-data`
- **Role:** `staff`

**Form fields:**

| Field | Bắt buộc | Mô tả |
|--------|-----------|--------|
| `file` | Có | File ảnh |
| `caption` | Không | string |
| `isCover` | Không | boolean (mặc định false; gửi `true`/`false` dạng text form) |

**Giới hạn:** tối đa **10 MB**; MIME: **JPEG, PNG, WebP, GIF**.

**Response `200`:** một object `ExperiencePhotoResponse` (có `id`, `photoUrl` dạng **`/uploads/experiences/{experienceGuid}/{fileGuid}.ext`**, …).

**Hiển thị:** `<API_BASE>` + `photoUrl`.

**Lỗi:** `400` (file sai / quá lớn / MIME không hỗ trợ), `404` (không có experience `id`).

---

## 6. Xoá một ảnh — `DELETE /api/micro-experiences/{id}/photos/{photoId}`

- **Role:** `staff`
- **Response:** `204` nếu thành công; `404` nếu không tìm thấy ảnh.

File lưu cục bộ dưới `wwwroot/uploads/...` sẽ được xoá khi có thể.

---

## 7. Xoá cả experience — `DELETE /api/micro-experiences/{id}`

**Role:** `staff` — `204` / `404`.

(Lưu ý: tuỳ DB/CASCADE, ảnh liên quan có thể bị xoá theo; file trên disk có thể còn sót nếu chưa dọn — ưu tiên xoá ảnh qua API mục 6 trước khi xoá experience nếu cần kiểm soát.)

---

## 8. Gợi ý tích hợp UI

1. **List:** cột ảnh dùng `coverPhotoUrl` + fallback placeholder nếu null.
2. **Detail / chỉnh sửa:** carousel hoặc grid từ `photos`; đánh dấu `isCover`.
3. **Thêm ảnh:**  
   - URL có sẵn → gửi trong `PUT` / kèm `POST` create qua `photos[]`.  
   - File → `POST .../{id}/photos` với `FormData`, sau đó có thể `GET` lại chi tiết để đồng bộ state.
4. **Base URL ảnh:** cấu hình một biến `VITE_API_BASE_URL` (hoặc tương đương); mọi `photoUrl` bắt đầu bằng `/` đều nối với host API.

---

## 9. Swagger

Mở `/swagger` trên môi trường dev để xem schema đầy đủ và thử `multipart` cho upload ảnh.
