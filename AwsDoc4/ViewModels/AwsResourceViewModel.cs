using Amazon.Auth.AccessControlPolicy;
using AwsDoc4.Resources;
using AwsDoc4.Services;
using R3;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace AwsDoc4.ViewModels;

public class AwsResourceViewModel:BoxViewModelBase
{
    public List<PropertyGroupViewModel> Groups { get; private set; } = new List<PropertyGroupViewModel>();
    public BindableReactiveProperty<int> SelectedIndex { get; }
    public BindableReactiveProperty<AwsResourceBase> Resource { get; }
    public AwsResourceViewModel():base()
    {
        this.SelectedIndex = new BindableReactiveProperty<int>();
        this.Resource = new BindableReactiveProperty<AwsResourceBase>();
        this.Resource.SubscribeAwait(async (x, ct) =>
        {
            if (x != null)
            {
                this.Groups = PropertyGroupViewModel.Build(x);
            }
        });
    }
    public async Task RefreshAsync(CancellationToken ct = default)
    {
        var profile = App.GetService<IAwsProfileManager>()!.CurrentProfile!;
        await this.Resource.Value.RefreshAsync(profile, ct);
        this.SelectedIndex.Value = 0;
    }
}
