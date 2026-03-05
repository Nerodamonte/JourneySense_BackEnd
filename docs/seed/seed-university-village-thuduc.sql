-- Seed data for JourneySense v10
-- Khu vực: Làng Đại Học Thủ Đức (ĐHQG TP.HCM)
-- Chạy sau khi đã apply JSEAv5.sql
-- Lưu ý: các UUID cố định để dễ tham chiếu trong code/tests.

BEGIN;

-- =========================================================
-- 1. USERS & USER PROFILES
-- =========================================================

-- staff tạo Experience
INSERT INTO users (id, email, phone, password_hash, role, status, email_verified, phone_verified, created_at)
VALUES (
    '00000000-0000-0000-0000-000000000001',
    'staff1@journeysense.local',
    NULL,
    NULL,                          -- có thể set hash sau
    'staff',
    'active',
    TRUE,
    FALSE,
    now()
)
ON CONFLICT (id) DO NOTHING;

-- traveler dùng để test journeys
INSERT INTO users (id, email, phone, password_hash, role, status, email_verified, phone_verified, created_at)
VALUES (
    '00000000-0000-0000-0000-000000000002',
    'user@test.com',
    NULL,
    NULL,
    'traveler',
    'active',
    TRUE,
    FALSE,
    now()
)
ON CONFLICT (id) DO NOTHING;

-- user_profiles
INSERT INTO user_profiles (id, user_id, full_name, department, permissions)
VALUES (
    '00000000-0000-0000-0000-000000000101',
    '00000000-0000-0000-0000-000000000001',
    'Staff 1',
    'Operations',
    '{}'::jsonb
)
ON CONFLICT (id) DO NOTHING;

INSERT INTO user_profiles (id, user_id, full_name, department, permissions)
VALUES (
    '00000000-0000-0000-0000-000000000102',
    '00000000-0000-0000-0000-000000000002',
    'Traveler 1',
    'Student',
    '{}'::jsonb
)
ON CONFLICT (id) DO NOTHING;

-- =========================================================
-- 2. FACTORS (vibes & moods)
-- =========================================================

-- Moods
INSERT INTO factors (id, name, type, type_weight, name_weight, description, is_active) VALUES
('00000000-0000-0000-0000-000000001001', 'Relax',       'mood', 0.60, 3, 'Muốn thư giãn, nhẹ nhàng', TRUE),
('00000000-0000-0000-0000-000000001002', 'Study',       'mood', 0.60, 4, 'Tập trung học tập / làm bài', TRUE),
('00000000-0000-0000-0000-000000001003', 'Foodie',      'mood', 0.60, 4, 'Tìm đồ ăn ngon quanh làng ĐH', TRUE),
('00000000-0000-0000-0000-000000001004', 'Photography', 'mood', 0.60, 2, 'Chụp ảnh sống ảo, góc đẹp', TRUE)
ON CONFLICT (id) DO NOTHING;

-- Vibes (tùy chọn)
INSERT INTO factors (id, name, type, type_weight, name_weight, description, is_active) VALUES
('00000000-0000-0000-0000-000000001101', 'Chill',          'vibe', 0.40, 3, 'Không khí chill, yên tĩnh', TRUE),
('00000000-0000-0000-0000-000000001102', 'Social',         'vibe', 0.40, 2, 'Đông vui, dễ kết bạn', TRUE),
('00000000-0000-0000-0000-000000001103', 'NightStudy',     'vibe', 0.40, 4, 'Học khuya, mở muộn', TRUE)
ON CONFLICT (id) DO NOTHING;

-- Traveler 1 chọn một số vibes
INSERT INTO user_vibes (user_profile_id, factor_id, selected_at) VALUES
('00000000-0000-0000-0000-000000000102', '00000000-0000-0000-0000-000000001101', now()),
('00000000-0000-0000-0000-000000000102', '00000000-0000-0000-0000-000000001103', now())
ON CONFLICT (user_profile_id, factor_id) DO NOTHING;

-- =========================================================
-- 3. CATEGORIES
-- =========================================================

INSERT INTO categories (id, name, slug, description, icon_url, display_order, is_active) VALUES
('00000000-0000-0000-0000-000000002001', 'Cafe & Study',      'cafe-study',      'Quán cà phê, không gian học bài', NULL, 1, TRUE),
('00000000-0000-0000-0000-000000002002', 'Ăn uống bình dân',  'student-food',   'Quán ăn sinh viên quanh làng ĐH', NULL, 2, TRUE),
('00000000-0000-0000-0000-000000002003', 'Tiện ích sinh viên','student-utility','Nhà sách, photocopy, nhà văn hóa', NULL, 3, TRUE),
('00000000-0000-0000-0000-000000002004', 'Công viên & ngoài trời','outdoor',   'Không gian xanh, tập thể dục', NULL, 4, TRUE)
ON CONFLICT (id) DO NOTHING;

-- =========================================================
-- 4. EXPERIENCES quanh Làng Đại Học Thủ Đức / Linh Trung / Đông Hoà
-- Tọa độ từ Goong Geocode: ST_MakePoint(longitude, latitude)
-- =========================================================


INSERT INTO experiences (
    id, created_by_user_id, category_id,
    name, slug, location, address, city, country,
    accessible_by, weather_suitability, preferred_times, seasonality, tags, status, created_at
) VALUES
('00000000-0000-0000-0000-000000003001','00000000-0000-0000-0000-000000000001','00000000-0000-0000-0000-000000002002',
 'Cơm Gà Xối Mỡ Nghĩa Ký','com-ga-xoi-mo-nghia-ky',ST_SetSRID(ST_MakePoint(106.7979649, 10.871449),4326)::geography,
 'Đường Trục Chính 10, Phường Linh Trung, Thủ Đức, Thành phố Hồ Chí Minh, Việt Nam','Thủ Đức','Vietnam',
 ARRAY['walking','bicycle','motorbike'],ARRAY['Sunny','Cloudy','Rainy'],ARRAY['Morning','Afternoon','Evening'],ARRAY['AllYear'],ARRAY['com-ga','student-food'],'active',now()),
('00000000-0000-0000-0000-000000003002','00000000-0000-0000-0000-000000000001','00000000-0000-0000-0000-000000002002',
 'Min Báo - Bánh Tráng & Trà Sữa','min-bao-banh-trang-tra-sua',ST_SetSRID(ST_MakePoint(106.797764531, 10.869399863),4326)::geography,
 '18/155 tổ 8 Khu Phố 6, Phường Linh Trung, Thủ Đức, Thành phố Hồ Chí Minh, Việt Nam','Thủ Đức','Vietnam',
 ARRAY['walking','bicycle','motorbike'],ARRAY['Sunny','Cloudy'],ARRAY['Morning','Afternoon','Evening'],ARRAY['AllYear'],ARRAY['tra-sua','banh-trang'],'active',now()),
('00000000-0000-0000-0000-000000003003','00000000-0000-0000-0000-000000000001','00000000-0000-0000-0000-000000002001',
 'Cà Phê-Giải Khát Phong Trần','ca-phe-giai-khat-phong-tran',ST_SetSRID(ST_MakePoint(106.7984347, 10.867406),4326)::geography,
 'VQ8X+X9 Thủ Đức, Thành phố Hồ Chí Minh, Việt Nam','Thủ Đức','Vietnam',
 ARRAY['walking','bicycle','motorbike'],ARRAY['Sunny','Cloudy'],ARRAY['Morning','Afternoon','Evening'],ARRAY['AllYear'],ARRAY['cafe','giai-khat'],'active',now()),
('00000000-0000-0000-0000-000000003004','00000000-0000-0000-0000-000000000001','00000000-0000-0000-0000-000000002001',
 'TAN Cà Phê','tan-ca-phe',ST_SetSRID(ST_MakePoint(106.7978097, 10.869333),4326)::geography,
 '18/80 tổ 8, Khu Phố 6, Thủ Đức, Thành phố Hồ Chí Minh, Việt Nam','Thủ Đức','Vietnam',
 ARRAY['walking','bicycle','motorbike'],ARRAY['Sunny','Cloudy'],ARRAY['Morning','Afternoon','Evening'],ARRAY['AllYear'],ARRAY['cafe'],'active',now()),
('00000000-0000-0000-0000-000000003005','00000000-0000-0000-0000-000000000001','00000000-0000-0000-0000-000000002002',
 'Cơm gà xối mỡ Green','com-ga-xoi-mo-green',ST_SetSRID(ST_MakePoint(106.798108913, 10.871764742),4326)::geography,
 'Khu Phố 6, Thủ Đức, Thành phố Hồ Chí Minh, Việt Nam','Thủ Đức','Vietnam',
 ARRAY['walking','bicycle','motorbike'],ARRAY['Sunny','Cloudy','Rainy'],ARRAY['Morning','Afternoon','Evening'],ARRAY['AllYear'],ARRAY['com-ga'],'active',now()),
('00000000-0000-0000-0000-000000003006','00000000-0000-0000-0000-000000000001','00000000-0000-0000-0000-000000002001',
 'Trà Sữa Nọng','tra-sua-nong',ST_SetSRID(ST_MakePoint(106.7980267, 10.871844),4326)::geography,
 'VQCX+P6J, Đường Trục Chính 10, Phường Linh Trung, Thủ Đức, Thành phố Hồ Chí Minh, Việt Nam','Thủ Đức','Vietnam',
 ARRAY['walking','bicycle','motorbike'],ARRAY['Sunny','Cloudy'],ARRAY['Morning','Afternoon','Evening'],ARRAY['AllYear'],ARRAY['tra-sua'],'active',now()),
('00000000-0000-0000-0000-000000003007','00000000-0000-0000-0000-000000000001','00000000-0000-0000-0000-000000002002',
 'Cơm tấm Ngô Quyền','com-tam-ngo-quyen',ST_SetSRID(ST_MakePoint(106.797887, 10.871712),4326)::geography,
 '21/5 Võ Nguyên Giáp, Phường Linh Trung, Thủ Đức, Thành phố Hồ Chí Minh, Việt Nam','Thủ Đức','Vietnam',
 ARRAY['walking','bicycle','motorbike'],ARRAY['Sunny','Cloudy','Rainy'],ARRAY['Morning','Afternoon','Evening'],ARRAY['AllYear'],ARRAY['com-tam'],'active',now()),
('00000000-0000-0000-0000-000000003008','00000000-0000-0000-0000-000000000001','00000000-0000-0000-0000-000000002002',
 'Quán Ăn Vặt Cô Hoa','quan-an-vat-co-hoa',ST_SetSRID(ST_MakePoint(106.7643068, 10.8717986),4326)::geography,
 'Đ. Vào Đại Học Quốc Gia, Phường Linh Trung, Thủ Đức, Thành phố Hồ Chí Minh, Việt Nam','Thủ Đức','Vietnam',
 ARRAY['walking','bicycle','motorbike'],ARRAY['Sunny','Cloudy'],ARRAY['Morning','Afternoon','Evening'],ARRAY['AllYear'],ARRAY['an-vat'],'active',now()),
('00000000-0000-0000-0000-000000003009','00000000-0000-0000-0000-000000000001','00000000-0000-0000-0000-000000002002',
 'Bún bò - Hủ tiếu - Bánh canh','bun-bo-hu-tieu-banh-canh',ST_SetSRID(ST_MakePoint(106.7995562, 10.8724448),4326)::geography,
 '17/13 Đường Số 7, Khu phố 5, Thủ Đức, Thành phố Hồ Chí Minh, Việt Nam','Thủ Đức','Vietnam',
 ARRAY['walking','bicycle','motorbike'],ARRAY['Sunny','Cloudy','Rainy'],ARRAY['Morning','Afternoon','Evening'],ARRAY['AllYear'],ARRAY['bun-bo','hu-tieu'],'active',now()),
('00000000-0000-0000-0000-000000003010','00000000-0000-0000-0000-000000000001','00000000-0000-0000-0000-000000002001',
 'Hồng Trà Ngô Gia','hong-tra-ngo-gia',ST_SetSRID(ST_MakePoint(106.8089235, 10.8749839),4326)::geography,
 'H171 Tổ 8/Ấp Tân Lập, Đông Hoà, Dĩ An, Bình Dương, Việt Nam','Dĩ An','Vietnam',
 ARRAY['walking','bicycle','motorbike'],ARRAY['Sunny','Cloudy'],ARRAY['Morning','Afternoon','Evening'],ARRAY['AllYear'],ARRAY['tra','cafe'],'active',now()),
('00000000-0000-0000-0000-000000003011','00000000-0000-0000-0000-000000000001','00000000-0000-0000-0000-000000002002',
 'Khoai mỡ chiên Thiện Nhân','khoai-mo-chien-thien-nhan',ST_SetSRID(ST_MakePoint(109.191830093, 12.253990352),4326)::geography,
 'VRF2+G79, Nguyễn Thái Học, Vạn Thạnh, Nha Trang, Khánh Hòa, Việt Nam','Nha Trang','Vietnam',
 ARRAY['walking','bicycle','motorbike'],ARRAY['Sunny','Cloudy'],ARRAY['Morning','Afternoon','Evening'],ARRAY['AllYear'],ARRAY['an-vat','khoai'],'active',now()),
('00000000-0000-0000-0000-000000003012','00000000-0000-0000-0000-000000000001','00000000-0000-0000-0000-000000002001',
 'SoYa Tiệm Kem Trứng','soya-tiem-kem-trung',ST_SetSRID(ST_MakePoint(106.7995865, 10.8740291),4326)::geography,
 'Đ. Quảng Trường Sáng Tạo, Đông Hoà, Thủ Đức, Bình Dương, Việt Nam','Thủ Đức','Vietnam',
 ARRAY['walking','bicycle','motorbike'],ARRAY['Sunny','Cloudy'],ARRAY['Morning','Afternoon','Evening'],ARRAY['AllYear'],ARRAY['kem','trang-mieng'],'active',now()),
('00000000-0000-0000-0000-000000003013','00000000-0000-0000-0000-000000000001','00000000-0000-0000-0000-000000002001',
 'MÊ LINH 24H coffee tea','me-linh-24h-coffee-tea',ST_SetSRID(ST_MakePoint(106.7994417, 10.8743156),4326)::geography,
 'Đ. Quảng Trường Sáng Tạo Tân Lập, Dĩ An, Bình Dương, Việt Nam','Dĩ An','Vietnam',
 ARRAY['walking','bicycle','motorbike'],ARRAY['Sunny','Cloudy'],ARRAY['Morning','Afternoon','Evening','Night'],ARRAY['AllYear'],ARRAY['cafe','24h'],'active',now()),
('00000000-0000-0000-0000-000000003014','00000000-0000-0000-0000-000000000001','00000000-0000-0000-0000-000000002001',
 'Rau Má Xay BK (Nhà Văn Hóa SV)','rau-ma-xay-bk',ST_SetSRID(ST_MakePoint(106.7994117, 10.8745969),4326)::geography,
 'Đ. Quảng Trường Sáng Tạo, Đông Hoà, Thủ Đức, Bình Dương, Việt Nam','Thủ Đức','Vietnam',
 ARRAY['walking','bicycle','motorbike'],ARRAY['Sunny','Cloudy'],ARRAY['Morning','Afternoon'],ARRAY['AllYear'],ARRAY['nuoc-ep','rau-ma'],'active',now()),
('00000000-0000-0000-0000-000000003015','00000000-0000-0000-0000-000000000001','00000000-0000-0000-0000-000000002002',
 'Mì Cay Sasin','mi-cay-sasin',ST_SetSRID(ST_MakePoint(106.7990178, 10.8751937),4326)::geography,
 'VQGX+3JC, Đ. Vào Đại Học Quốc Gia, Đông Hoà, Thủ Đức, Thành phố Hồ Chí Minh, Việt Nam','Thủ Đức','Vietnam',
 ARRAY['walking','bicycle','motorbike'],ARRAY['Sunny','Cloudy','Rainy'],ARRAY['Morning','Afternoon','Evening'],ARRAY['AllYear'],ARRAY['mi-cay'],'active',now()),
('00000000-0000-0000-0000-000000003016','00000000-0000-0000-0000-000000000001','00000000-0000-0000-0000-000000002002',
 'Cơm Chay Thiên Nhiên','com-chay-thien-nhien',ST_SetSRID(ST_MakePoint(106.8084726, 10.874396),4326)::geography,
 '10/10 Tân Lập, Đông Hoà, Dĩ An, Bình Dương, Việt Nam','Dĩ An','Vietnam',
 ARRAY['walking','bicycle','motorbike'],ARRAY['Sunny','Cloudy'],ARRAY['Morning','Afternoon','Evening'],ARRAY['AllYear'],ARRAY['com-chay'],'active',now()),
('00000000-0000-0000-0000-000000003017','00000000-0000-0000-0000-000000000001','00000000-0000-0000-0000-000000002002',
 'Bún đậu VĂN','bun-dau-van',ST_SetSRID(ST_MakePoint(106.7987834, 10.8746657),4326)::geography,
 'VQFX+VG7, Đ. Vào Đại Học Quốc Gia, Đông Hoà, Dĩ An, Bình Dương, Việt Nam','Dĩ An','Vietnam',
 ARRAY['walking','bicycle','motorbike'],ARRAY['Sunny','Cloudy','Rainy'],ARRAY['Morning','Afternoon','Evening'],ARRAY['AllYear'],ARRAY['bun-dau'],'active',now()),
('00000000-0000-0000-0000-000000003018','00000000-0000-0000-0000-000000000001','00000000-0000-0000-0000-000000002002',
 'Bò Né Chibi (Beefsteak)','bo-ne-chibi',ST_SetSRID(ST_MakePoint(106.7986673, 10.8744752),4326)::geography,
 'Đường T1/2/10 Đ. Vào Đại Học Quốc Gia, Đông Hoà, Dĩ An, Bình Dương, Việt Nam','Dĩ An','Vietnam',
 ARRAY['walking','bicycle','motorbike'],ARRAY['Sunny','Cloudy','Rainy'],ARRAY['Morning','Afternoon','Evening'],ARRAY['AllYear'],ARRAY['bo-ne'],'active',now())
ON CONFLICT (id) DO NOTHING;

-- EXPERIENCE_DETAILS
INSERT INTO experience_details (experience_id, description, opening_hours, price_range, crowd_level, estimated_duration_minutes, safety_notes, accessibility_info, created_at)
VALUES
('00000000-0000-0000-0000-000000003001','Quán cơm gà xối mỡ quanh làng đại học. Phù hợp sinh viên.','{"mon":"07:00-22:00","tue":"07:00-22:00","wed":"07:00-22:00","thu":"07:00-22:00","fri":"07:00-22:00","sat":"07:00-22:00","sun":"07:00-22:00"}','50-100k','normal',45,NULL,'Lối vào bằng phẳng.',now()),
('00000000-0000-0000-0000-000000003002','Bánh tráng, trà sữa. Quanh Linh Trung.','{"mon":"09:00-22:00","tue":"09:00-22:00","wed":"09:00-22:00","thu":"09:00-22:00","fri":"09:00-22:00","sat":"09:00-22:00","sun":"09:00-22:00"}','50-100k','normal',45,NULL,'Lối vào bằng phẳng.',now()),
('00000000-0000-0000-0000-000000003003','Cà phê giải khát, Thủ Đức.','{"mon":"07:00-23:00","tue":"07:00-23:00","wed":"07:00-23:00","thu":"07:00-23:00","fri":"07:00-23:00","sat":"07:00-23:00","sun":"07:00-23:00"}','50-100k','normal',60,NULL,'Lối vào bằng phẳng.',now()),
('00000000-0000-0000-0000-000000003004','TAN Cà Phê, Khu Phố 6.','{"mon":"07:00-22:00","tue":"07:00-22:00","wed":"07:00-22:00","thu":"07:00-22:00","fri":"07:00-22:00","sat":"07:00-22:00","sun":"07:00-22:00"}','50-100k','normal',60,NULL,'Lối vào bằng phẳng.',now()),
('00000000-0000-0000-0000-000000003005','Cơm gà xối mỡ Green, Khu Phố 6.','{"mon":"06:00-21:00","tue":"06:00-21:00","wed":"06:00-21:00","thu":"06:00-21:00","fri":"06:00-21:00","sat":"06:00-21:00","sun":"06:00-21:00"}','<50k','busy',30,NULL,'Lối vào bằng phẳng.',now()),
('00000000-0000-0000-0000-000000003006','Trà sữa Nọng, Linh Trung.','{"mon":"09:00-22:00","tue":"09:00-22:00","wed":"09:00-22:00","thu":"09:00-22:00","fri":"09:00-22:00","sat":"09:00-22:00","sun":"09:00-22:00"}','50-100k','normal',45,NULL,'Lối vào bằng phẳng.',now()),
('00000000-0000-0000-0000-000000003007','Cơm tấm Ngô Quyền, Võ Nguyên Giáp.','{"mon":"06:00-21:00","tue":"06:00-21:00","wed":"06:00-21:00","thu":"06:00-21:00","fri":"06:00-21:00","sat":"06:00-21:00","sun":"06:00-21:00"}','<50k','busy',30,NULL,'Lối vào bằng phẳng.',now()),
('00000000-0000-0000-0000-000000003008','Quán ăn vặt Cô Hoa, đường vào ĐHQG.','{"mon":"10:00-22:00","tue":"10:00-22:00","wed":"10:00-22:00","thu":"10:00-22:00","fri":"10:00-22:00","sat":"10:00-22:00","sun":"10:00-22:00"}','<50k','normal',30,NULL,'Lối vào bằng phẳng.',now()),
('00000000-0000-0000-0000-000000003009','Bún bò, hủ tiếu, bánh canh. Khu phố 5.','{"mon":"06:00-21:00","tue":"06:00-21:00","wed":"06:00-21:00","thu":"06:00-21:00","fri":"06:00-21:00","sat":"06:00-21:00","sun":"06:00-21:00"}','<50k','busy',30,NULL,'Lối vào bằng phẳng.',now()),
('00000000-0000-0000-0000-000000003010','Hồng Trà Ngô Gia, Đông Hoà Dĩ An.','{"mon":"08:00-22:00","tue":"08:00-22:00","wed":"08:00-22:00","thu":"08:00-22:00","fri":"08:00-22:00","sat":"08:00-22:00","sun":"08:00-22:00"}','50-100k','normal',45,NULL,'Lối vào bằng phẳng.',now()),
('00000000-0000-0000-0000-000000003011','Khoai mỡ chiên Thiện Nhân, Nha Trang.','{"mon":"09:00-21:00","tue":"09:00-21:00","wed":"09:00-21:00","thu":"09:00-21:00","fri":"09:00-21:00","sat":"09:00-21:00","sun":"09:00-21:00"}','<50k','normal',20,NULL,'Lối vào bằng phẳng.',now()),
('00000000-0000-0000-0000-000000003012','SoYa tiệm kem trứng, Quảng Trường Sáng Tạo.','{"mon":"10:00-22:00","tue":"10:00-22:00","wed":"10:00-22:00","thu":"10:00-22:00","fri":"10:00-22:00","sat":"10:00-22:00","sun":"10:00-22:00"}','50-100k','normal',45,NULL,'Lối vào bằng phẳng.',now()),
('00000000-0000-0000-0000-000000003013','MÊ LINH 24H coffee tea, Dĩ An.','{"mon":"00:00-23:59","tue":"00:00-23:59","wed":"00:00-23:59","thu":"00:00-23:59","fri":"00:00-23:59","sat":"00:00-23:59","sun":"00:00-23:59"}','50-100k','normal',60,NULL,'Lối vào bằng phẳng.',now()),
('00000000-0000-0000-0000-000000003014','Rau má xay BK, Nhà Văn Hóa SV.','{"mon":"07:00-21:00","tue":"07:00-21:00","wed":"07:00-21:00","thu":"07:00-21:00","fri":"07:00-21:00","sat":"07:00-21:00","sun":"07:00-21:00"}','<50k','normal',30,NULL,'Lối vào bằng phẳng.',now()),
('00000000-0000-0000-0000-000000003015','Mì cay Sasin, đường vào ĐHQG.','{"mon":"10:00-22:00","tue":"10:00-22:00","wed":"10:00-22:00","thu":"10:00-22:00","fri":"10:00-22:00","sat":"10:00-22:00","sun":"10:00-22:00"}','50-100k','normal',45,NULL,'Lối vào bằng phẳng.',now()),
('00000000-0000-0000-0000-000000003016','Cơm chay Thiên Nhiên, Tân Lập Dĩ An.','{"mon":"07:00-20:00","tue":"07:00-20:00","wed":"07:00-20:00","thu":"07:00-20:00","fri":"07:00-20:00","sat":"07:00-20:00","sun":"07:00-20:00"}','50-100k','quiet',45,NULL,'Lối vào bằng phẳng.',now()),
('00000000-0000-0000-0000-000000003017','Bún đậu VĂN, đường vào ĐHQG.','{"mon":"10:00-22:00","tue":"10:00-22:00","wed":"10:00-22:00","thu":"10:00-22:00","fri":"10:00-22:00","sat":"10:00-22:00","sun":"10:00-22:00"}','50-100k','normal',45,NULL,'Lối vào bằng phẳng.',now()),
('00000000-0000-0000-0000-000000003018','Bò né Chibi (Beefsteak), Đông Hoà Dĩ An.','{"mon":"10:00-22:00","tue":"10:00-22:00","wed":"10:00-22:00","thu":"10:00-22:00","fri":"10:00-22:00","sat":"10:00-22:00","sun":"10:00-22:00"}','50-100k','normal',45,NULL,'Lối vào bằng phẳng.',now())
ON CONFLICT (experience_id) DO UPDATE SET
    description = EXCLUDED.description,
    opening_hours = EXCLUDED.opening_hours,
    price_range = EXCLUDED.price_range,
    crowd_level = EXCLUDED.crowd_level,
    estimated_duration_minutes = EXCLUDED.estimated_duration_minutes,
    accessibility_info = EXCLUDED.accessibility_info;

-- EXPERIENCE_METRICS (khởi tạo)
INSERT INTO experience_metrics (experience_id, featured_score, total_visits, total_ratings, avg_rating, acceptance_rate, updated_at)
VALUES
('00000000-0000-0000-0000-000000003001',0.7,0,0,0.0,0.00,now()),('00000000-0000-0000-0000-000000003002',0.7,0,0,0.0,0.00,now()),('00000000-0000-0000-0000-000000003003',0.7,0,0,0.0,0.00,now()),('00000000-0000-0000-0000-000000003004',0.7,0,0,0.0,0.00,now()),('00000000-0000-0000-0000-000000003005',0.7,0,0,0.0,0.00,now()),
('00000000-0000-0000-0000-000000003006',0.7,0,0,0.0,0.00,now()),('00000000-0000-0000-0000-000000003007',0.7,0,0,0.0,0.00,now()),('00000000-0000-0000-0000-000000003008',0.7,0,0,0.0,0.00,now()),('00000000-0000-0000-0000-000000003009',0.7,0,0,0.0,0.00,now()),('00000000-0000-0000-0000-000000003010',0.7,0,0,0.0,0.00,now()),
('00000000-0000-0000-0000-000000003011',0.7,0,0,0.0,0.00,now()),('00000000-0000-0000-0000-000000003012',0.7,0,0,0.0,0.00,now()),('00000000-0000-0000-0000-000000003013',0.7,0,0,0.0,0.00,now()),('00000000-0000-0000-0000-000000003014',0.7,0,0,0.0,0.00,now()),('00000000-0000-0000-0000-000000003015',0.7,0,0,0.0,0.00,now()),
('00000000-0000-0000-0000-000000003016',0.7,0,0,0.0,0.00,now()),('00000000-0000-0000-0000-000000003017',0.7,0,0,0.0,0.00,now()),('00000000-0000-0000-0000-000000003018',0.7,0,0,0.0,0.00,now())
ON CONFLICT (experience_id) DO NOTHING;

-- EXPERIENCE_TAGS (Cafe/trà -> Relax, Study; Ăn uống -> Foodie, Relax)
INSERT INTO experience_tags (experience_id, factor_id) VALUES
('00000000-0000-0000-0000-000000003001','00000000-0000-0000-0000-000000001003'),('00000000-0000-0000-0000-000000003001','00000000-0000-0000-0000-000000001001'),
('00000000-0000-0000-0000-000000003002','00000000-0000-0000-0000-000000001003'),('00000000-0000-0000-0000-000000003002','00000000-0000-0000-0000-000000001001'),
('00000000-0000-0000-0000-000000003003','00000000-0000-0000-0000-000000001001'),('00000000-0000-0000-0000-000000003003','00000000-0000-0000-0000-000000001002'),
('00000000-0000-0000-0000-000000003004','00000000-0000-0000-0000-000000001001'),('00000000-0000-0000-0000-000000003004','00000000-0000-0000-0000-000000001002'),
('00000000-0000-0000-0000-000000003005','00000000-0000-0000-0000-000000001003'),('00000000-0000-0000-0000-000000003005','00000000-0000-0000-0000-000000001001'),
('00000000-0000-0000-0000-000000003006','00000000-0000-0000-0000-000000001001'),('00000000-0000-0000-0000-000000003006','00000000-0000-0000-0000-000000001003'),
('00000000-0000-0000-0000-000000003007','00000000-0000-0000-0000-000000001003'),('00000000-0000-0000-0000-000000003007','00000000-0000-0000-0000-000000001001'),
('00000000-0000-0000-0000-000000003008','00000000-0000-0000-0000-000000001003'),('00000000-0000-0000-0000-000000003008','00000000-0000-0000-0000-000000001102'),
('00000000-0000-0000-0000-000000003009','00000000-0000-0000-0000-000000001003'),('00000000-0000-0000-0000-000000003009','00000000-0000-0000-0000-000000001001'),
('00000000-0000-0000-0000-000000003010','00000000-0000-0000-0000-000000001001'),('00000000-0000-0000-0000-000000003010','00000000-0000-0000-0000-000000001003'),
('00000000-0000-0000-0000-000000003011','00000000-0000-0000-0000-000000001003'),('00000000-0000-0000-0000-000000003011','00000000-0000-0000-0000-000000001001'),
('00000000-0000-0000-0000-000000003012','00000000-0000-0000-0000-000000001001'),('00000000-0000-0000-0000-000000003012','00000000-0000-0000-0000-000000001003'),
('00000000-0000-0000-0000-000000003013','00000000-0000-0000-0000-000000001001'),('00000000-0000-0000-0000-000000003013','00000000-0000-0000-0000-000000001002'),
('00000000-0000-0000-0000-000000003014','00000000-0000-0000-0000-000000001001'),('00000000-0000-0000-0000-000000003014','00000000-0000-0000-0000-000000001003'),
('00000000-0000-0000-0000-000000003015','00000000-0000-0000-0000-000000001003'),('00000000-0000-0000-0000-000000003015','00000000-0000-0000-0000-000000001102'),
('00000000-0000-0000-0000-000000003016','00000000-0000-0000-0000-000000001003'),('00000000-0000-0000-0000-000000003016','00000000-0000-0000-0000-000000001001'),
('00000000-0000-0000-0000-000000003017','00000000-0000-0000-0000-000000001003'),('00000000-0000-0000-0000-000000003017','00000000-0000-0000-0000-000000001102'),
('00000000-0000-0000-0000-000000003018','00000000-0000-0000-0000-000000001003'),('00000000-0000-0000-0000-000000003018','00000000-0000-0000-0000-000000001001')
ON CONFLICT (experience_id, factor_id) DO NOTHING;

-- =========================================================
-- 5. SAMPLE JOURNEY quanh Làng ĐH (để test gợi ý)
-- =========================================================

-- Journey: từ KTX A đến Công viên Bờ Hồ
INSERT INTO journeys (
    id, traveler_id,
    origin_location, origin_address,
    destination_location, destination_address,
    route_path,
    total_distance_meters, estimated_duration_minutes,
    current_mood_factor_id,
    preferred_crowd_level,
    vehicle_type,
    max_detour_distance_meters,
    preferred_stop_duration_minutes,
    time_budget_minutes,
    max_stops,
    status,
    created_at
) VALUES (
    '00000000-0000-0000-0000-000000004001',
    '00000000-0000-0000-0000-000000000002',
    ST_SetSRID(ST_MakePoint(106.8030, 10.8780),4326)::geography,
    'KTX khu A, ĐHQG TP.HCM',
    ST_SetSRID(ST_MakePoint(106.8090, 10.8790),4326)::geography,
    'Công viên Bờ Hồ Làng Đại Học',
    ST_GeogFromText('SRID=4326;LINESTRING(106.8030 10.8780, 106.8040 10.8785, 106.8060 10.8775, 106.8075 10.8765, 106.8090 10.8790)'),
    1500,
    10,
    '00000000-0000-0000-0000-000000001002', -- Study
    'all',
    'walking',
    800,
    30,
    90,
    3,
    'planning',
    now()
)
ON CONFLICT (id) DO NOTHING;

COMMIT;

