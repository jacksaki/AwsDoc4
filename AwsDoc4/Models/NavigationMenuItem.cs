using MaterialDesignThemes.Wpf;
using System.Windows;
using System.Windows.Controls;

namespace AwsDoc4.Models
{
    public class NavigationMenuItem(
        string name,
        Type contentType,
        PackIconKind? packIconKind,
        ScrollBarVisibility horizontalScrollBarVisibilityRequirement,
        ScrollBarVisibility verticalScrollBarVisibilityRequirement)
    {
        public string Name => name;
        public PackIconKind? PackIconKind => packIconKind;
        private Type _contentType => contentType;
        private object? _content;
        public object? Content => _content ??= CreateContent();
        public ScrollBarVisibility HorizontalScrollBarVisibilityRequirement => horizontalScrollBarVisibilityRequirement;
        public ScrollBarVisibility VerticalScrollBarVisibilityRequirement => verticalScrollBarVisibilityRequirement;

        public Thickness MarginRequirement { get; }

        public NavigationMenuItem(
            string name,
            Type t,
            PackIconKind? packIconKind
            ) : this(name, t, packIconKind, ScrollBarVisibility.Disabled, ScrollBarVisibility.Disabled)
        {
        }

        public NavigationMenuItem(
            string name,
            Type t) : this(name, t, null, ScrollBarVisibility.Disabled, ScrollBarVisibility.Disabled)
        {
        }
        private object? CreateContent()
        {
            return Activator.CreateInstance(_contentType);
        }
    }
}