using AwsDoc4.Resources;
using AwsDoc4.Services;
using MahApps.Metro.Controls.Dialogs;
using ObservableCollections;
using R3;
using System;
using System.Collections.Generic;
using System.Text;

namespace AwsDoc4.ViewModels;

public class LoginWindowViewModel:ViewModelBase
{
    public ObservableList<AwsProfile> Profiles { get; }
    public ISynchronizedView<AwsProfile, AwsProfile> SyncView{ get; }
    public NotifyCollectionChangedSynchronizedViewList<AwsProfile> ProfilesView { get; }
    public BindableReactiveProperty<AwsProfile> SelectedProfile { get; }
    public BindableReactiveProperty<string> MFACode { get; }
    public ReactiveCommand OKCommand { get; }
    public ReactiveCommand CancelCommand { get; }
    public bool? DialogResult { get; set; }
    public BindableReactiveProperty<bool> NeedMFA { get; }

    public IDialogCoordinator? DialogCoordinator
    {
        get;
        set;
    }
    public LoginWindowViewModel() : base()
    {
        this.DialogCoordinator = MahApps.Metro.Controls.Dialogs.DialogCoordinator.Instance;
        this.Profiles = new ObservableList<AwsProfile>(AwsProfile.ListProfiles());
        this.SyncView = this.Profiles.CreateView(x => x);
        this.ProfilesView = this.SyncView.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);
        this.MFACode = new BindableReactiveProperty<string>();
        this.SelectedProfile = new BindableReactiveProperty<AwsProfile>();
        this.CancelCommand = new ReactiveCommand();
        this.NeedMFA = new BindableReactiveProperty<bool>();
        this.SelectedProfile.Subscribe(x => this.NeedMFA.Value = x?.NeedMfa ?? false);
        this.OKCommand = this.SelectedProfile.CombineLatest(this.MFACode, 
            (profile, code) => profile != null && (!profile.NeedMfa || !string.IsNullOrWhiteSpace(code))).ToReactiveCommand();

        this.OKCommand.SubscribeAwait(async (_, ct) =>
        {
            var profile = this.SelectedProfile.Value!;
            await profile.ConnectAsync(
                (_, _) => Task.FromResult(this.MFACode.Value),
                ct);
            profile.SetMFAProvider(MfaDialogProvider.GetCodeAsync);

            App.GetService<IAwsProfileManager>()!.CurrentProfile = profile;

            this.DialogResult = true;
            RaisePropertyChanged(nameof(DialogResult));
        });
        
        this.CancelCommand.Subscribe(_ => {
            this.DialogResult = false;
            RaisePropertyChanged(nameof(DialogResult));
        });
    }
}
