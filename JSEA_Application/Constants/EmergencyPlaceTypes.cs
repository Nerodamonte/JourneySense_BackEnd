namespace JSEA_Application.Constants;

/// <summary>Giá trị <c>type</c> trong POST /api/emergency/nearby. Từ khóa autocomplete khác nhau; mặc định số kết quả: khẩn cấp → 1 (ưu tiên vẽ tuyến gần nhất), ăn/nghỉ/cà phê → list chọn.</summary>
public static class EmergencyPlaceTypes
{
    public const string RepairShop = "repair_shop";
    public const string Hospital = "hospital";
    public const string Pharmacy = "pharmacy";

    /// <summary>Hết xăng / cây xăng gần nhất.</summary>
    public const string GasStation = "gas_station";

    /// <summary>Đói bụng / quán ăn gần nhất.</summary>
    public const string Restaurant = "restaurant";

    /// <summary>Nghỉ chân / nhà nghỉ, khách sạn.</summary>
    public const string Lodging = "lodging";

    /// <summary>Uống nước / quán cà phê.</summary>
    public const string Coffee = "coffee";

    /// <summary>
    /// Bệnh viện, xăng, sửa xe, thuốc: mặc định 1 điểm + tuyến; SignalR chỉ loại này (trừ khi client chỉnh MaxResults list 1 kết quả).
    /// </summary>
    public static bool PrefersSingleRoute(string normalizedPlaceType) =>
        normalizedPlaceType is Hospital or Pharmacy or GasStation or RepairShop;

    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        var t = value.Trim().ToLowerInvariant();
        return t is RepairShop or Hospital or Pharmacy
            or GasStation or Restaurant or Lodging or Coffee;
    }

    public static string? NormalizeOrNull(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var t = value.Trim().ToLowerInvariant();
        return IsValid(t) ? t : null;
    }

    /// <summary>Từ khóa AutoComplete gửi Goong theo loại khẩn cấp (và phương tiện khi sửa xe).</summary>
    public static string SearchInputFor(string placeType, string? vehicleTypeLower)
    {
        var v = string.IsNullOrWhiteSpace(vehicleTypeLower)
            ? null
            : vehicleTypeLower.Trim().ToLowerInvariant();
        return placeType switch
        {
            RepairShop => v switch
            {
                "car" => "sửa xe ô tô",
                "motorbike" => "sửa xe máy",
                "bicycle" => "sửa xe đạp",
                "walking" => "sửa xe đạp",
                _ => "sửa xe máy"
            },
            Hospital => "bệnh viện",
            Pharmacy => "nhà thuốc",
            GasStation => "cây xăng",
            Restaurant => "quán ăn",
            Lodging => "khách sạn nhà nghỉ",
            Coffee => "quán cà phê",
            _ => "cửa hàng"
        };
    }
}
