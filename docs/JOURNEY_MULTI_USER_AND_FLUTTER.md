# Journey multi-user + Flutter — nghiệp vụ, API, SignalR, UI

Tài liệu gửi **team Flutter**: mô tả nghiệp vụ, màn hình cần làm, endpoint REST, hub SignalR (chỉ multi), payload và thứ tự gọi. Backend trả JSON **camelCase**.

**Base URL:** `{API_BASE}/api/...`  
**Hub SignalR:** `{API_BASE}/hubs/journey-live` — **chỉ luồng multi-user**; **solo không kết nối hub.**

> **Lưu ý:** Redis chạy **trên server** backend (multi: cache vị trí, scale SignalR). **Flutter không cần** cài Docker/Redis. **Solo** không gọi `live-location` / attendance / SignalR — chỉ REST map + checkpoint là đủ.

---

## 0. Hai luồng rõ rệt: **Solo (gốc)** và **Multi (mở rộng từ Solo)**

Backend và tài liệu này đi theo hướng: **multi-user được build thêm trên nền luồng solo cũ**, không thay thế luồng solo. Khi journey **chỉ có owner** tham gia (không ai join) → **chế độ Solo** (chỉ REST, không realtime nhóm). Khi có thêm member/guest active → **chế độ Multi** (thêm SignalR, attendance/tooltip, live-location, ràng buộc đích).

### 0.1 Luồng **Solo** — đúng như luồng cổ điển đã làm

Dành cho **một người** (owner) đi một mình.

**FE Solo — không làm các việc sau:** **không** kết nối SignalR / hub; **không** gọi `GET .../waypoints/attendance`; **không** vẽ tooltip **x/N** trên map; **không** gọi `live-location` / `live-locations` / không gửi GPS lên Redis cho nhóm. (Backend vẫn có các API đó nếu ai đó gọi nhầm; nghiệp vụ sản phẩm **solo không dùng**.)

**FE Solo — chỉ cần:**

1. **Start** — `POST .../start`.
2. **Bản đồ** — `GET .../polyline` (+ GPS nếu cần): vẽ tuyến qua waypoint tới đích.
3. **Waypoint** — check-in, check-out; **skip** (JWT); cập nhật UI **cục bộ** sau mỗi response (không chờ push).
4. **Hoàn tất** — `POST .../complete` khi user tới đích / muốn kết thúc (không ràng buộc “cả nhóm đích” — mục 1.3).

**Khẩn cấp trên solo:** có thể dùng `POST /api/emergency/nearby` **không** gửi `journeyId` (chỉ JWT + GPS) để vẽ tuyến **trên máy mình**; **không** cần `announce` / lắng nghe SignalR nhóm.

### 0.2 Luồng **Multi-user** — cùng nền Solo, **thêm** realtime + nhóm

Khi có **≥2** người **active** (owner + member/guest):

- **Giữ** toàn bộ bước REST như solo (start, polyline, check-in/out/skip).
- **Thêm bắt buộc cho trải nghiệm nhóm:**
  - **Tooltip x/N:** `GET .../waypoints/attendance` lúc vào màn; cập nhật qua SignalR **`waypointAttendanceUpdated`** (giảm poll).
  - **Vị trí mọi người:** `GET .../live-locations` + **`POST .../live-location`** định kỳ; kết nối hub **`JoinJourney`**, lắng nghe **`memberLocationUpdated`**.
  - **Đích & complete:** mọi người active xử lý đích; owner **complete** khi đủ điều kiện; SignalR **`destinationMemberArrived`**.
  - **Share / join / `guestKey`**, toolkit khẩn cấp **có `journeyId`** + SignalR **`EmergencyPlaceSelected`**, v.v.

**Tóm lại:** **Solo** = map + waypoint + complete, **không** SignalR, **không** attendance UI. **Multi** = bật thêm hub, live-location, attendance, đích nhóm — **một codebase**, hai chế độ.

---

## 1. Nghiệp vụ tổng quan

### 1.1 Vai trò

| Vai trò | Mô tả |
|---------|--------|
| **Owner** | User tạo journey. Sau `POST .../start` được ensure trong `journey_members` (`role = owner`). |
| **Member** | User đăng nhập join qua share code (`role = member`). |
| **Guest** | Không đăng nhập: `join-guest` → server trả `guestKey` (UUID). Client **bắt buộc lưu** `guestKey` + `journeyId`. |

### 1.2 Mục tiêu trải nghiệm (để FE thiết kế UI)

| Khu vực | Solo (chỉ owner) | Multi (≥2 người active) |
|---------|-------------------|-------------------------|
| **Map** | Polyline + waypoint của **mình**; **không** marker người khác; **không** hub. | Polyline; marker từng member; **live-location** + SignalR. |
| **Waypoint** | Check-in / out / skip; **không** tooltip x/N, **không** `GET attendance`. | Giống solo + **tooltip x/N** (`attendance` + SignalR `waypointAttendanceUpdated`). |
| **Đích & Complete** | `complete` khi user sẵn sàng (mục 1.3). **Không** SignalR đích. | Đích từng người; owner **complete** khi cả nhóm đã xử lý đích; **`destinationMemberArrived`**. |
| **Khẩn cấp** | Nearby **không** `journeyId` (cục bộ). **Không** `announce` / broadcast nhóm. | Nearby + `journeyId` + hub; list → **announce**; **`EmergencyPlaceSelected`**. |

### 1.3 Điều kiện hoàn tất journey — hai luồng (backend tự nhận)

Chỉ **owner** được `POST .../complete`.

**Luồng solo (đi một mình)** — *mặc định khi không ai khác tham gia*  
- Điều kiện: trên journey chỉ còn **đúng một** `journey_members` **active** và `role = owner` (hoặc chưa có dòng member — dữ liệu legacy: vẫn coi như solo).  
- **Không** áp dụng kiểm tra “cả nhóm đã tới đích”. Owner **complete** như trước: journey đã **start**, không **cancelled**, v.v.  
- **FE:** không SignalR, không attendance/tooltip, không live-location — xem mục 0.1.

**Luồng multi-user** — mọi trường hợp **không** được coi là solo ở trên (thường là **≥2** người **active**, hoặc edge case một người active mà **không** phải owner)  
- **Bắt buộc:** mọi `journey_members` **is_active = true** phải có progress **destination** với ít nhất một trong: **đã tới**, **đã rời đích**, hoặc **skip đích**.  
- Vi phạm → HTTP **400** với message về đích.  
- **Waypoint** vẫn không bắt buộc để complete.

*Ghi chú:* Khi mọi người **leave** hết và chỉ còn owner active → roster lại thành **solo** → complete không còn ràng buộc “cả nhóm đích”.

### 1.4 Rời nhóm (leave)

- Member: `POST .../leave` (Bearer). Guest: `leave-guest` + body `guestKey`.
- Người đã leave (**is_active = false**) **không** còn trong điều kiện complete.

### 1.5 Deep link / share

- `POST .../share` → `shareCode`, `sharePath`, **`shareLink`** (URL frontend mở đúng route join).
- Mở link **chỉ dẫn đường**; **join thật** vẫn là `POST join` hoặc `join-guest`.

---

## 2. Authentication — Flutter

### 2.1 User đăng nhập (member / owner)

- Mọi request cần auth: header `Authorization: Bearer <accessToken>`.

### 2.2 Guest

- Không Bearer cho các API `*-guest`, `join-guest`, `leave-guest`.
- Gửi **`guestKey`** trong body (hoặc query như bên dưới) theo từng endpoint.

### 2.3 SignalR + JWT (**chỉ multi-user**)

**Solo: bỏ qua mục này.** Chỉ khi app đang ở chế độ nhóm (đã join ≥2 người active):

- Kết nối hub: `wss://.../hubs/journey-live?access_token=<URL_ENCODED_JWT>` (nếu có JWT).
- Guest **không JWT**: vẫn connect hub; `JoinJourney` / `UpdateLocation` (nếu dùng) kèm **guestKey**.

---

## 3. REST API — bản đồ & vị trí realtime (**chỉ multi-user**)

Phần **3.1–3.3** dành cho **multi**: chia sẻ vị trí lên Redis và broadcast qua SignalR. **Solo không gọi** các endpoint `live-location` / `live-locations`.

### 3.1 Kiến trúc GPS (production — multi)

1. App lấy GPS định kỳ (~1–3 giây).
2. **`POST /api/journeys/{journeyId}/live-location`** — validate → **Redis** (TTL ~5 phút/key) → **SignalR** `MemberLocationUpdated`.
3. Các máy khác đã `JoinJourney` nhận event, cập nhật marker.

### 3.2 `POST /api/journeys/{journeyId}/live-location`

| | |
|--|--|
| Auth | `AllowAnonymous`: cần **Bearer** *hoặc* body có **`guestKey`**. |
| Body (JSON) | `latitude`, `longitude`, `accuracyMeters?`, `headingDegrees?`, `guestKey?` |
| Response | **204** nếu ok; **429 không có** — quá nhanh thì **204** luôn (server throttle ~750ms/member). |

**Lưu ý:** Journey phải **đã start**; caller phải là **member active** (JWT khớp hoặc `guestKey` khớp).

### 3.3 `GET /api/journeys/{journeyId}/live-locations?guestKey=`

- **Snapshot** vị trí mới nhất **tất cả member active** (từ Redis).
- Gọi **một lần** khi vào màn map / sau reconnect để “hydrate” marker trước khi chờ SignalR.
- Auth: Bearer *hoặc* query `guestKey`.

Response: `List<...>` cùng cấu trúc payload `MemberLocationUpdated` (xem mục 5).

### 3.4 `GET /api/journeys/{journeyId}/polyline`

- JWT **hoặc** query **`guestKey`** (đã join).
- Query: `latitude?`, `longitude?`, `excludeCompletedWaypoints` (mặc định true), `guestKey?`.
- Có `lat`+`lon`: polyline **từ vị trí hiện tại** tới waypoint kế / đích (theo **tiến độ của đúng user đó**). Không có: polyline **cả tuyến**.

### 3.5 `GET /api/journeys/{journeyId}/waypoints/attendance` (**chỉ multi-user**)

**Solo: không gọi API này, không UI tooltip x/N.**

- JWT **hoặc** `guestKey`.
- Trả: `activeMemberCount`, `waypoints[]`: `waypointId`, `stopOrder`, **`arrivedCount`**.
- **Multi:** tooltip x/N; tải ban đầu + cập nhật qua **SignalR** `WaypointAttendanceUpdated` (giảm poll).

---

## 4. REST API — checkpoint & lifecycle

### 4.1 Share & join

| Method | Path | Auth | Ghi chú |
|--------|------|------|---------|
| POST | `/api/journeys/{journeyId}/share` | Bearer | Response: `shareCode`, `shareLink`, … |
| GET | `/api/journeys/shared/{shareCode}` | Không | Preview public |
| POST | `/api/journeys/shared/{shareCode}/join` | Bearer | Member |
| POST | `/api/journeys/shared/{shareCode}/join-guest` | Không | Body: `displayName`, `guestKey?` |

### 4.2 Leave

| POST | `.../leave` | Bearer |
| POST | `.../leave-guest` | Body `{ "guestKey" }` |

### 4.3 Start / waypoint / destination / complete

| POST | Path | Ghi chú |
|------|------|---------|
| | `.../start` | Bearer owner |
| | `.../waypoints/{id}/checkin` | Bearer |
| | `.../waypoints/{id}/checkout` | Bearer |
| | `.../waypoints/{id}/skip?latitude=&longitude=` | Bearer; trả polyline tiếp |
| | `.../destination/checkin` | Bearer |
| | `.../destination/checkout` | Bearer |
| | `.../complete` | Bearer owner; 400 nếu thiếu người chưa xử lý đích |

### 4.4 Guest checkpoint

| POST | Path | Body |
|------|------|------|
| | `.../checkin-guest` | `guestKey`, … |
| | `.../checkout-guest` | `guestKey`, … |
| | `.../skip-guest` | `guestKey` |
| | `.../destination/checkin-guest` | `guestKey` |
| | `.../destination/checkout-guest` | `guestKey` |

**Guest skip:** response không kèm polyline — có thể **`GET polyline?guestKey=&latitude=&longitude=`** ngay sau đó để vẽ tuyến tiếp.

---

## 5. SignalR — hub `journey-live` (**chỉ multi-user**)

**Solo: không cài package hub, không mở WebSocket journey-live.**

### 5.1 Kết nối (Flutter — multi)

- Gói tham khảo: **`signalr_netcore`** (hoặc tương đương hỗ trợ .NET Core SignalR protocol).
- URL: `{API_BASE}/hubs/journey-live` + `?access_token=` nếu user có JWT.

### 5.2 Server → client (subscribe)

Đăng ký listener **camelCase** (theo JSON .NET):

| Event | Payload (tóm tắt) | UI gợi ý |
|-------|-------------------|----------|
| **`memberLocationUpdated`** | `journeyId`, `memberId`, `travelerId`, `guestKey`, `displayName`, `role`, `latitude`, `longitude`, `accuracyMeters`, `headingDegrees`, `atUtc` | Di chuyển / vẽ marker; bỏ qua nếu `memberId` là **chính mình** (tuỳ UX). |
| **`waypointAttendanceUpdated`** | Cùng shape **`GET .../waypoints/attendance`** | Cập nhật tooltip **x/N** toàn bộ waypoint, không cần gọi lại GET ngay. |
| **`destinationMemberArrived`** | `journeyId`, `memberId`, `displayName`, `role`, `isGuest`, `arrivedAt` | Toast / banner “Ai đó đã tới đích” (lần check-in đích **đầu tiên** của member đó). |
| **`emergencyPlaceSelected`** | Loại khẩn cấp / tên địa điểm / tọa độ / ai công bố (`announcedByTravelerId`, `announcedByGuestKey`, …) | Hiển thị trên map + tuyến tới điểm khẩn cấp (FE có thể dùng `routePolyline` từ API nearby nếu cần). |

### 5.3 Client → server (invoke)

| Method | Tham số | Ghi chú |
|--------|---------|---------|
| `JoinJourney` | `journeyId`, `guestKey?` | **Bắt buộc** sau connect để nhận broadcast. Journey phải **đã start**. |
| `LeaveJourney` | `journeyId` | Rời group. |
| `UpdateLocation` | `journeyId`, `latitude`, `longitude`, `guestKey?`, `accuracyMeters?`, `headingDegrees?` | **Tùy chọn / fallback** — cùng pipeline Redis + broadcast như REST. **Khuyến nghị production:** ưu tiên **`POST live-location`**. |

---

## 6. Toolkit khẩn cấp (`/api/emergency`)

### 6.1 `POST /api/emergency/nearby`

- Body: `type`, `latitude`, `longitude`, `journeyId?`, `guestKey?`, `vehicleType?`, `maxResults?`, …
- **Không có `journeyId`:** bắt buộc **đăng nhập** — dùng cho **solo** (chỉ vẽ trên máy, không broadcast nhóm).
- **Có `journeyId`:** Bearer **hoặc** `guestKey` — dùng cho **multi** (đã start → có thể SignalR **`EmergencyPlaceSelected`** nếu là loại tuyến gấp).
- **Loại “tuyến gấp”** (`hospital`, `pharmacy`, `gas_station`, `repair_shop`): mặc định **1** kết quả + polyline; với `journeyId` + đã start → broadcast (**multi** đang nghe hub).
- **Loại list** (`restaurant`, `lodging`, `coffee`): **multi** → chọn xong gọi **announce**; **solo** → chỉ hiển thị list + vẽ local, **không** gọi announce.

### 6.2 `POST /api/emergency/announce` (**chỉ multi-user**)

- Sau khi user chọn 1 địa điểm từ list (ăn/nghỉ/cà phê).
- Body: `journeyId`, `type`, `placeId`, `guestKey?` (+ Bearer nếu có).
- Server: Place Detail Goong → **`EmergencyPlaceSelected`** (ai đó đang trong hub nhóm mới thấy).

### 6.3 Gợi ý UI toolkit

- **Solo:** nearby **không** `journeyId` → 4 nút gấp / 3 loại list đều **cục bộ** trên map.
- **Multi:** có `journeyId` + hub; 4 nút gấp → vẫn có thể broadcast; 3 loại list → **announce** sau khi chọn.

---

## 7. Luồng Flutter đề xuất (thứ tự triển khai)

### 7.1 **Solo** (milestone 1)

1. Setup journey, **`POST .../start`**.
2. **`GET .../polyline`** (+ GPS) — vẽ map; waypoint check-in / out / skip — cập nhật UI **local**.
3. **`POST .../complete`** khi user kết thúc tại đích.
4. **Không:** hub, `attendance`, `live-location`, destination check-in bắt buộc (trừ khi sau này product đổi). Khẩn cấp: nearby **không** `journeyId`.

### 7.2 **Multi** (milestone 2 — bật khi có người join)

**Bước A — Join & state**

1. Deep link / `shareCode` → `GET shared/{code}` → `join` / `join-guest`.
2. Lưu `journeyId`, `guestKey?`, role.

**Bước B — Màn đang đi (multi)**

1. **`GET .../polyline`** (± `guestKey`, ± GPS).
2. **`GET .../live-locations`**, kết nối hub **`JoinJourney`**, listener **`memberLocationUpdated`**.
3. Timer GPS → **`POST .../live-location`** mỗi 1–3s.
4. **`GET .../waypoints/attendance`** + SignalR **`waypointAttendanceUpdated`** cho tooltip x/N.

**Bước C — Waypoint & đích**

- REST check-in/out/skip, destination check-in/out khi cần cho rule complete.
- Sau thao tác: có thể nhận **`waypointAttendanceUpdated`** / cập nhật local.

**Bước D — Owner complete**

- **Multi:** chỉ khi cả nhóm đã xử lý đích; **400** → hiển thị message.

**Bước E — Toolkit khẩn cấp (multi)**

- Nearby **`journeyId`** + hub; list → **`announce`**.

---

## 8. Lỗi thường gặp (FE xử lý UX)

| HTTP | Nguyên nhân gợi ý |
|------|-------------------|
| 401/404 checkpoint | Chưa start; `guestKey` sai; không còn active |
| 400 complete | **Multi:** thiếu member chưa xử lý đích (**Solo** không gặp rule này) |
| Hub **`HubException`** | **Multi:** chưa start; chưa `JoinJourney`; GPS invalid |

---

## 9. Tệp backend tham chiếu

- `JourneyController.cs` — polyline, attendance, live-location, lifecycle, guest.
- `EmergencyController.cs` — nearby, announce.
- `Hubs/JourneyLiveHub.cs` — Join / Leave / UpdateLocation.
- `JourneyProgressService.cs` — waypoint attendance broadcast, destination SignalR.
- `RedisJourneyLocationCache` — key GPS Redis (TTL).
- DTO: `UpdateLiveLocationRequest`, `JourneyMemberLocationNotification`, `JourneyWaypointAttendanceResponse`, `EmergencyNearbyRequest`, `EmergencyPlaceAnnounceRequest`, …

---

*Tài liệu đồng bộ backend: **Solo** = REST map + checkpoint, không SignalR/attendance/live-location trên FE; **Multi** = thêm hub, attendance, GPS nhóm, Redis. Codebase hiện tại.*
