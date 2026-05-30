using AwsDoc4.Models;
using MaterialDesignThemes.Wpf;
using R3;

namespace AwsDoc4.ViewModels
{
    public class ThemeSettingsViewModel : ViewModelBase
    {
        public BindableReactiveProperty<bool> IsDarkTheme { get; }
        public BindableReactiveProperty<bool> IsColorAdjusted { get; }
        public BindableReactiveProperty<float> DesiredContrastRatio { get; }
        public BindableReactiveProperty<Contrast> ContrastValue { get; }
        public BindableReactiveProperty<ColorSelection> ColorSelectionValue { get; }
        public IEnumerable<Contrast> ContrastValues => Enum.GetValues(typeof(Contrast)).Cast<Contrast>();
        public IEnumerable<ColorSelection> ColorSelectionValues => Enum.GetValues(typeof(ColorSelection)).Cast<ColorSelection>();

        public ThemeSettingsViewModel() : base()
        {
            var conf = App.GetService<AppConfig>()!;
            PaletteHelper paletteHelper = new PaletteHelper();
            Theme theme = paletteHelper.GetTheme();
            this.DesiredContrastRatio = new BindableReactiveProperty<float>(4.5f);
            this.DesiredContrastRatio.Subscribe(x =>
            {
                ModifyTheme(theme =>
                {
                    if (theme is Theme internalTheme && internalTheme.ColorAdjustment != null)
                    {
                        internalTheme.ColorAdjustment.DesiredContrastRatio = x;
                    }
                });
                App.GetService<AppConfig>()!.Save();
            });

            this.ContrastValue = new BindableReactiveProperty<Contrast>();
            this.ContrastValue.Subscribe(x =>
            {
                ModifyTheme(theme =>
                {
                    if (theme is Theme internalTheme && internalTheme.ColorAdjustment != null)
                    {
                        internalTheme.ColorAdjustment.Contrast = x;
                    }
                });
                App.GetService<AppConfig>()!.Save();
            });
            this.ColorSelectionValue = new BindableReactiveProperty<ColorSelection>();
            this.ColorSelectionValue.Subscribe(x =>
            {
                ModifyTheme(theme =>
                {
                    if (theme is Theme internalTheme && internalTheme.ColorAdjustment != null)
                    {
                        internalTheme.ColorAdjustment.Colors = x;
                    }
                });
                App.GetService<AppConfig>()!.Save();
            });
            this.IsDarkTheme = new BindableReactiveProperty<bool>(theme.GetBaseTheme() == BaseTheme.Dark);
            this.IsDarkTheme.Subscribe(x =>
            {
                ModifyTheme(theme => theme.SetBaseTheme(x ? BaseTheme.Dark : BaseTheme.Light));
                App.GetService<AppConfig>()!.Save();
            });
            this.IsColorAdjusted = new BindableReactiveProperty<bool>();
            this.IsColorAdjusted.Subscribe(x =>
            {
                ModifyTheme(theme =>
                {
                    if (theme is Theme internalTheme)
                    {
                        internalTheme.ColorAdjustment = x
                            ? new ColorAdjustment
                            {
                                DesiredContrastRatio = this.DesiredContrastRatio.Value,
                                Contrast = this.ContrastValue.Value,
                                Colors = this.ColorSelectionValue.Value,
                            }
                            : null;
                    }
                });
                App.GetService<AppConfig>()!.Save();
            });

            if (theme is Theme internalTheme)
            {
                this.IsColorAdjusted.Value = internalTheme.ColorAdjustment is not null;

                var colorAdjustment = internalTheme.ColorAdjustment ?? new ColorAdjustment();
                this.DesiredContrastRatio.Value = colorAdjustment.DesiredContrastRatio;
                this.ContrastValue.Value = colorAdjustment.Contrast;
                this.ColorSelectionValue.Value = colorAdjustment.Colors;
            }

            if (paletteHelper.GetThemeManager() is { } themeManager)
            {
                themeManager.ThemeChanged += (_, e) =>
                {
                    IsDarkTheme.Value = e.NewTheme?.GetBaseTheme() == BaseTheme.Dark;
                };
            }
        }

        private static void ModifyTheme(Action<Theme> modificationAction)
        {
            var paletteHelper = new PaletteHelper();
            Theme theme = paletteHelper.GetTheme();
            modificationAction?.Invoke(theme);
            paletteHelper.SetTheme(theme);
        }
    }
}
