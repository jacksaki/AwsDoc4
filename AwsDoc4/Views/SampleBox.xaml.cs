using AwsDoc4.ViewModels;
using System.Windows.Controls;

namespace AwsDoc4.Views
{
    /// <summary>
    /// SampleBox.xaml の相互作用ロジック
    /// </summary>
    public partial class SampleBox : UserControl
    {
        public SampleBox()
        {
            InitializeComponent();
            this.DataContext = App.GetService<SampleBoxViewModel>();
        }
    }
}
