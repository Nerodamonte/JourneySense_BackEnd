namespace JSEA_Application.Constants;

/// <summary>Giá trị emergency_places.type và body nearby.</summary>
public static class EmergencyPlaceTypes
{
    public const string RepairShop = "repair_shop";
    public const string Hospital = "hospital";
    public const string Pharmacy = "pharmacy";

    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        var t = value.Trim().ToLowerInvariant();
        return t is RepairShop or Hospital or Pharmacy;
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
            _ => "cửa hàng"
        };
    }
}
