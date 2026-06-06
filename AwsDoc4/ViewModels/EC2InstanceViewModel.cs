using AwsDoc4.Resources;
using AwsDoc4.Services;
using R3;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace AwsDoc4.ViewModels;

public class EC2InstanceViewModel:ViewModelBase
{
    public EC2Resource Instance { get; }
    public BindableReactiveProperty<string> State { get; }
    public ReactiveCommand RefreshCommand { get; }
    public BindableReactiveProperty<bool> IsOpened { get; }
    public BindableReactiveProperty<string> Text { get; }
    public EC2InstanceViewModel(EC2Resource resource)
    {
        this.Instance = resource;
        this.Text = new BindableReactiveProperty<string>($"{this.Instance.Name}\t{this.Instance.State}");
        this.IsOpened = new BindableReactiveProperty<bool>();
        this.State = new BindableReactiveProperty<string>(this.Instance.State ?? string.Empty);
        this.State.Subscribe(x =>
        {
            this.IsOpened.Value = "running".Equals(x);
            this.Text.Value = $"{this.Instance.Name}\t{this.Instance.State}";
        });

        this.RefreshCommand = new ReactiveCommand();
        this.RefreshCommand.SubscribeAwait(async (x, ct) =>
        {
            var profile = App.GetService<IAwsProfileManager>()!.CurrentProfile!;
            await this.Instance.RefreshAsync(profile, CancellationToken.None);
            this.State.Value = this.Instance.State ?? string.Empty;
        });
    }
    
}
