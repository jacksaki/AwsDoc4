using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Text;

namespace AwsDoc4.Resources;

public class PropertyGroupItem
{
    public string Name { get; init; } = "";
    public int Order { get; init; }

    public ObservableCollection<PropertyMeta> Items { get; init; } = new();
    public static List<PropertyGroupItem> Build(AwsResourceBase instance)
    {
        var props = instance.GetType()
            .GetProperties()
            .Select(p => new
            {
                Prop = p,
                Attr = p.GetCustomAttribute<PropertyDescriptionAttribute>()
            })
            .Where(x => x.Attr != null)
            .Select(x => new PropertyMeta
            {
                Property = x.Prop,
                Attr = x.Attr!,
                Instance = instance
            });

        return props
            .GroupBy(x => x.Attr.Group.Order)
            .Select(g => new PropertyGroupItem
            {
                Name = g.First().Attr.Group.Name,
                Order = g.First().Attr.Group.Order,
                Items = new ObservableCollection<PropertyMeta>(
                    g.OrderBy(x => x.Attr.Order))
            })
            .OrderBy(x => x.Order)
            .ToList();
    }
}

