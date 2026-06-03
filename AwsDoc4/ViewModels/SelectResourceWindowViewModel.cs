using AwsDoc4.Resources;
using AwsDoc4.Services;
using ObservableCollections;
using R3;
using System.Reflection;
using System.Windows.Input;
using ZLinq;

namespace AwsDoc4.ViewModels;

public class ResourceTypeState
{
    public ResourceTypeState(Type t)
    {
        this.AwsResourceType = t;
        this.HasCreateDate = (bool?)t.GetProperty("HasCreateDate")?.GetValue(null) == true;
        this.HasLastModified = (bool?)t.GetProperty("HasLastModified")?.GetValue(null) == true;
    }

    public Type AwsResourceType { get; }
    public bool HasCreateDate { get; }
    public bool HasLastModified { get; }
}
public class SelectResourceWindowViewModel:BoxViewModelBase
{
    private Dictionary<string, ResourceTypeState> _resourceTypes;
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
    public BindableReactiveProperty<bool> ShowCreateDate { get; }
    public BindableReactiveProperty<bool> ShowLastModified { get; }

    public SelectResourceWindowViewModel() : base()
    {
        _resourceTypes = AwsResourceProvider.GetAll();

        this.Types = _resourceTypes.AsValueEnumerable().Select(x => x.Key).ToList();
        this.SelectedType = new BindableReactiveProperty<string>();
        this.ShowCreateDate = new BindableReactiveProperty<bool>();
        this.ShowLastModified = new BindableReactiveProperty<bool>();
        this.SelectedType.Subscribe(x =>
        {
            if (string.IsNullOrEmpty(x))
            {
                this.ShowCreateDate.Value = false;
                this.ShowLastModified.Value = false;
            }
            else
            {
                this.ShowCreateDate.Value = _resourceTypes.TryGetValue(x, out var vc) ? vc.HasCreateDate : false;
                this.ShowLastModified.Value = _resourceTypes.TryGetValue(x, out var vl) ? vl.HasLastModified : false;
            }
        });

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
                var req = new EnumerateResourceRequest()
                {
                    QueryString = this.SearchString.Value
                };
                await foreach (var resource in AwsResourceSearcher.EnumerateResourceAsync(t.AwsResourceType, req, ct))
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
