using AwsDoc4.Resources;
using AwsDoc4.Services;
using ObservableCollections;
using R3;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Windows.Input;

namespace AwsDoc4.ViewModels;

public class SelectResourceWindowViewModel:BoxViewModelBase
{
    private Dictionary<string, Type> _resourceTypes;
    public List<string> Types { get; }
    public bool? DialogResult { get; set; }
    public BindableReactiveProperty<string> SelectedType { get; }
    public ReactiveCommand<string> RefreshResourcesCommand { get; }
    public BindableReactiveProperty<string> SearchString { get; }
    public ObservableList<AwsResourceBase> Resources { get; }
    public ISynchronizedView<AwsResourceBase, AwsResourceBase> ResourcesView { get; }
    public NotifyCollectionChangedSynchronizedViewList<AwsResourceBase> FilteredView { get; }

    public BindableReactiveProperty<AwsResourceBase> SelectedResource { get; }
    public ReactiveCommand<AwsResourceBase> SelectResourceCommand { get; }

    public SelectResourceWindowViewModel() : base()
    {
        _resourceTypes = typeof(AwsResourceBase).Assembly.GetTypes().Where(x =>
            x.IsSubclassOf(typeof(AwsResourceBase)) &&
            x.GetCustomAttribute<AwsResourceAttribute>() != null
            ).ToDictionary(x => x.GetCustomAttribute<AwsResourceAttribute>()!.Type, y => y);

        this.Types = _resourceTypes.Select(x => x.Key).ToList();
        this.SelectedType = new BindableReactiveProperty<string>();
        this.RefreshResourcesCommand = new ReactiveCommand<string>();
        this.SearchString = new BindableReactiveProperty<string>();
        this.Resources = new ObservableList<AwsResourceBase>();
        this.SelectedResource = new BindableReactiveProperty<AwsResourceBase>();
        this.SelectResourceCommand = new ReactiveCommand<AwsResourceBase>();
        this.ResourcesView = this.Resources.CreateView(x => x);
        this.FilteredView = this.ResourcesView.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);

        this.RefreshResourcesCommand.SubscribeAwait(async (x, ct) =>
        {
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                this.Resources.Clear();
                if (!_resourceTypes.TryGetValue(this.SelectedType.Value, out var t))
                {
                    return;
                }
                await foreach (var resource in AwsResourceSearcher.EnumerateResourceAsync(t, this.SearchString.Value, ct))
                {
                    this.Resources.Add(resource);
                }
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        });
        this.SearchString.Subscribe(x =>
        {
            this.ResourcesView.AttachFilter(MatchesFilter);
        });
        this.SelectResourceCommand.Subscribe(x =>
        {
            if(x==null)
            {
                return;
            }
            this.SelectedResource.Value = x;
            this.DialogResult = true;
            RaisePropertyChanged(nameof(DialogResult));
        });
    }
    private bool MatchesFilter(AwsResourceBase item)
    {
        if (!string.IsNullOrEmpty(this.SearchString.Value) && item.Name?.Contains(this.SearchString.Value, StringComparison.CurrentCultureIgnoreCase) == false)
        {
            return false;
        }

        return true;
    }
}
