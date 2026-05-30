using System.Globalization;
using System.Text.Json;

namespace AwsDoc4.Resources;

public static class Extensions
{
    private static readonly HashSet<string> YesValues =
        new(StringComparer.OrdinalIgnoreCase) { "Y", "YES", "T", "TRUE" };

    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    private static bool IsNull(object? value)
        => value is null || value == DBNull.Value;

    // =========================
    // BOOL
    // =========================
    public static bool? ToBoolN(this object? value)
    {
        if (IsNull(value))
        {
            return null;
        }

        return value switch
        {
            bool b => b,
            string s => YesValues.Contains(s),
            _ => YesValues.Contains(value!.ToString()!)
        };
    }

    public static bool ToBool(this object? value, bool defaultValue)
        => value.ToBoolN() ?? defaultValue;

    // =========================
    // GENERIC PARSER (.NET8)
    // =========================
    private static T? ParseNullable<T>(object? value)
        where T : struct, ISpanParsable<T>
    {
        if (IsNull(value))
        {
            return null;
        }

        return value switch
        {
            T v => v,
            string s when T.TryParse(s, Invariant, out var r) => r,
            _ => T.TryParse(value!.ToString(), Invariant, out var r) ? r : null
        };
    }

    private static T ParseOrDefault<T>(object? value, T defaultValue)
        where T : struct, ISpanParsable<T>
        => ParseNullable<T>(value) ?? defaultValue;

    // =========================
    // NUMERIC
    // =========================
    public static int? ToIntN(this object? value) => ParseNullable<int>(value);
    public static int ToInt32(this object? value, int def) => ParseOrDefault(value, def);
    public static int ToInt32(this object? value) => ParseOrDefault(value, default(int));

    public static uint? ToUIntN(this object? value) => ParseNullable<uint>(value);
    public static uint ToUInt32(this object? value, uint def) => ParseOrDefault(value, def);

    public static ulong? ToUInt64N(this object? value) => ParseNullable<ulong>(value);
    public static ulong ToUInt64(this object? value, ulong def) => ParseOrDefault(value, def);

    public static double? ToDoubleN(this object? value) => ParseNullable<double>(value);
    public static double ToDouble(this object? value, double def) => ParseOrDefault(value, def);

    public static decimal? ToDecimalN(this object? value) => ParseNullable<decimal>(value);
    public static decimal ToDecimal(this object? value, decimal def) => ParseOrDefault(value, def);

    public static float? ToFloatN(this object? value) => ParseNullable<float>(value);
    public static float ToFloat(this object? value, float def) => ParseOrDefault(value, def);

    public static short? ToShortN(this object? value) => ParseNullable<short>(value);
    public static short ToShort(this object? value, short def) => ParseOrDefault(value, def);

    public static byte? ToByteN(this object? value) => ParseNullable<byte>(value);
    public static byte ToByte(this object? value, byte def) => ParseOrDefault(value, def);

    // =========================
    // DATETIME
    // =========================
    public static DateTime? ToDateTimeN(this object? value)
    {
        if (IsNull(value)) return null;

        return value switch
        {
            DateTime dt => dt,
            DateOnly d => d.ToDateTime(TimeOnly.MinValue),
            string s when DateTime.TryParse(s, Invariant, DateTimeStyles.None, out var dt) => dt,
            double d => DateTime.FromOADate(d),
            _ => null
        };
    }

    public static DateTime ToDateTime(this object? value, DateTime def)
        => value.ToDateTimeN() ?? def;
    public static DateTime ToDateTime(this object? value, string format)
        => value.ToDateTimeN() ?? default(DateTime);

    public static DateTime? ToDateTimeN(this object? value, string? format = null)
    {
        if (value == null || value == DBNull.Value)
        {
            return null;
        }
        if (string.IsNullOrEmpty(format))
        {
            return DateTime.TryParse(value.ToString()!, System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : null;
        }
        else
        {
            return DateTime.TryParseExact(value.ToString()!, format, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var d) ? d : null;
        }
    }

    public static string? ToJson(this object? obj, JsonSerializerOptions? option = null)
    {
        var json = obj.ToJsonDocument();
        if (json == null)
        {
            return null;
        }

        return json is JsonDocument doc
                    ? doc.RootElement.ToString()
                    : string.Empty;
    }

    public static JsonDocument? ToJsonDocument(this object? obj, JsonSerializerOptions? option = null)
    {
        if (option == null)
        {
            option = new JsonSerializerOptions() { WriteIndented = true };
        }
        try
        {
            if (obj == null)
            {
                return null;
            }
            if (obj is string json)
            {
                return JsonDocument.Parse(json);
            }
            else
            {
                return JsonDocument.Parse(JsonSerializer.Serialize(obj, option));
            }
        }
        catch
        {
            return null;
        }
    }
}
