using AwsDoc4.Models;
using MaterialDesignColors;
using MaterialDesignThemes.Wpf;
using R3;
using System.Windows.Media;

namespace AwsDoc4.ViewModels
{
    internal class ColorToolBoxViewModel : BoxViewModelBase
    {
        private readonly PaletteHelper _paletteHelper = new PaletteHelper();
        public ReactiveCommand<Color> ChangeCustomHueCommand { get; }
        public ReactiveCommand<Color> ChangeHueCommand { get; }
        public ReactiveCommand ChangeToPrimaryCommand { get; }
        public ReactiveCommand ChangeToSecondaryCommand { get; }
        public ReactiveCommand ChangeToPrimaryForegroundCommand { get; }
        public ReactiveCommand ChangeToSecondaryForegroundCommand { get; }
        public ReactiveCommand<bool> ToggleBaseCommand { get; }

        public BindableReactiveProperty<Color?> SelectedColor { get; }
        public IEnumerable<ISwatch> Swatches { get; } = SwatchHelper.Swatches;

        public BindableReactiveProperty<ColorScheme> ActiveScheme { get; }

        public Color? PrimaryColor { get; set; }

        public Color? SecondaryColor { get; set; }

        public Color? PrimaryForegroundColor { get; set; }
        public Color? SecondaryForegroundColor { get; set; }

        private void ApplyBase(bool isDark)
        {
            Theme theme = _paletteHelper.GetTheme();
            theme.SetBaseTheme(isDark ? BaseTheme.Dark : BaseTheme.Light);
            _paletteHelper.SetTheme(theme);
            App.GetService<AppConfig>()!.Save();
        }

        public ColorToolBoxViewModel() : base()
        {
            var conf = App.GetService<AppConfig>()!;
            this.SelectedColor = new BindableReactiveProperty<Color?>();
            this.ActiveScheme = new BindableReactiveProperty<ColorScheme>(ColorScheme.Primary);
            this.ToggleBaseCommand = new ReactiveCommand<bool>();
            this.ToggleBaseCommand.Subscribe(x => ApplyBase((bool)x!));
            this.ChangeHueCommand = new ReactiveCommand<Color>();
            this.ChangeHueCommand.Subscribe(x => ChangeHue(x));
            this.ChangeCustomHueCommand = new ReactiveCommand<Color>();
            this.ChangeCustomHueCommand.Subscribe(x => ChangeCustomColor(x));
            this.ChangeToPrimaryCommand = new ReactiveCommand();
            this.ChangeToPrimaryCommand.Subscribe(_ => ChangeScheme(ColorScheme.Primary));
            this.ChangeToSecondaryCommand = new ReactiveCommand();
            this.ChangeToSecondaryCommand.Subscribe(_ => ChangeScheme(ColorScheme.Secondary));
            this.ChangeToPrimaryForegroundCommand = new ReactiveCommand();
            this.ChangeToPrimaryForegroundCommand.Subscribe(_ => ChangeScheme(ColorScheme.PrimaryForeground));
            this.ChangeToSecondaryForegroundCommand = new ReactiveCommand();
            this.ChangeToSecondaryForegroundCommand.Subscribe(_ => ChangeScheme(ColorScheme.SecondaryForeground));

            this.SelectedColor.Subscribe(x =>
            {
                var currentSchemeColor = ActiveScheme.Value switch
                {
                    ColorScheme.Primary => this.PrimaryColor,
                    ColorScheme.Secondary => this.SecondaryColor,
                    ColorScheme.PrimaryForeground => this.PrimaryForegroundColor,
                    ColorScheme.SecondaryForeground => this.SecondaryForegroundColor,
                    _ => throw new NotSupportedException($"{ActiveScheme} is not a handled ColorScheme.. Ye daft programmer!")
                };

                if (x.HasValue)
                {
                    ChangeCustomColor(x.Value);
                }
            });

            Theme theme = _paletteHelper.GetTheme();

            this.PrimaryColor = theme.PrimaryMid.Color;
            this.SecondaryColor = theme.SecondaryMid.Color;
            SelectedColor.Value = this.PrimaryColor;
        }

        private void ChangeCustomColor(Color color)
        {
            Theme theme = _paletteHelper.GetTheme();
            if (ActiveScheme.Value == ColorScheme.Primary)
            {
                _paletteHelper.ChangePrimaryColor(color);
                this.PrimaryColor = color;
            }
            else if (ActiveScheme.Value == ColorScheme.Secondary)
            {
                _paletteHelper.ChangeSecondaryColor(color);
                this.SecondaryColor = color;
            }
            else if (ActiveScheme.Value == ColorScheme.PrimaryForeground)
            {
                _paletteHelper.SetPrimaryForegroundToSingleColor(color);
                this.PrimaryForegroundColor = color;
            }
            else if (ActiveScheme.Value == ColorScheme.SecondaryForeground)
            {
                _paletteHelper.SetSecondaryForegroundToSingleColor(color);
                this.SecondaryForegroundColor = color;
            }
            RaisePropertyChanged(nameof(ActiveScheme));
            _paletteHelper.SetTheme(theme);
            App.GetService<AppConfig>()!.Save();
        }

        private void ChangeScheme(ColorScheme scheme)
        {
            this.ActiveScheme.Value = scheme;
            if (ActiveScheme.Value == ColorScheme.Primary)
            {
                SelectedColor.Value = this.PrimaryColor;
            }
            else if (ActiveScheme.Value == ColorScheme.Secondary)
            {
                SelectedColor.Value = this.SecondaryColor;
            }
            else if (ActiveScheme.Value == ColorScheme.PrimaryForeground)
            {
                SelectedColor.Value = this.PrimaryForegroundColor;
            }
            else if (ActiveScheme.Value == ColorScheme.SecondaryForeground)
            {
                SelectedColor.Value = this.SecondaryForegroundColor;
            }
            RaisePropertyChanged(nameof(ActiveScheme));
        }

        private void ChangeHue(Color hue)
        {
            Theme theme = _paletteHelper.GetTheme();
            SelectedColor.Value = hue;
            if (ActiveScheme.Value == ColorScheme.Primary)
            {
                _paletteHelper.ChangePrimaryColor(hue);
                this.PrimaryColor = hue;
                this.PrimaryForegroundColor = _paletteHelper.GetTheme().PrimaryMid.GetForegroundColor();
            }
            else if (ActiveScheme.Value == ColorScheme.Secondary)
            {
                _paletteHelper.ChangeSecondaryColor(hue);
                this.SecondaryColor = hue;
                this.SecondaryForegroundColor = _paletteHelper.GetTheme().SecondaryMid.GetForegroundColor();
            }
            else if (ActiveScheme.Value == ColorScheme.PrimaryForeground)
            {
                _paletteHelper.SetPrimaryForegroundToSingleColor(hue);
                this.PrimaryForegroundColor = hue;
            }
            else if (ActiveScheme.Value == ColorScheme.SecondaryForeground)
            {
                _paletteHelper.SetSecondaryForegroundToSingleColor(hue);
                this.SecondaryForegroundColor = hue;
            }
            _paletteHelper.SetTheme(theme);
            RaisePropertyChanged(nameof(ActiveScheme));
            App.GetService<AppConfig>()!.Save();
        }
    }
}