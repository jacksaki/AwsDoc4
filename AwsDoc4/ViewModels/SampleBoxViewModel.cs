using AwsDoc4.Models;
using R3;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace AwsDoc4.ViewModels
{
    public class SampleBoxViewModel : BoxViewModelBase
    {
        public BindableReactiveProperty<int> Value { get; }
        public ReactiveCommand IncrementCommand { get; }
        public ReactiveCommand ResetCommand { get; }
        public BindableReactiveProperty<string> SnackBarMessageText { get; }
        public BindableReactiveProperty<string> MessageTitle { get; }
        public BindableReactiveProperty<string> MessageText { get; }
        public BindableReactiveProperty<bool> CanSendMessage { get; }
        public BindableReactiveProperty<bool> IsError { get; }
        public ReactiveCommand SendMessageCommand { get; }
        public ReactiveCommand<string> SendSnackBarMessageCommand { get; }
        public SampleBoxViewModel(MainWindowViewModel parent) : base()
        {
            var conf = App.GetService<AppConfig>()!;
            this.Value = new BindableReactiveProperty<int>(0);
            this.IncrementCommand = new ReactiveCommand();
            this.IncrementCommand.Subscribe(_ =>
            {
                this.Value.Value++;
                //conf.Save(this);
            }
            );
            this.ResetCommand = new ReactiveCommand();
            this.ResetCommand.Subscribe(_ => this.Value.Value = 0);
            this.SnackBarMessageText = new BindableReactiveProperty<string>();
            this.SendSnackBarMessageCommand = this.SnackBarMessageText.Select(x => !string.IsNullOrEmpty(x)).ToReactiveCommand<string>();
            this.SendSnackBarMessageCommand.Subscribe(x =>
            {
                this.OnSnackBarMessage(new SnackBarMessageEventArgs(x));
            });
            this.IsError = new BindableReactiveProperty<bool>(false);
            this.MessageText = new BindableReactiveProperty<string>();
            this.MessageTitle = new BindableReactiveProperty<string>();
            this.CanSendMessage = this.MessageTitle.CombineLatest(this.MessageText, (title, message) =>
            !string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(message)).ToBindableReactiveProperty();
            this.SendMessageCommand = this.CanSendMessage.ToReactiveCommand();
            this.SendMessageCommand.Subscribe(x =>
            {
                if (this.IsError.Value)
                {
                    this.OnErrorOccurred(new ErrorOccurredEventArgs(this.MessageTitle.Value, this.MessageText.Value));
                }
                else
                {
                    this.OnMessage(new MessageEventArgs(this.MessageTitle.Value, this.MessageText.Value));
                }
            });
            //conf.Load(this);
        }
    }
}
