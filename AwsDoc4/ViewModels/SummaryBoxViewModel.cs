using AwsDoc4.Views;
using MaterialDesignThemes.Wpf;
using ObservableCollections;
using R3;
using System.Windows.Input;

namespace AwsDoc4.ViewModels;

public class SummaryBoxViewModel:BoxViewModelBase
{
    public BindableReactiveProperty<string> ExcelPath { get; }
    public ReactiveCommand SaveDlgCommand { get; }
    public ReactiveCommand ExecuteCommand { get; }
    public BindableReactiveProperty<string?> TokenCode { get; }
    public ObservableList<AwsResourceViewModel> Resources { get; }
    public ISynchronizedView<AwsResourceViewModel, AwsResourceViewModel> ResourcesView { get; }
    public NotifyCollectionChangedSynchronizedViewList<AwsResourceViewModel> FilteredView { get; }
    public BindableReactiveProperty<AwsResourceViewModel> SelectedResource { get; }
    public ReactiveCommand AddResourceCommand { get; }
    public ReactiveCommand RemoveSelectedResourceCommand { get; }
    public SummaryBoxViewModel() : base()
    {
        this.ExcelPath = new BindableReactiveProperty<string>();
        this.TokenCode=new BindableReactiveProperty<string?>();
        this.SaveDlgCommand = new ReactiveCommand();
        this.Resources = new ObservableList<AwsResourceViewModel>();
        this.ResourcesView = this.Resources.CreateView<AwsResourceViewModel>(x => x);
        this.SelectedResource = new BindableReactiveProperty<AwsResourceViewModel>();
        this.SelectedResource.SubscribeAwait(async (x, ct) =>
        {
            if (x != null)
            {
                Mouse.OverrideCursor = Cursors.Wait;
                try
                {
                    if (!x.Resource.Value.IsLoaded)
                    {
                        await x.RefreshAsync(ct);
                    }
                }
                finally
                {
                    Mouse.OverrideCursor = null;
                }
            }
        });

        this.AddResourceCommand = new ReactiveCommand();
        this.RemoveSelectedResourceCommand = new ReactiveCommand();
        this.ExecuteCommand = new ReactiveCommand();
        this.AddResourceCommand.Subscribe(_ =>
        {
            var window = App.GetService<SelectResourceWindow>()!;
            if (window.ShowDialog()==true)
            {
                var viewModel = App.GetService<AwsResourceViewModel>() !;
                viewModel.Resource.Value = ((SelectResourceWindowViewModel)(window.DataContext)).SelectedResource.Value;
                this.Resources.Add(viewModel);
            }
        });
        this.FilteredView = this.ResourcesView.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);
    }
}
