using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JSEA_Presentation.JsonConverters;

/// <summary>
/// Serialize DateTime/DateTime? and DateTimeOffset/DateTimeOffset? as Vietnam time (+07:00).
/// Read keeps values in UTC to avoid affecting persistence/business logic.
/// </summary>
public sealed class VietnamDateTimeJsonConverterFactory : JsonConverterFactory
{
    private static readonly TimeZoneInfo VietnamTimeZone = ResolveVietnamTimeZone();

    public override bool CanConvert(Type typeToConvert)
    {
        var t = Nullable.GetUnderlyingType(typeToConvert) ?? typeToConvert;
        return t == typeof(DateTime) || t == typeof(DateTimeOffset);
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var underlying = Nullable.GetUnderlyingType(typeToConvert);

        if (underlying == typeof(DateTime))
            return new NullableVietnamDateTimeConverter(VietnamTimeZone);

        if (typeToConvert == typeof(DateTime))
            return new VietnamDateTimeConverter(VietnamTimeZone);

        if (underlying == typeof(DateTimeOffset))
            return new NullableVietnamDateTimeOffsetConverter();

        if (typeToConvert == typeof(DateTimeOffset))
            return new VietnamDateTimeOffsetConverter();

        throw new NotSupportedException($"Unsupported type: {typeToConvert}");
    }

    private static TimeZoneInfo ResolveVietnamTimeZone()
    {
        // Windows: "SE Asia Standard Time"; Linux (IANA): "Asia/Ho_Chi_Minh".
        // Try both, fall back to fixed +07:00.
        var candidates = new[] { "SE Asia Standard Time", "Asia/Ho_Chi_Minh" };
        foreach (var id in candidates)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch
            {
                // ignore
            }
        }

        return TimeZoneInfo.CreateCustomTimeZone(
            id: "Vietnam Standard Time",
            baseUtcOffset: TimeSpan.FromHours(7),
            displayName: "Vietnam Standard Time",
            standardDisplayName: "Vietnam Standard Time");
    }

    private sealed class VietnamDateTimeConverter : JsonConverter<DateTime>
    {
        private readonly TimeZoneInfo _tz;

        public VietnamDateTimeConverter(TimeZoneInfo tz) => _tz = tz;

        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Accept both "Z" and "+07:00"; normalize to UTC DateTime.
            if (reader.TokenType == JsonTokenType.String)
            {
                var s = reader.GetString();
                if (string.IsNullOrWhiteSpace(s))
                    return default;

                var dto = DateTimeOffset.Parse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
                return dto.UtcDateTime;
            }

            if (reader.TokenType == JsonTokenType.Null)
                return default;

            // Fallback (rare)
            return reader.GetDateTime();
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            var utc = value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
            };

            var vn = TimeZoneInfo.ConvertTimeFromUtc(utc, _tz);
            var vnOffset = new DateTimeOffset(vn, TimeSpan.FromHours(7));
            writer.WriteStringValue(vnOffset.ToString("O", CultureInfo.InvariantCulture));
        }
    }

    private sealed class NullableVietnamDateTimeConverter : JsonConverter<DateTime?>
    {
        private readonly VietnamDateTimeConverter _inner;

        public NullableVietnamDateTimeConverter(TimeZoneInfo tz) => _inner = new VietnamDateTimeConverter(tz);

        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;
            return _inner.Read(ref reader, typeof(DateTime), options);
        }

        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        {
            if (!value.HasValue)
            {
                writer.WriteNullValue();
                return;
            }
            _inner.Write(writer, value.Value, options);
        }
    }

    private sealed class VietnamDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
    {
        public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var s = reader.GetString();
                if (string.IsNullOrWhiteSpace(s))
                    return default;
                return DateTimeOffset.Parse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind).ToUniversalTime();
            }

            if (reader.TokenType == JsonTokenType.Null)
                return default;

            return reader.GetDateTimeOffset().ToUniversalTime();
        }

        public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
        {
            var vn = value.ToUniversalTime().ToOffset(TimeSpan.FromHours(7));
            writer.WriteStringValue(vn.ToString("O", CultureInfo.InvariantCulture));
        }
    }

    private sealed class NullableVietnamDateTimeOffsetConverter : JsonConverter<DateTimeOffset?>
    {
        private static readonly VietnamDateTimeOffsetConverter Inner = new();

        public override DateTimeOffset? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;
            return Inner.Read(ref reader, typeof(DateTimeOffset), options);
        }

        public override void Write(Utf8JsonWriter writer, DateTimeOffset? value, JsonSerializerOptions options)
        {
            if (!value.HasValue)
            {
                writer.WriteNullValue();
                return;
            }
            Inner.Write(writer, value.Value, options);
        }
    }
}
