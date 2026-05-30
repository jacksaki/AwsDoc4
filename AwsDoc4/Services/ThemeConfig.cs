using AwsDoc4.Extensions;
using AwsDoc4.Models;
using MaterialDesignThemes.Wpf;
using System.Text.Json.Serialization;
using System.Windows.Media;

namespace AwsDoc4.Services
{
    public class ThemeConfig
    {
        public static ThemeConfig CreateDefault()
        {
            return new ThemeConfig()
            {
                IsDarkTheme = false,
                ContrastValue = Contrast.Medium,
                ColorSelection = ColorSelection.All,
                IsColorAdjusted = false,
                DesiredContrastRatio = 4.5f,
                Colors = new ColorConfig[]
                {
                    new ColorConfig(){Scheme = ColorScheme.Primary, Color = "#FF673AB7".ToColor()},
                    new ColorConfig(){Scheme = ColorScheme.Primary, Color = "#FFAEEA00".ToColor()},
                    new ColorConfig(){Scheme = ColorScheme.Primary, Color = null},
                    new ColorConfig(){Scheme = ColorScheme.Primary, Color = null},
                }
            };
        }
        [JsonPropertyName("is_dark")]
        public bool IsDarkTheme { get; set; }
        [JsonPropertyName("is_color_adjusted")]
        public bool IsColorAdjusted { get; set; }
        [JsonPropertyName("desired_contrast_ratio")]
        public float DesiredContrastRatio { get; set; }
        [JsonPropertyName("contrast_value")]
        public Contrast ContrastValue { get; set; } = Contrast.Medium;
        [JsonPropertyName("color_selection")]
        public ColorSelection ColorSelection { get; set; } = ColorSelection.All;
        [JsonPropertyName("colors")]
        public ColorConfig[]? Colors { get; set; }
        [JsonIgnore]
        public Color? PrimaryColor => this.Colors?.Where(x => x.Scheme == ColorScheme.Primary).FirstOrDefault()?.Color;
        [JsonIgnore]
        public Color? SecondaryColor => this.Colors?.Where(x => x.Scheme == ColorScheme.Secondary).FirstOrDefault()?.Color;
        [JsonIgnore]
        public Color? PrimaryForegroundColor => this.Colors?.Where(x => x.Scheme == ColorScheme.PrimaryForeground).FirstOrDefault()?.Color;
        [JsonIgnore]
        public Color? SecondaryForegroundColor => this.Colors?.Where(x => x.Scheme == ColorScheme.SecondaryForeground).FirstOrDefault()?.Color;
    }
}
