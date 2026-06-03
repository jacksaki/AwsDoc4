using System.Security.Cryptography;
using System.Text;

namespace AwsDoc4;

public static class ConvertExtensions
{
    // =========================
    // HASH
    // =========================
    public static string ToSha256(this string value)
        => Encoding.UTF8.GetBytes(value).ToSha256();

    public static string ToSha256(this byte[] value)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(value);

        var sb = new StringBuilder(hash.Length * 2);
        foreach (var b in hash)
        {
            sb.Append(b.ToString("x2"));
        }

        return sb.ToString();
    }

    // =========================
    // URL
    // =========================
    public static string UrlEncode(this string value)
        => Uri.EscapeDataString(value);

    public static string UrlDecode(this string value)
        => Uri.UnescapeDataString(value);

    // =========================
    // BASE64
    // =========================
    public static string Base64Encode(this string value)
        => string.IsNullOrEmpty(value)
            ? string.Empty
            : Convert.ToBase64String(Encoding.UTF8.GetBytes(value));

    public static byte[] Base64Decode(this string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return Array.Empty<byte>();
        }

        try
        {
            return Convert.FromBase64String(value);
        }
        catch
        {
            return Array.Empty<byte>();
        }
    }

    // =========================
    // STRING CASE
    // =========================
    public static string ToSnakeCase(this string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        var sb = new StringBuilder(text.Length + 8);
        sb.Append(char.ToLowerInvariant(text[0]));

        for (int i = 1; i < text.Length; i++)
        {
            var c = text[i];
            if (char.IsUpper(c))
            {
                sb.Append('_');
                sb.Append(char.ToLowerInvariant(c));
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }

    public static string ToPascalCase(this string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        Span<char> buffer = stackalloc char[input.Length];
        int pos = 0;
        bool toUpper = true;

        foreach (var c in input)
        {
            if (!char.IsLetterOrDigit(c))
            {
                toUpper = true;
                continue;
            }

            buffer[pos++] = toUpper
                ? char.ToUpperInvariant(c)
                : char.ToLowerInvariant(c);

            toUpper = false;
        }

        return new string(buffer[..pos]);
    }
}