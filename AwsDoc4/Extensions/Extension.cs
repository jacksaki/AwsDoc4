using System.Reflection;
using System.Windows.Media;

namespace AwsDoc4.Extensions
{
    public static class Extension
    {
        public static string? GetVersion(this Assembly asm)
        {
            var version = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            if (version != null)
            {
                int plusIndex = version.IndexOf('+');
                if (plusIndex != -1)
                {
                    return version.Substring(0, plusIndex);
                }
            }

            return version;
        }

        public static string? ToHtmlColor(this Color? value)
        {
            if (value is null)
            {
                return null;
            }

            static string lowerHexString(int i) => i.ToString("X2").ToLower();
            var hex = lowerHexString(value.Value.R) +
                      lowerHexString(value.Value.G) +
                      lowerHexString(value.Value.B);
            return "#" + hex;
        }

        public static Color? ToColor(this string code)
        {
            try
            {
                return (Color)ColorConverter.ConvertFromString(code);
            }
            catch (FormatException)
            {
                return null;
            }
        }
    }
}
