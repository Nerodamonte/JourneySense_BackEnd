# Website Admin & Staff — API backend & Frontend (React + TypeScript + Vite + Redux)

Tài liệu **chuẩn hoá** cho portal JourneySense: backend, **CORS**, **JWT**, danh sách API, và **React** với **Redux cổ điển** (action types, action creators, reducers, `combineReducers`, `createStore`, `redux-thunk`).

**Base URL API:** cấu hình ở frontend qua biến môi trường (vd. `http://localhost:5062`). Mọi path trong bảng là tương đối (bắt đầu sau base).

**Auth:** `POST /api/auth/login` → nhận `accessToken`, `refreshToken`, `userId`, `email`, `role`. Gửi kèm request: `Authorization: Bearer <accessToken>`. Claim role trong JWT khớp `users.role` (**chữ thường:** `admin` | `staff` | `traveler`).

**CORS (backend):** `JSEA.Presentation/appsettings.json` → `Cors:AllowedOrigins` (mặc định `http://localhost:5173`). Thêm origin production khi deploy.

**DB:** cột `feedbacks.moderation_status` — migration `20260326120000_AddFeedbackModerationStatus` hoặc SQL tương đương. Cột `journeys.journey_feedback_moderation_status` — migration `20260330120000_AddJourneyFeedbackModerationStatus` (mặc định `approved`; text journey feedback mới thường `pending` sau khi user lưu). **Khẩn cấp** không còn bảng: dùng **Goong Places** realtime (`POST /api/emergency/nearby`). Migration `20260329120000_DropEmergencyPlacesTable` xoá bảng `emergency_places` nếu từng tạo; có thể bỏ qua trên DB mới.

**Test admin:** `UPDATE users SET role = 'admin' WHERE email = '...';` rồi login.

---

## Bảng API — thứ tự test (Swagger / Postman)

| # | Method | Path | Role JWT |
|---|--------|------|----------|
| 1 | `GET` | `/api/admin/users` | `admin` |
| 2 | `GET` | `/api/admin/users/{userId}` | `admin` |
| 3 | `PATCH` | `/api/admin/users/{userId}/status` | `admin` — body `{ "status": "active"\|"suspended", "reason"?: "..." }` |
| 4 | `POST` | `/api/admin/staff-accounts` | `admin` — `{ "email", "password" }` |
| 5 | `GET` | `/api/admin/analytics/summary` | `admin` |
| 6 | `GET` | `/api/admin/audit-logs` | `admin` — query `userId`, `actionType`, `entityType`, `fromUtc`, `toUtc`, `page`, `pageSize` |
| 7 | `GET` | `/api/staff/feedbacks/journeys` | `admin`, `staff` — **feedback cả chuyến** (`journeys.journey_feedback`); query `moderationStatus`, `page`, `pageSize` |
| 8 | `POST` | `/api/staff/feedbacks/journeys/{journeyId}/moderate` | `admin`, `staff` — cùng body như duyệt waypoint: `{ "decision": "approve"\|"reject", "reason"?: "..." }` → `204` |
| 9 | `GET` | `/api/staff/feedbacks` | `admin`, `staff` — **feedback waypoint** (bảng `feedbacks`); query `moderationStatus`, `experienceId`, `page`, `pageSize` |
| 10 | `GET` | `/api/staff/feedbacks/{feedbackId}` | `admin`, `staff` |
| 11 | `POST` | `/api/staff/feedbacks/{feedbackId}/moderate` | `admin`, `staff` — `{ "decision": "approve"\|"reject", "reason"?: "..." }` |
| 12 | `POST` | `/api/staff/reports/users/{userId}` | `admin`, `staff` — `{ "reason", "relatedFeedbackId"?: guid }` |
| 13 | `POST` | `/api/admin/embeddings/generate` | `admin` |
| — | `POST` | `/api/auth/login` | Không — `{ "email", "password" }` |
| — | `GET` | `/api/profile` | **`admin`**, **`staff`**, **`traveler`** — JWT; xem mục **Profile** bên dưới |
| — | `PUT` | `/api/profile` | **`admin`**, **`staff`**, **`traveler`** — cập nhật họ tên, ảnh, bio, SĐT… |
| — | `GET` | `/api/micro-experiences` | **Không bắt buộc** JWT |
| — | `POST` / `PUT` / `DELETE` | `/api/micro-experiences` | Chỉ **`staff`** |
| — | `POST` | `/api/emergency/nearby` | **`traveler`** — `{ "type", "latitude", "longitude", "radiusMeters"?, "maxResults"?, "vehicleType"?, "placeKeyword"? }` |

**Login response (JSON):** `userId`, `email`, `role`, `accessToken`, `refreshToken`.

**Feedback waypoint:** mới từ mobile → `feedbacks.moderation_status = pending`; RAG/`SuggestService` chỉ dùng feedback **`approved`**; staff duyệt qua API **(11)**.

**Feedback cả chuyến:** text trong `journeys.journey_feedback` có `journey_feedback_moderation_status` (`pending` \| `approved` \| `rejected`). Web staff xem hàng chờ **(7)** và duyệt **(8)**. (Khác với danh sách **(9)** dành cho từng waypoint.)

**Journey + feedback lồng nhau (mobile / chi tiết chuyến):** `GET /api/journeys/{id}` (JWT traveler chủ chuyến) trả `journeyFeedback`, `journeyFeedbackModerationStatus`, và trong `waypoints[]` có `visitFeedback` khi đã check-in. Staff xem một bản ghi waypoint: `GET /api/staff/feedbacks/{id}` kèm `journeyId`, `journeyFeedback`, `journeyFeedbackModerationStatus`, `waypointStopOrder`.

**Mobile — chi tiết gợi ý (không thuộc portal web):** `GET /api/journeys/suggestions/{suggestionId}/community` — metrics địa điểm + feedback công khai đã duyệt.

---

## Profile cá nhân (`/api/profile`) — làm màn hồ sơ admin & staff

Cùng endpoint với mobile traveler; backend **phân biệt theo `users.role`** trong JWT.

### `GET /api/profile`

Header: `Authorization: Bearer <accessToken>`.

**Luôn có trong JSON:** `userId`, `role` (`admin` \| `staff` \| `traveler`), `email`, `phone`, `fullName`, `avatarUrl`, `bio`, `accessibilityNeeds` (các field có thể `null` tùy dữ liệu).

**Chỉ `traveler`:** thêm `travelStyle` (mảng vibe) và `point` (điểm thưởng, số nguyên).

**`admin` và `staff`:** **không** trả hai field `travelStyle` và `point` (JSON bỏ hẳn property, không phải `null`), vì portal không dùng travel vibe / điểm thưởng.

### `PUT /api/profile`

Body (tất cả optional trừ quy tắc traveler bên dưới): `fullName`, `phone`, `avatarUrl`, `bio`, `accessibilityNeeds`, `travelStyle` (mảng enum).

- **`admin` / `staff`:** có thể đổi họ tên, avatar, bio, SĐT… **Không** bắt chọn travel style lần đầu. Nếu client gửi `travelStyle`, backend **bỏ qua** (không lưu, không gọi Gemini).
- **`traveler`:** như tài liệu mobile — nếu trong DB chưa có travel style thì lần đầu **bắt buộc** gửi ít nhất một vibe; các lần sau `travelStyle` optional.

### Gợi ý FE portal (React)

- Sau login đã có `role` trong response; có thể gọi `GET /api/profile` để đồng bộ đầy đủ + cập nhật Redux.
- Trang **Profile** admin/staff: **không** hiển thị form / section travel style và điểm; chỉ đọc các field còn lại.
- TypeScript: type response profile nên có `travelStyle?` và `point?` optional để khớp JSON thực tế.

---

## Trạng thái backend (tóm tắt)

| Hạng mục | Trạng thái |
|----------|------------|
| JWT + phân quyền portal | Đã có |
| Profile `GET/PUT /api/profile` — admin/staff không có `travelStyle`/`point`, có `role` | Đã có |
| Admin: users, staff-accounts, analytics, audit, embeddings | Đã có |
| Staff/Admin: feedback waypoint + **feedback cả chuyến** (`/feedbacks/journeys`), moderate, report user | Đã có |
| Micro-experience: GET public; ghi chỉ `staff`; admin xem + chạy embedding | Đã có |
| Khẩn cấp: Goong AutoComplete + Place Detail qua `POST /api/emergency/nearby` | Đã có |
| DTO địa điểm: Tags, RichDescription, OpeningHours, PriceRange, CrowdLevel, AmenityTags + regenerate embedding | Đã có |
| CORS | Đã có (`appsettings` + `Program.cs`) |

---

## Chi tiết API địa điểm & payload (staff)

- **GET** `/api/micro-experiences` — query: `keyword`, `categoryId`, `status`, `mood`, `timeOfDay`
- **GET** `/api/micro-experiences/{id}`
- **POST** `/api/micro-experiences` — JWT **staff**; body gồm `name`, `categoryId`, `accessibleBy`, …; `richDescription`, `tags`, `amenityTags`, … cho embedding; tuỳ chọn **`photos`** (URL).
- **PUT** `/api/micro-experiences/{id}` — JWT **staff**; tuỳ chọn **`photos`** (append).
- **POST** `/api/micro-experiences/{id}/photos` — JWT **staff**, `multipart/form-data` (`file`, `caption?`, `isCover?`).
- **DELETE** `/api/micro-experiences/{id}/photos/{photoId}` — JWT **staff**.
- **DELETE** `/api/micro-experiences/{id}` — JWT **staff**
- Chi tiết payload & ảnh cho FE: **`docs/MICRO_EXPERIENCE_FE.md`**
- **GET** `/api/categories` — danh mục (thường public).

### Khẩn cấp (mobile)

- **POST** `/api/emergency/nearby` — JWT; Goong `Place/AutoComplete` (bias bán kính **tối thiểu ~15km** dù `radiusMeters` nhỏ, tránh cắt BV gần theo đường) → `Place/Detail` → `Direction`. Chỉ trả về địa điểm có **khoảng cách ≤ `radiusMeters`** (mặc định **10000**). Tuỳ chọn **`placeKeyword`** để đổi từ khóa (vd. vùng Đồng Nai).
- Cần `Goong:ApiKey` trong `appsettings`.
- `vehicleType` (tuỳ chọn, khi `type=repair_shop`): `walking` \| `bicycle` \| `motorbike` \| `car` — đổi từ khóa tìm (vd. sửa xe máy / ô tô).

---

## Phần 6 — Frontend: Vite + React + TS + Redux (classic) + JWT + API

Giả định bạn **đã** có project `react-ts` (Vite). Dưới đây là **Redux bình thường**: constants → actions → reducers → `combineReducers` → `createStore` + `redux-thunk`.

### 6.1 Cài package

```bash
cd <thư-mục-project-react>
npm install redux react-redux redux-thunk react-router-dom axios
npm install -D @types/node
```

Tuỳ chọn: `npm install jwt-decode` — chỉ để đọc payload JWT trên UI, **không** thay kiểm tra quyền server.

### 6.2 Biến môi trường

```env
VITE_API_BASE_URL=http://localhost:5062
```

Dùng: `import.meta.env.VITE_API_BASE_URL`.

### 6.3 Cấu trúc thư mục gợi ý

```
src/
  constants/
    authTypes.ts             # AUTH_SET_CREDENTIALS, AUTH_LOGOUT, ...
  actions/
    authActions.ts           # action creators + thunk loginUser
    adminUserActions.ts      # ví dụ fetchUsers (thunk)
  reducers/
    authReducer.ts
    adminUsersReducer.ts     # tùy module
    rootReducer.ts           # combineReducers({ auth, adminUsers, ... })
  store.ts                   # createStore(rootReducer, applyMiddleware(thunk))
  api/
    axios.ts                 # instance + Bearer + 401
  routes/
    AppRoutes.tsx
    ProtectedRoute.tsx
  pages/
    LoginPage.tsx
    admin/
    staff/
  main.tsx
```

### 6.4 Action types & action creators (auth)

`constants/authTypes.ts`:

```ts
export const AUTH_SET_CREDENTIALS = 'AUTH_SET_CREDENTIALS';
export const AUTH_LOGOUT = 'AUTH_LOGOUT';
```

`actions/authActions.ts` — sync:

```ts
import type { AuthCredentials } from '../types/auth'; // tuỳ bạn định nghĩa
import * as T from '../constants/authTypes';

export const setCredentials = (payload: AuthCredentials) =>
  ({ type: T.AUTH_SET_CREDENTIALS, payload } as const);

export const logout = () => ({ type: T.AUTH_LOGOUT } as const);
```

Trong **thunk** `loginUser(email, password)`:

1. `POST /api/auth/login` (nên dùng `axios` **riêng** không gắn Bearer, hoặc instance chung khi chưa có token).
2. `dispatch(setCredentials({ accessToken, refreshToken, userId, email, role }))`.
3. Ghi `sessionStorage` (đồng bộ với reducer hoặc trong middleware — tuỳ bạn).
4. `react-router` navigate theo `role`.

### 6.5 Reducer & root reducer

`reducers/authReducer.ts` — state gợi ý: `accessToken`, `refreshToken`, `userId`, `email`, `role`, `isAuthenticated`.

```ts
import * as T from '../constants/authTypes';

const initialState = { /* ... null/false */ };

export default function authReducer(state = initialState, action: any) {
  switch (action.type) {
    case T.AUTH_SET_CREDENTIALS:
      return { ...state, ...action.payload, isAuthenticated: true };
    case T.AUTH_LOGOUT:
      return initialState;
    default:
      return state;
  }
}
```

`reducers/rootReducer.ts`:

```ts
import { combineReducers } from 'redux';
import authReducer from './authReducer';

export const rootReducer = combineReducers({
  auth: authReducer,
  // adminUsers: adminUsersReducer,
});

export type RootState = ReturnType<typeof rootReducer>;
```

### 6.6 Store

`store.ts`:

```ts
import { createStore, applyMiddleware } from 'redux';
import thunk from 'redux-thunk';
import { rootReducer } from './reducers/rootReducer';

// Tuỳ chọn Redux DevTools: compose với window.__REDUX_DEVTOOLS_EXTENSION_COMPOSE__
export const store = createStore(rootReducer, applyMiddleware(thunk));
```

Khi app load: đọc `sessionStorage` → nếu còn token thì `store.dispatch(setCredentials(...))` một lần (trong `main` hoặc component bootstrap).

### 6.7 Axios + JWT

`src/api/axios.ts`:

1. `baseURL: import.meta.env.VITE_API_BASE_URL`
2. Request interceptor: `Authorization: Bearer ${getToken()}` — `getToken` đọc `store.getState().auth.accessToken` **hoặc** `sessionStorage` (tránh vòng phụ thuộc store ↔ axios).
3. Response `401`: `store.dispatch(logout())`, xóa storage, chuyển `/login`.

**Login:** gọi trực tiếp `axios.post(baseURL + '/api/auth/login', ...)` (import `axios` gốc) hoặc tạo `publicApi` không interceptor Bearer.

### 6.8 Async actions (redux-thunk) — ví dụ danh sách user

```ts
import type { Dispatch } from 'redux';
import api from '../api/axios';
import * as T from '../constants/adminUserTypes';

export const fetchAdminUsers = (params: { page?: number; role?: string }) => {
  return async (dispatch: Dispatch) => {
    dispatch({ type: T.ADMIN_USERS_LOADING });
    try {
      const { data } = await api.get('/api/admin/users', { params });
      dispatch({ type: T.ADMIN_USERS_SUCCESS, payload: data });
    } catch (e: any) {
      dispatch({
        type: T.ADMIN_USERS_ERROR,
        payload: e.response?.data?.message ?? 'Lỗi mạng',
      });
    }
  };
};
```

Reducer tương ứng xử lý `LOADING` / `SUCCESS` / `ERROR`. Hoặc gọi `api.get` trong `useEffect` và chỉ dùng Redux cho auth — tùy quy mô project.

### 6.9 React Router + `main.tsx`

- `/login` public; `/admin/*`, `/staff/*` bọc `ProtectedRoute` đọc `useSelector((s: RootState) => s.auth)`.

```tsx
import { Provider } from 'react-redux';
import { BrowserRouter } from 'react-router-dom';
import { store } from './store';
import AppRoutes from './routes/AppRoutes';

createRoot(document.getElementById('root')!).render(
  <Provider store={store}>
    <BrowserRouter>
      <AppRoutes />
    </BrowserRouter>
  </Provider>
);
```

Gợi ý typing: `useSelector` / `useDispatch` với `RootState` và `ThunkDispatch` (hoặc `AppDispatch` = typeof store.dispatch).

### 6.10 Checklist trước khi nối API

| Việc | Ghi chú |
|------|---------|
| Backend chạy, đúng `VITE_API_BASE_URL` | |
| CORS có origin Vite (`5173`) | Đã cấu hình backend |
| User `admin` / `staff` trong DB | |
| Staff gọi POST/PUT/DELETE experience | Token phải là user `role=staff` |
| Admin gọi embedding | Token `role=admin` |

### 6.11 Bảo mật

- Không commit `.env` production.
- Token trong `sessionStorage` an toàn hơn `localStorage` một chút trước XSS persistent; vẫn **không** coi là an toàn tuyệt đối.
- UI chỉ ẩn menu; **quyền thật ở API** (401/403).

---

## Thứ tự làm frontend (gợi ý)

1. `constants` → `authReducer` + `rootReducer` + `store` + `redux-thunk`.
2. `authActions` (setCredentials, logout, thunk `loginUser`) + persist `sessionStorage` + axios interceptor.
3. `LoginPage` + `ProtectedRoute` + layout theo `role`.
4. Trang staff: list micro-experiences (GET có thể không token), form create/update (JWT staff).
5. Trang admin: users, analytics, audit, embedding — thêm reducers/actions từng module khi cần.
6. Feedback: thunk + reducer list hoặc state local.
7. **Profile:** `GET/PUT /api/profile` — layout admin/staff bỏ travel style & điểm; dùng `role` từ response.

---

## Mang ngữ cảnh sang project React (Cursor)

- Copy file **`docs/WEB_ADMIN_STAFF_PORTAL.md`** sang repo FE, **hoặc** mở workspace monorepo và `@` file này trong chat React.
- Mỗi phiên chat mới: nhắc `VITE_API_BASE_URL` + flow login + bảng API trên.

---

*Tài liệu đồng bộ với backend JourneySense (portal + CORS + JWT). Cập nhật khi thêm endpoint mới.*
