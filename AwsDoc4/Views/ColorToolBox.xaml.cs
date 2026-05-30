using AwsDoc4.ViewModels;

namespace AwsDoc4.Views
{
    public partial class ColorToolBox
    {
        public ColorToolBox()
        {
            InitializeComponent();
            this.DataContext = App.GetService<ColorToolBoxViewModel>();
        }
    }
}
