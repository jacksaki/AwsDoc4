using AwsDoc4.Models;
using AwsDoc4.Resources;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace AwsDoc4.Views;

public class PropertyTemplateSelector : DataTemplateSelector
{
    public DataTemplate StringTemplate { get; set; } = default!;
    public DataTemplate BoolTemplate { get; set; } = default!;
    public DataTemplate JsonTemplate { get; set; } = default!;

    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {
        var meta = (PropertyMeta)item;
        var t = meta.PropertyType;

        if (t == typeof(string))
        {
            return StringTemplate;
        }

        if (t == typeof(bool) || t == typeof(bool?))
        {
            return BoolTemplate;
        }

        if (t == typeof(JsonDocument))
        {
            return JsonTemplate;
        }

        return StringTemplate;
    }
}