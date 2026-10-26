using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rxintel.AI.Converters
{
    /// <summary>
    /// Tolerant JSON converter for DateTime? that accepts standard ISO-8601,
    /// common US formats (e.g. MM/dd/yyyy, MM/dd/yyyy hh:mm:ss tt), Excel OADate numbers,
    /// and treats empty/whitespace strings as null instead of throwing JsonException.
    /// </summary>
    public class FlexibleNullableDateTimeConverter : JsonConverter<DateTime?>
    {
        private static readonly string[] Formats =
        {
            "yyyy-MM-ddTHH:mm:ss.FFFFFFFZ",
            "yyyy-MM-ddTHH:mm:ss.FFFFFFFzzz",
            "yyyy-MM-ddTHH:mm:ss.fffZ",
            "yyyy-MM-ddTHH:mm:ss.fff",
            "yyyy-MM-ddTHH:mm:ssZ",
            "yyyy-MM-ddTHH:mm:ss",
            "yyyy-MM-dd",
            "MM/dd/yyyy",
            "M/d/yyyy",
            "MM/dd/yyyy HH:mm:ss",
            "M/d/yyyy HH:mm:ss",
            "MM/dd/yyyy hh:mm:ss tt",
            "M/d/yyyy hh:mm:ss tt",
            "MM/dd/yyyy h:mm:ss tt",
            "M/d/yyyy h:mm:ss tt",
            "MM/dd/yyyy h:mm tt",
            "M/d/yyyy h:mm tt",
            "MM/dd/yyyy HH:mm",
            "M/d/yyyy HH:mm",
            "MM-dd-yyyy",
            "M-d-yyyy",
            "yyyy/MM/dd",
            "yyyy/M/d"
        };

        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            if (reader.TokenType == JsonTokenType.String)
            {
                var str = reader.GetString();
                if (string.IsNullOrWhiteSpace(str) || str == "—" || str.Equals("null", StringComparison.OrdinalIgnoreCase))
                    return null;

                var trimmed = str.Trim();

                if (DateTime.TryParseExact(trimmed, Formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var exactDt))
                    return exactDt;

                if (DateTime.TryParseExact(trimmed, Formats, CultureInfo.GetCultureInfo("en-US"), DateTimeStyles.None, out var usDt))
                    return usDt;

                if (DateTime.TryParse(trimmed, CultureInfo.GetCultureInfo("en-US"), DateTimeStyles.None, out var generalUsDt))
                    return generalUsDt;

                if (DateTime.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.None, out var generalDt))
                    return generalDt;

                if (double.TryParse(trimmed, NumberStyles.Any, CultureInfo.InvariantCulture, out var oaDate) && oaDate >= 20000 && oaDate <= 100000)
                {
                    try { return DateTime.FromOADate(oaDate); } catch { }
                }

                // If parsing fails, return null rather than breaking the entire request
                return null;
            }

            if (reader.TokenType == JsonTokenType.Number)
            {
                if (reader.TryGetDouble(out var num) && num >= 20000 && num <= 100000)
                {
                    try { return DateTime.FromOADate(num); } catch { }
                }
            }

            return null;
        }

        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
                writer.WriteStringValue(value.Value.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture));
            else
                writer.WriteNullValue();
        }
    }

    /// <summary>
    /// Tolerant JSON converter for non-nullable DateTime.
    /// </summary>
    public class FlexibleDateTimeConverter : JsonConverter<DateTime>
    {
        private readonly FlexibleNullableDateTimeConverter _inner = new();

        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var result = _inner.Read(ref reader, typeof(DateTime?), options);
            return result ?? default;
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture));
        }
    }

    /// <summary>
    /// Tolerant JSON converter for TimeSpan? that accepts standard formats ("12:11:00", "12:11")
    /// and treats empty/whitespace strings as null.
    /// </summary>
    public class FlexibleNullableTimeSpanConverter : JsonConverter<TimeSpan?>
    {
        public override TimeSpan? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            if (reader.TokenType == JsonTokenType.String)
            {
                var str = reader.GetString();
                if (string.IsNullOrWhiteSpace(str) || str == "—" || str.Equals("null", StringComparison.OrdinalIgnoreCase))
                    return null;

                var trimmed = str.Trim();
                if (TimeSpan.TryParse(trimmed, CultureInfo.InvariantCulture, out var ts))
                    return ts;

                if (DateTime.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                    return dt.TimeOfDay;

                return null;
            }

            return null;
        }

        public override void Write(Utf8JsonWriter writer, TimeSpan? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
                writer.WriteStringValue(value.Value.ToString("c", CultureInfo.InvariantCulture));
            else
                writer.WriteNullValue();
        }
    }
}
