using AwsDoc4.Models;
using AwsDoc4.Services;
using MaterialDesignColors;
using MaterialDesignColors.ColorManipulation;
using System.Windows.Media;

namespace MaterialDesignThemes.Wpf
{
    public static class PaletteHelperExtensions
    {
        public static void ChangePrimaryColor(this PaletteHelper paletteHelper, Color? color)
        {
            if (!color.HasValue)
            {
                return;
            }
            Theme theme = paletteHelper.GetTheme();

            theme.PrimaryLight = new ColorPair(color.Value.Lighten());
            theme.PrimaryMid = new ColorPair(color.Value);
            theme.PrimaryDark = new ColorPair(color.Value.Darken());

            paletteHelper.SetTheme(theme);
        }
        public static void ChangeSecondaryColor(this PaletteHelper paletteHelper, Color? color)
        {
            if (!color.HasValue)
            {
                return;
            }
            Theme theme = paletteHelper.GetTheme();

            theme.SecondaryLight = new ColorPair(color.Value.Lighten());
            theme.SecondaryMid = new ColorPair(color.Value);
            theme.SecondaryDark = new ColorPair(color.Value.Darken());

            paletteHelper.SetTheme(theme);
        }

        public static void SetSecondaryForegroundToSingleColor(this PaletteHelper paletteHelper, Color? color)
        {
            if (!color.HasValue)
            {
                return;
            }
            Theme theme = paletteHelper.GetTheme();

            theme.SecondaryLight = new ColorPair(theme.SecondaryLight.Color, color);
            theme.SecondaryMid = new ColorPair(theme.SecondaryMid.Color, color);
            theme.SecondaryDark = new ColorPair(theme.SecondaryDark.Color, color);

            paletteHelper.SetTheme(theme);
        }
        public static void SetPrimaryForegroundToSingleColor(this PaletteHelper paletteHelper, Color? color)
        {
            if (!color.HasValue)
            {
                return;
            }
            Theme theme = paletteHelper.GetTheme();

            theme.PrimaryLight = new ColorPair(theme.PrimaryLight.Color, color);
            theme.PrimaryMid = new ColorPair(theme.PrimaryMid.Color, color);
            theme.PrimaryDark = new ColorPair(theme.PrimaryDark.Color, color);

            paletteHelper.SetTheme(theme);
        }

        public static ThemeConfig GetConfig(this PaletteHelper paletteHelper)
        {
            Theme theme = paletteHelper.GetTheme();
            var isColorAdjustment = theme.ColorAdjustment != null;

            return new ThemeConfig()
            {
                ColorSelection = isColorAdjustment ? theme.ColorAdjustment!.Colors : ColorSelection.All,
                IsColorAdjusted = isColorAdjustment,
                ContrastValue = isColorAdjustment ? theme.ColorAdjustment!.Contrast : Contrast.None,
                DesiredContrastRatio = isColorAdjustment ? theme.ColorAdjustment!.DesiredContrastRatio : 4.5f,
                IsDarkTheme = theme.GetBaseTheme() == BaseTheme.Dark,
                Colors = new ColorConfig[]
               {
                    new ColorConfig() { Color = theme.PrimaryMid.Color, Scheme = ColorScheme.Primary },
                    new ColorConfig() { Color = theme.SecondaryMid.Color, Scheme = ColorScheme.Secondary },
                    new ColorConfig() { Color = theme.PrimaryMid.Color, Scheme = ColorScheme.PrimaryForeground },
                    new ColorConfig() { Color = theme.SecondaryMid.Color, Scheme = ColorScheme.SecondaryForeground },
               }
            };
        }
        public static void SetConfig(this PaletteHelper paletteHelper, ThemeConfig config)
        {
            Theme theme = paletteHelper.GetTheme();
            if (config.PrimaryColor.HasValue)
            {
                theme.PrimaryLight = new ColorPair(config.PrimaryColor.Value.Lighten());
                theme.PrimaryMid = new ColorPair(config.PrimaryColor.Value);
                theme.PrimaryDark = new ColorPair(config.PrimaryColor.Value.Darken());
            }
            if (config.SecondaryColor.HasValue)
            {
                theme.SecondaryLight = new ColorPair(theme.SecondaryLight.Color, config.SecondaryColor.Value.Lighten());
                theme.SecondaryMid = new ColorPair(theme.SecondaryMid.Color, config.SecondaryColor.Value);
                theme.SecondaryDark = new ColorPair(theme.SecondaryDark.Color, config.SecondaryColor.Value.Darken());
            }
            if (config.PrimaryForegroundColor.HasValue)
            {
                theme.PrimaryLight = new ColorPair(theme.PrimaryLight.Color, config.PrimaryForegroundColor.Value);
                theme.PrimaryMid = new ColorPair(theme.PrimaryMid.Color, config.PrimaryForegroundColor);
                theme.PrimaryDark = new ColorPair(theme.PrimaryDark.Color, config.PrimaryForegroundColor);

            }
            if (config.SecondaryForegroundColor.HasValue)
            {
                theme.SecondaryLight = new ColorPair(theme.SecondaryLight.Color, config.SecondaryForegroundColor.Value);
                theme.SecondaryMid = new ColorPair(theme.SecondaryMid.Color, config.SecondaryForegroundColor.Value);
                theme.SecondaryDark = new ColorPair(theme.SecondaryDark.Color, config.SecondaryForegroundColor.Value);
            }
            theme.ColorAdjustment = config.IsColorAdjusted
                ? new ColorAdjustment
                {
                    DesiredContrastRatio = config.DesiredContrastRatio,
                    Contrast = config.ContrastValue,
                    Colors = config.ColorSelection,
                }
                : null;
            paletteHelper.SetTheme(theme);
        }
    }
}