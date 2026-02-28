-- Fix: "relation categories already exists" khi chạy dotnet ef database update
-- Chạy script này TRÊN DATABASE ĐANG DÙNG khi bạn đã có sẵn đủ bảng (từ lần migrate trước hoặc tạo tay).
-- Mục đích: Đánh dấu migration "initial" là đã apply để EF không chạy lại (tạo lại bảng).

-- 1. Đảm bảo bảng lịch sử migration tồn tại
CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" varchar(150) NOT NULL PRIMARY KEY,
    "ProductVersion" varchar(32) NOT NULL
);

-- 2. Đánh dấu migration initial đã chạy (chỉ thêm nếu chưa có)
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260213031635_initial', '8.0.23')
ON CONFLICT ("MigrationId") DO NOTHING;

-- 3. (Tùy chọn) Nếu bạn đã tự thêm cột reward_points vào user_profiles rồi,
--    đánh dấu luôn migration AddRewardPointsToUserProfile để EF không chạy lại:
-- INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
-- VALUES ('20260228043757_AddRewardPointsToUserProfile', '8.0.23')
-- ON CONFLICT ("MigrationId") DO NOTHING;

-- Sau khi chạy script trên, chạy lại: dotnet ef database update
