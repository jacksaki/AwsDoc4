using AwsDoc4.Resources;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace AwsDoc4.Resources;

public class PropertyMeta
{
    public PropertyInfo Property { get; init; } = default!;
    public PropertyDescriptionAttribute Attr { get; init; } = default!;
    public object? Instance { get; init; }

    public object? Value
    {
        get => Property.GetValue(Instance);
        set => Property.SetValue(Instance, value);
    }

    public Type PropertyType => Property.PropertyType;
}