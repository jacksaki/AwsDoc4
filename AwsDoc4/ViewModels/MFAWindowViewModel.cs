using AwsDoc4.Resources;
using R3;
using System;
using System.Collections.Generic;
using System.Text;

namespace AwsDoc4.ViewModels;

public class MFAWindowViewModel:ViewModelBase
{
    public BindableReactiveProperty<AwsProfile> Profile { get; }
    public BindableReactiveProperty<string> MFACode { get; }
    public ReactiveCommand OKCommand { get; }
    public ReactiveCommand CancelCommand { get; }
    public bool? DialogResult { get; set; }
    public MFAWindowViewModel() : base()
    {
        this.Profile = new BindableReactiveProperty<AwsProfile>();
        this.MFACode = new BindableReactiveProperty<string>();
        this.OKCommand = this.MFACode.Select(x => !string.IsNullOrEmpty(x)).ToReactiveCommand();
        this.OKCommand.Subscribe(_ =>
        {
            this.DialogResult = true;
            RaisePropertyChanged(nameof(DialogResult));
        });
        this.CancelCommand = new ReactiveCommand();
        this.CancelCommand.Subscribe(_ =>
        {
            this.DialogResult = false;
            RaisePropertyChanged(nameof(DialogResult));
        });
    }

}
