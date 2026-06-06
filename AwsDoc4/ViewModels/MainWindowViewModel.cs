using AwsDoc4.Extensions;
using AwsDoc4.Models;
using AwsDoc4.Views;
using MahApps.Metro.Controls.Dialogs;
using MaterialDesignThemes.Wpf;
using R3;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Data;

namespace AwsDoc4.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public delegate void SnackbarMessageEventHandler(object sender, SnackbarMessageEventArgs e);
        public event SnackbarMessageEventHandler SnackbarMessage = delegate { };

        private readonly ICollectionView _menuItemsView;
        public BindableReactiveProperty<int> SelectedIndex { get; }

        public BindableReactiveProperty<string?> SearchKeyword { get; }

        public ObservableCollection<NavigationMenuItem> MenuItems { get; }

        public BindableReactiveProperty<NavigationMenuItem?> SelectedItem { get; }

        public BindableReactiveProperty<bool> ControlsEnabled { get; }

        public ReactiveCommand HomeCommand { get; }
        public ReactiveCommand MovePrevCommand { get; }
        public ReactiveCommand MoveNextCommand { get; }

        public SnackbarMessageQueue SnackbarMessageQueue { get; }

        public IDialogCoordinator? DialogCoordinator
        {
            get;
            set;
        }

        public bool? DialogResult { get; set; }
        public string AppTitle { get; }
        public string AppFullTitle { get; }
        public string AppVersion { get; }

        public MainWindowViewModel()
        {
            this.DialogCoordinator = MahApps.Metro.Controls.Dialogs.DialogCoordinator.Instance;
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetVersion();
            var fv = System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location);
            this.AppTitle = $"{fv.ProductName}";
            this.AppVersion = $"ver {version}";
            this.AppFullTitle = $"{AppTitle} {AppVersion}";
            this.MenuItems = new ObservableCollection<NavigationMenuItem>();
            foreach (var item in GenerateMenuItems())
            {
                this.MenuItems.Add(item);
            }
            this.SelectedIndex = new BindableReactiveProperty<int>(0);
            this.SearchKeyword = new BindableReactiveProperty<string?>();
            this.SelectedItem = new BindableReactiveProperty<NavigationMenuItem?>();
            this.ControlsEnabled = new BindableReactiveProperty<bool>();

            this.SelectedItem.Value = this.MenuItems.FirstOrDefault();
            _menuItemsView = CollectionViewSource.GetDefaultView(MenuItems);
            _menuItemsView.Filter = MenuItemsFilter;

            this.SelectedIndex = new BindableReactiveProperty<int>(0);
            this.HomeCommand = new ReactiveCommand();
            this.HomeCommand.Subscribe(
                _ =>
                {
                    this.SearchKeyword.Value = string.Empty;
                    this.SelectedIndex.Value = 0;
                });

            this.MovePrevCommand = new ReactiveCommand();
            this.MovePrevCommand.Select(x => this.SelectedIndex.Value > 0).Subscribe(
                _ =>
                {
                    if (!string.IsNullOrWhiteSpace(this.SearchKeyword.Value))
                        this.SearchKeyword.Value = string.Empty;

                    this.SelectedIndex.Value--;
                });

            this.MoveNextCommand = new ReactiveCommand();
            this.MoveNextCommand.Select(x => SelectedIndex.Value < MenuItems.Count - 1).Subscribe(
               _ =>
               {
                   if (!string.IsNullOrWhiteSpace(this.SearchKeyword.Value))
                       this.SearchKeyword.Value = string.Empty;

                   this.SelectedIndex.Value++;
               });
            this.SnackbarMessageQueue = new SnackbarMessageQueue();

            var window = App.GetService<LoginWindow>() !;
            if (window.ShowDialog() != true)
            {
                App.Current.Shutdown();
            }
        }

        private static IEnumerable<NavigationMenuItem> GenerateMenuItems()
        {
            yield return new NavigationMenuItem("Summary", typeof(SummaryBox), PackIconKind.Home);
            yield return new NavigationMenuItem("EC2", typeof(EC2CommandBox), PackIconKind.Terminal);
            yield return new NavigationMenuItem("Color", typeof(ColorSettingsBox), PackIconKind.Palette);
        }

        private bool MenuItemsFilter(object obj)
        {
            if (string.IsNullOrWhiteSpace(this.SearchKeyword.Value))
            {
                return true;
            }

            return obj is NavigationMenuItem item
                   && item.Name.ToLower().Contains(this.SearchKeyword.Value!.ToLower());
        }

        public void OnErrorOccurred(object sender, ErrorOccurredEventArgs e)
        {
            DialogCoordinator?.ShowMessageAsync(this, "エラー", e.Message, MessageDialogStyle.Affirmative);
        }

        public void OnMessage(object sender, MessageEventArgs e)
        {
            DialogCoordinator?.ShowMessageAsync(this, e.Title, e.Message, MessageDialogStyle.Affirmative);
        }

        public void OnSnackBarMessage(object sender, SnackBarMessageEventArgs e)
        {
            this.SnackbarMessageQueue.Enqueue(e.Message);
        }
    }
}