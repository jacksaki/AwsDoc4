using AwsDoc4.Models;

namespace AwsDoc4.ViewModels
{
    public class BoxViewModelBase : ViewModelBase
    {

        private readonly MainWindowViewModel _parent;
        public BoxViewModelBase()
        {
            _parent = App.GetService<MainWindowViewModel>()!;
        }

        public void OnErrorOccurred(ErrorOccurredEventArgs e)
        {
            _parent.OnErrorOccurred(this, e);
        }

        public void OnMessage(MessageEventArgs e)
        {
            _parent.OnMessage(this, e);
        }

        public void OnSnackBarMessage(SnackBarMessageEventArgs e)
        {
            _parent.OnSnackBarMessage(this, e);
        }
    }
}
