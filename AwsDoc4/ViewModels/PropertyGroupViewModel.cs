using AwsDoc4.Resources;
using System.Collections.ObjectModel;

namespace AwsDoc4.ViewModels;

public class PropertyGroupViewModel
{
    public string Name { get; init; } = "";
    public int Order { get; init; }

    public ObservableCollection<PropertyMeta> Items { get; init; } = new();
    public static List<PropertyGroupViewModel> Build(AwsResourceBase instance)
    {
        return PropertyGroupItem.Build(instance).
            Select(x=> new PropertyGroupViewModel
            {
                Name = x.Name,
                Order = x.Order,
                Items = x.Items
            }).ToList();
    }
}

