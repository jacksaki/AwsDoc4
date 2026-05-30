using AwsDoc4.ViewModels;

namespace AwsDoc4.Views
{
    public partial class ThemeSettingsBox
    {
        public ThemeSettingsBox()
        {
            InitializeComponent();
            DataContext = App.GetService<ThemeSettingsViewModel>();
        }
    }
}
