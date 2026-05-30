using ICSharpCode.AvalonEdit;
using System.Windows;

namespace AwsDoc4.Extensions
{
    public static class AvalonEditBehaviors
    {
        public static readonly DependencyProperty BindableTextProperty =
            DependencyProperty.RegisterAttached(
                "BindableText",
                typeof(string),
                typeof(AvalonEditBehaviors),
                new FrameworkPropertyMetadata(default(string), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnBindableTextChanged));

        public static string GetBindableText(DependencyObject obj)
        {
            return (string)obj.GetValue(BindableTextProperty);
        }

        public static void SetBindableText(DependencyObject obj, string value)
        {
            obj.SetValue(BindableTextProperty, value);
        }

        private static void OnBindableTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextEditor editor)
            {
                string newText = e.NewValue as string ?? string.Empty;

                if (editor.Text != newText)
                    editor.Text = newText;

                // イベントがまだ登録されていないなら登録
                if (!_isUpdatingText.Contains(editor))
                {
                    editor.TextChanged += Editor_TextChanged;
                }
            }
        }

        // 再帰更新防止用
        private static readonly HashSet<TextEditor> _isUpdatingText = new();

        private static void Editor_TextChanged(object? sender, EventArgs e)
        {
            if (sender is TextEditor editor)
            {
                _isUpdatingText.Add(editor);
                SetBindableText(editor, editor.Text);
                _isUpdatingText.Remove(editor);
            }
        }
    }
}