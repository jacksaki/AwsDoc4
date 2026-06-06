using AwsDoc4.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AwsDoc4.Views
{
    /// <summary>
    /// EC2CommandBox.xaml の相互作用ロジック
    /// </summary>
    public partial class EC2CommandBox : UserControl
    {
        public EC2CommandBox()
        {
            InitializeComponent();
            this.DataContext = App.GetService<EC2CommandBoxViewModel>();
        }
    }
}
