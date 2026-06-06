using AwsDoc4.Models;
using AwsDoc4.Resources;
using AwsDoc4.Services;
using ObservableCollections;
using R3;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace AwsDoc4.ViewModels;

public class EC2CommandBoxViewModel: BoxViewModelBase
{

    public ObservableList<EC2CommandLog> Logs { get; }
    public ISynchronizedView<EC2CommandLog, EC2CommandLog> LogsView { get; }
    public NotifyCollectionChangedSynchronizedViewList<EC2CommandLog> FilteredLogsView { get; }

    public ObservableList<EC2InstanceViewModel> Instances { get; }
    public ISynchronizedView<EC2InstanceViewModel, EC2InstanceViewModel> InstancesView { get; }
    public NotifyCollectionChangedSynchronizedViewList<EC2InstanceViewModel> FilteredInstancesView { get; }
    public ReactiveCommand RefreshInstancesCommand { get; }
    public BindableReactiveProperty<string> CommandText { get; }
    public BindableReactiveProperty<EC2InstanceViewModel> SelectedInstance { get; }
    public ReactiveCommand SendCommand { get; }
    public ReactiveCommand CopyAllCommand { get; }
    public EC2CommandBoxViewModel() : base()
    {
        this.Instances = new ObservableList<EC2InstanceViewModel>();
        this.InstancesView = this.Instances.CreateView(x => x);
        this.FilteredInstancesView= this.InstancesView.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);
        this.SelectedInstance = new BindableReactiveProperty<EC2InstanceViewModel>();

        this.Logs = new ObservableList<EC2CommandLog>();
        this.LogsView = this.Logs.CreateView(x => x);
        this.FilteredLogsView = this.LogsView.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);
        this.CommandText = new BindableReactiveProperty<string>();
        this.SendCommand = this.SelectedInstance.CombineLatest(
            this.CommandText, (x, y) => x != null && x.IsOpened.Value && !string.IsNullOrEmpty(y)).ToReactiveCommand();
        this.SendCommand.SubscribeAwait(async (x, ct) =>
        {
            var cmd = new EC2CommandExecutor();
            var result = await cmd.ExecuteAsync(this.SelectedInstance.Value.Instance, this.CommandText.Value);
            if (result != null)
            {
                if(!string.IsNullOrEmpty(result.StdOut))
                {
                    var lines = result.StdOut.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                    foreach(var line in lines)
                    {
                        this.Logs.Add(new EC2CommandLog(line, DateTime.Now));
                    }
                }
                else
                {
                    var lines = result.StdErr.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                    foreach (var line in lines)
                    {
                        this.Logs.Add(new EC2CommandLog($"Error: {line}", DateTime.Now));
                    }
                }
            }
        });

        this.CopyAllCommand = new ReactiveCommand();
        this.CopyAllCommand.Subscribe(_ =>
        {
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                Clipboard.SetText(string.Join("\r\n", this.Logs));
                OnSnackBarMessage(new SnackBarMessageEventArgs("コピーしました"));
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        });
        this.RefreshInstancesCommand = new ReactiveCommand();
        this.RefreshInstancesCommand.SubscribeAwait(async (x, ct) =>
        {
            Mouse.OverrideCursor = Cursors.Wait;
            this.Instances.Clear();
            try
            {
                await foreach (var instance in AwsResourceSearcher.EnumerateResourceAsync(typeof(EC2Resource), new EnumerateResourceRequest(), CancellationToken.None))
                {
                    this.Instances.Add(new EC2InstanceViewModel((EC2Resource)instance));
                }
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        });
    }
}
